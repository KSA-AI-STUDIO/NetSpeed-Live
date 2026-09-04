using System.Diagnostics;
using System.Net.NetworkInformation;

namespace NetPulseOverlay.Services;

/// <summary>
/// A single aggregated speed measurement across all eligible network adapters.
/// </summary>
public sealed class NetworkSpeedSample
{
    public NetworkSpeedSample(double uploadBytesPerSecond, double downloadBytesPerSecond)
    {
        UploadBytesPerSecond = uploadBytesPerSecond;
        DownloadBytesPerSecond = downloadBytesPerSecond;
        TimestampUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Aggregated upload throughput in bytes per second.</summary>
    public double UploadBytesPerSecond { get; }

    /// <summary>Aggregated download throughput in bytes per second.</summary>
    public double DownloadBytesPerSecond { get; }

    /// <summary>UTC timestamp at which the sample was computed.</summary>
    public DateTimeOffset TimestampUtc { get; }
}

/// <summary>
/// Real-time network throughput monitor.
///
/// Every ~1 second the service snapshots all eligible network adapters, diffs
/// their byte counters against the previous snapshot and aggregates the deltas
/// into upload/download bytes-per-second values.
///
/// The service is fully UI-independent: it raises <see cref="SpeedUpdated"/> on
/// a thread-pool thread; consumers are responsible for marshalling to their own
/// dispatcher/synchronization context.
///
/// Robustness rules:
/// - loopback, tunnel and well-known virtual/pseudo adapters are ignored
///   (by interface type and by name/description heuristics)
/// - only adapters with <see cref="OperationalStatus.Up"/> are counted
/// - adapter appearance/disappearance is handled via id-keyed counter state;
///   state for adapters that vanish or go inactive is dropped so counters
///   never go stale
/// - counter resets and out-of-order readings are clamped to zero, so speeds
///   can never be negative
/// - the first sighting of an adapter records a baseline that contributes
///   zero, preventing a huge spike from boot-time-accumulated counters
/// - every tick is wrapped in exception handling: transient failures (adapter
///   churn, Win32 errors) never crash the host application
/// </summary>
public sealed class NetworkMonitorService : IDisposable
{
    private const double DefaultIntervalMilliseconds = 1000;

    /// <summary>
    /// Case-insensitive substrings that identify adapters whose traffic must not
    /// be counted: virtual switches, mirrored and pseudo adapters that would
    /// double-count or report irrelevant traffic. VPN TAP-style adapters are
    /// deliberately NOT excluded here because they carry the real internet
    /// traffic while a VPN is active.
    /// </summary>
    private static readonly string[] ExcludedNameFragments =
    {
        "loopback",
        "vethernet",
        "hyper-v",
        "wsl",
        "wi-fi direct",
        "virtual wifi",
        "kernel debug",
    };

    private readonly System.Timers.Timer _timer;
    private readonly object _gate = new object();

    /// <summary>Interface id -> byte counters captured at the previous tick.</summary>
    private readonly Dictionary<string, (long Sent, long Received)> _lastCounters =
        new Dictionary<string, (long Sent, long Received)>();

    private Stopwatch? _lastTickWatch;
    private volatile bool _tickInProgress;

    /// <summary>Raised roughly once per interval with the aggregated speeds.</summary>
    public event EventHandler<NetworkSpeedSample>? SpeedUpdated;

    /// <summary>Gets the polling interval in milliseconds.</summary>
    public double IntervalMilliseconds { get; }

    /// <summary>Gets a value indicating whether the monitor is currently running.</summary>
    public bool IsRunning => _timer.Enabled;

    /// <summary>Gets the most recent sample, or <c>null</c> before the first tick.</summary>
    public NetworkSpeedSample? LatestSample { get; private set; }

    /// <summary>
    /// Creates the monitor. The interval is clamped to a sane minimum so the
    /// service can never be configured into a busy loop.
    /// </summary>
    public NetworkMonitorService(double intervalMilliseconds = DefaultIntervalMilliseconds)
    {
        if (intervalMilliseconds < 200)
        {
            intervalMilliseconds = DefaultIntervalMilliseconds;
        }

        IntervalMilliseconds = intervalMilliseconds;
        _timer = new System.Timers.Timer(intervalMilliseconds)
        {
            AutoReset = true,
            Enabled = false,
        };
        _timer.Elapsed += OnTimerElapsed;
    }

    /// <summary>
    /// Starts monitoring. An immediate baseline snapshot is taken so the first
    /// reported tick reflects only traffic that occurs after <see cref="Start"/>.
    /// </summary>
    public void Start()
    {
        lock (_gate)
        {
            if (_timer.Enabled)
            {
                return;
            }

            TakeBaselineSnapshot();
            _timer.Start();
        }
    }

