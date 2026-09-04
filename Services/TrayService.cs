using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Resources;

namespace NetPulseOverlay.Services;

/// <summary>
/// System tray integration.
///
/// Implementation note: this uses the WinForms <see cref="NotifyIcon"/> +
/// <see cref="ContextMenuStrip"/> pair — the most battle-tested tray stack on
/// Windows. It is fully WPF-compatible: the icon is created on the WPF UI
/// thread and its context menu pumps through the WPF dispatcher's message
/// loop. No external NuGet package is required; the csproj enables
/// <c>UseWindowsForms</c> alongside <c>UseWPF</c> for this purpose.
///
/// Menu layout (per spec):
///     Show Overlay
///     Hide Overlay
///     ────────────────
///     Start with Windows   (checkable, backed by the persisted setting)
///     Click-Through        (checkable, backed by the persisted setting)
///     ────────────────
///     About NetSpeed Live
///     ────────────────
///     Exit
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly Action _showOverlay;
    private readonly Action _hideOverlay;
    private readonly Action _exitApplication;
    private readonly Action _showAbout;
    private readonly Func<bool> _getStartWithWindows;
    private readonly Action<bool> _setStartWithWindows;
    private readonly Func<bool> _getClickThrough;
    private readonly Action<bool> _setClickThrough;

    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public TrayService(
        Action showOverlay,
        Action hideOverlay,
        Action exitApplication,
        Action showAbout,
        Func<bool> getStartWithWindows,
        Action<bool> setStartWithWindows,
        Func<bool> getClickThrough,
        Action<bool> setClickThrough)
    {
        _showOverlay = showOverlay ?? throw new ArgumentNullException(nameof(showOverlay));
        _hideOverlay = hideOverlay ?? throw new ArgumentNullException(nameof(hideOverlay));
        _exitApplication = exitApplication ?? throw new ArgumentNullException(nameof(exitApplication));
        _showAbout = showAbout ?? throw new ArgumentNullException(nameof(showAbout));
        _getStartWithWindows = getStartWithWindows ?? throw new ArgumentNullException(nameof(getStartWithWindows));
        _setStartWithWindows = setStartWithWindows ?? throw new ArgumentNullException(nameof(setStartWithWindows));
        _getClickThrough = getClickThrough ?? throw new ArgumentNullException(nameof(getClickThrough));
        _setClickThrough = setClickThrough ?? throw new ArgumentNullException(nameof(setClickThrough));

        ContextMenuStrip menu = new ContextMenuStrip();

        ToolStripMenuItem showItem = new ToolStripMenuItem("Show Overlay");
        showItem.Click += (_, _) => _showOverlay();

        ToolStripMenuItem hideItem = new ToolStripMenuItem("Hide Overlay");
        hideItem.Click += (_, _) => _hideOverlay();

        ToolStripMenuItem startupItem = new ToolStripMenuItem("Start with Windows");
        startupItem.CheckOnClick = true;
        startupItem.Checked = _getStartWithWindows();
        startupItem.CheckedChanged += (_, _) => _setStartWithWindows(startupItem.Checked);

        ToolStripMenuItem clickThroughItem = new ToolStripMenuItem("Click-Through");
        clickThroughItem.CheckOnClick = true;
        clickThroughItem.Checked = _getClickThrough();
        clickThroughItem.CheckedChanged += (_, _) => _setClickThrough(clickThroughItem.Checked);

        ToolStripMenuItem aboutItem = new ToolStripMenuItem("About NetSpeed Live");
        aboutItem.Click += (_, _) => _showAbout();

        ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => _exitApplication();

        menu.Items.Add(showItem);
        menu.Items.Add(hideItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(startupItem);
        menu.Items.Add(clickThroughItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(aboutItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "NetSpeed Live",
            Visible = true,
            ContextMenuStrip = menu,
        };

        // Double-clicking the tray icon is a standard shortcut for showing the
        // overlay again.
        _notifyIcon.DoubleClick += (_, _) => _showOverlay();
    }

    /// <summary>
    /// Loads the embedded application icon; falls back to a system icon so a
    /// resource problem can never prevent the tray from appearing. The frame
    /// is selected at the shell's small-icon size (16×16 at standard DPI,
    /// 32×32 at 200%) so the tray renders a native frame from the
    /// multi-resolution icon instead of downscaling the 32×32 default frame.
    /// </summary>
    private static Icon LoadAppIcon()
    {
        try
        {
            Uri uri = new Uri("pack://application:,,,/Assets/app.ico");
            StreamResourceInfo? info = System.Windows.Application.GetResourceStream(uri);
            if (info?.Stream != null)
            {
                Size traySize = SystemInformation.SmallIconSize;
                return new Icon(info.Stream, traySize.Width, traySize.Height);
            }
        }
        catch (Exception)
        {
            // fall through to the system icon
        }

        return SystemIcons.Application;
    }

    /// <summary>
    /// Removes the tray icon and releases every native and managed resource
    /// it owns. Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _notifyIcon.Visible = false; // removes the icon from the tray immediately
        ContextMenuStrip? menu = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = null;
        menu?.Dispose();
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
    }
}