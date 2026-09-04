using System.Windows;
using NetPulseOverlay.Models;
using NetPulseOverlay.Services;
using NetPulseOverlay.Views;

// Disambiguate from System.Windows.Forms.Application, which is imported by the
// implicit WinForms usings that come with <UseWindowsForms> in the csproj.
using Application = System.Windows.Application;

namespace NetPulseOverlay;

/// <summary>
/// Application entry point and owner of the long-lived services.
///
/// Stage 6: system tray. The app runs with <c>ShutdownMode=OnExplicitShutdown</c>
/// so it keeps running when the overlay is hidden or closed: closing the
/// overlay hides it to the tray instead of terminating. The tray menu provides
/// Show/Hide, the persisted Start-with-Windows toggle and Exit, which shuts the
/// app down and disposes the tray, the monitor and all other resources.
///
/// Stage 8: real autostart. On startup the persisted preference is reconciled
/// with the actual HKCU Run registry entry (development builds never register);
/// the tray toggle updates both the preference and the registry.
/// </summary>
public partial class App : Application
{
    private SettingsService? _settingsService;
    private AppSettings? _settings;
    private StartupService? _startupService;
    private NetworkMonitorService? _monitor;
    private OverlayWindow? _overlay;
    private TrayService? _tray;
    private bool _exitRequested;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SettingsService settingsService = new SettingsService();
        AppSettings settings = settingsService.Load();

        // Reconcile the persisted autostart preference with the actual HKCU Run
        // registry state: registers when preferred (published builds only),
        // removes stale entries when not, and refreshes the registered path if
        // the executable moved. Never throws.
        StartupService startupService = new StartupService();
        startupService.SetEnabled(settings.StartWithWindows);

        NetworkMonitorService monitor = new NetworkMonitorService();
        monitor.Start();

        OverlayWindow overlay = new OverlayWindow(monitor, settings, settingsService);

        // Close-to-tray: intercepting the close keeps the app running in the
        // tray; only an explicit Exit (via the tray menu) really shuts down.
        overlay.Closing += (_, args) =>
        {
            if (!_exitRequested)
            {
                args.Cancel = true;
                HideOverlay();
            }
        };

        // Overlay context menu: reuse the same lifecycle paths as the tray so
        // click-through, hide, about and exit behave identically from both menus.
        overlay.ClickThroughToggled += SetClickThrough;
        overlay.HideRequested += (_, _) => HideOverlay();
        overlay.AboutRequested += (_, _) => ShowAboutWindow();
        overlay.ExitRequested += (_, _) => ExitApplication();

        overlay.Show();
        _settingsService = settingsService;
        _settings = settings;
        _startupService = startupService;
        _monitor = monitor;
        _overlay = overlay;

        _tray = new TrayService(
            showOverlay: ShowOverlay,
            hideOverlay: HideOverlay,
            exitApplication: ExitApplication,
            showAbout: ShowAboutWindow,
            getStartWithWindows: () => settings.StartWithWindows,
            setStartWithWindows: value =>
            {
                settings.StartWithWindows = value;
                settingsService.Save(settings);
                startupService.SetEnabled(value);
            },
            getClickThrough: () => settings.ClickThroughEnabled,
            setClickThrough: SetClickThrough);
    }

    /// <summary>
    /// Tray action: apply click-through to the overlay window and persist it.
    /// The saved state is re-applied on every startup.
    /// </summary>
    private void SetClickThrough(bool value)
    {
        if (_settings is null || _settingsService is null)
        {
            return;
        }

        _settings.ClickThroughEnabled = value;
        _settingsService.Save(_settings);
        _overlay?.SetClickThrough(value);
    }

    /// <summary>Tray action: restore and activate the overlay (stays topmost).</summary>
    private void ShowOverlay()
    {
        if (_overlay is null)
        {
            return;
        }

        _overlay.Show();
        _overlay.Activate();
    }

    /// <summary>Tray action: hide the overlay; the monitor keeps running.</summary>
    private void HideOverlay()
    {
        _overlay?.Hide();
    }

    /// <summary>
    /// Opens the WPF About window (assembly metadata + Donate via UPI). Used by
    /// both the tray menu and the overlay context menu so they share the same
    /// implementation. The window is owned by the overlay only while the overlay
    /// is visible; otherwise it opens unowned so it still appears when the overlay
    /// is hidden to the tray. Modal so only one instance can be open at a time.
    /// </summary>
    private void ShowAboutWindow()
    {
        AboutWindow about = new AboutWindow
        {
            Owner = _overlay is { IsVisible: true } ? _overlay : null,
        };
        about.ShowDialog();
    }

    /// <summary>Tray action: explicitly shut the application down.</summary>
    private void ExitApplication()
    {
        _exitRequested = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose in dependency order: the tray icon first (removes it from the
        // notification area and frees its menu/icon), then the overlay
        // reference, then the monitor's timer and counter state.
        _tray?.Dispose();
        _tray = null;

        _overlay = null;

        _monitor?.Dispose();
        _monitor = null;

        _startupService = null;
        _settings = null;
        _settingsService = null;

        base.OnExit(e);
    }
}