    /// <summary>Stops monitoring and clears all captured counter state.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            _timer.Stop();
            _lastCounters.Clear();
            _lastTickWatch = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
        _timer.Dispose();
        SpeedUpdated = null;
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_tickInProgress)
        {
            return; // previous tick still running; skip rather than pile up
        }

        _tickInProgress = true;
        try
        {
            SampleOnce();
        }
        catch (Exception)
        {
            // Monitoring must never crash the host: adapter churn or transient
            // Win32 errors are simply retried on the next tick.
        }
        finally
        {
            _tickInProgress = false;
        }
    }

    /// <summary>
    /// Performs one sampling pass and raises <see cref="SpeedUpdated"/> with the
    /// aggregated result.
    /// </summary>
    private void SampleOnce()
    {
        double elapsedSeconds;
        lock (_gate)
        {
            if (_lastTickWatch is null)
            {
                return;
            }

            elapsedSeconds = _lastTickWatch.Elapsed.TotalSeconds;
            _lastTickWatch.Restart();
        }

        if (elapsedSeconds <= 0)
        {
            return;
        }

        NetworkInterface[] interfaces;
        try
        {
            interfaces = NetworkInterface.GetAllNetworkInterfaces();
        }
        catch (Exception)
        {
            return; // transient failure; retry on the next tick
        }

        long totalSentDelta = 0;
        long totalReceivedDelta = 0;
        HashSet<string> currentIds = new HashSet<string>();

        foreach (NetworkInterface nic in interfaces)
        {
            if (!IsEligible(nic))
            {
                continue;
            }

            IPInterfaceStatistics stats;
            try
            {
                stats = nic.GetIPStatistics();
            }
            catch (Exception)
            {
                continue; // adapter vanished mid-tick; skip it
            }

            currentIds.Add(nic.Id);

            (long Sent, long Received) previous;
            bool hasPrevious;
            lock (_gate)
            {
                hasPrevious = _lastCounters.TryGetValue(nic.Id, out previous);
            }

            if (hasPrevious)
            {
                // Clamp to zero: counters can reset (driver reset, reconnect),
                // which would otherwise produce negative speeds.
                long sentDelta = stats.BytesSent - previous.Sent;
                long receivedDelta = stats.BytesReceived - previous.Received;
                if (sentDelta > 0)
                {
                    totalSentDelta += sentDelta;
                }
                if (receivedDelta > 0)
                {
                    totalReceivedDelta += receivedDelta;
                }
            }

            lock (_gate)
            {
                _lastCounters[nic.Id] = (stats.BytesSent, stats.BytesReceived);
            }
        }

        // Drop counter state for adapters that disappeared or went inactive so
        // a stale baseline can never produce a bogus delta when they return.
        lock (_gate)
        {
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, (long Sent, long Received)> entry in _lastCounters)
            {
                if (!currentIds.Contains(entry.Key))
                {
                    toRemove.Add(entry.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                _lastCounters.Remove(toRemove[i]);
            }
        }

        NetworkSpeedSample sample = new NetworkSpeedSample(
            totalSentDelta / elapsedSeconds,
            totalReceivedDelta / elapsedSeconds);

        LatestSample = sample;
        SpeedUpdated?.Invoke(this, sample);
    }

    /// <summary>
    /// Captures the current counters of all eligible adapters as the baseline
    /// for the first real tick. Must be called while holding <see cref="_gate"/>.
    /// </summary>
    private void TakeBaselineSnapshot()
    {
        _lastCounters.Clear();
        _lastTickWatch = Stopwatch.StartNew();

        NetworkInterface[] interfaces;
        try
        {
            interfaces = NetworkInterface.GetAllNetworkInterfaces();
        }
        catch (Exception)
        {
            return; // no baseline; the first tick will treat every adapter as new
        }

        foreach (NetworkInterface nic in interfaces)
        {
            if (!IsEligible(nic))
            {
                continue;
            }

            try
            {
                IPInterfaceStatistics stats = nic.GetIPStatistics();
                _lastCounters[nic.Id] = (stats.BytesSent, stats.BytesReceived);
            }
            catch (Exception)
            {
                // adapter vanished mid-snapshot; it will be baselined on its
                // first tick instead
            }
        }
    }

    /// <summary>
    /// Determines whether an adapter's traffic should be counted: it must be
    /// operationally up, not a loopback/tunnel pseudo interface, and must not
    /// match a known virtual-adapter name pattern.
    /// </summary>
    private static bool IsEligible(NetworkInterface nic)
    {
        if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
            nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
        {
            return false;
        }

        if (nic.OperationalStatus != OperationalStatus.Up)
        {
            return false;
        }

        string name = nic.Name;
        string description = nic.Description;
        for (int i = 0; i < ExcludedNameFragments.Length; i++)
        {
            string fragment = ExcludedNameFragments[i];
            if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                description.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }
        }

        return true;
    }
}