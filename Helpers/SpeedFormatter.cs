using System;
using System.Globalization;

namespace NetPulseOverlay.Helpers;

/// <summary>
/// Formats byte-per-second throughput values into compact, human-readable
/// strings with unit-appropriate precision:
///
///     850 B/s        (whole bytes)
///     12.5 KB/s      (one decimal)
///     2.35 MB/s      (two decimals)
///     1.20 GB/s      (two decimals)
///
/// Output is culture-invariant so the overlay renders identically on every
/// locale, and defensively normalizes NaN/Infinity/negative input to zero.
/// </summary>
public static class SpeedFormatter
{
    private const double BytesPerKilobyte = 1024d;
    private const double BytesPerMegabyte = 1024d * 1024d;
    private const double BytesPerGigabyte = 1024d * 1024d * 1024d;

    /// <summary>Formats a bytes-per-second value, e.g. "2.35 MB/s".</summary>
    public static string Format(double bytesPerSecond)
    {
        if (double.IsNaN(bytesPerSecond) || double.IsInfinity(bytesPerSecond) || bytesPerSecond < 0)
        {
            bytesPerSecond = 0;
        }

        if (bytesPerSecond < BytesPerKilobyte)
        {
            return ((long)Math.Round(bytesPerSecond)).ToString(CultureInfo.InvariantCulture) + " B/s";
        }

        if (bytesPerSecond < BytesPerMegabyte)
        {
            return (bytesPerSecond / BytesPerKilobyte).ToString("0.0", CultureInfo.InvariantCulture) + " KB/s";
        }

        if (bytesPerSecond < BytesPerMegabyte * 1024d)
        {
            return (bytesPerSecond / BytesPerMegabyte).ToString("0.00", CultureInfo.InvariantCulture) + " MB/s";
        }

        return (bytesPerSecond / BytesPerGigabyte).ToString("0.00", CultureInfo.InvariantCulture) + " GB/s";
    }
}