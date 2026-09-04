using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using NetPulseOverlay.Helpers;
using NetPulseOverlay.Models;
using NetPulseOverlay.Services;
// Using aliases resolve ambiguity with System.Drawing, which is implicitly
// imported by <UseWindowsForms> in the csproj (used for the tray icon stack).
using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfBrushes = System.Windows.Media.Brushes;

namespace NetPulseOverlay.Views;

/// <summary>
/// Transparent, borderless, always-on-top overlay that displays the network
/// speed as a compact block (Horizontal by default, or Vertical):
///
/// Horizontal: ↓ 14.82 MB/s    ↑ 2.35 MB/s
/// Vertical:   ↓ 14.82 MB/s
///             ↑ 2.35 MB/s
///
/// Layout stability: the arrow column and the speed-value column are fixed to
/// measured widths so ordinary speed updates only swap the text content — the
/// window never re-sizes or re-positions while values change. Appearance
/// settings (display mode, colour, family, size, weight, opacity) are
/// applied eagerly and persisted; position safety re-measures and clamps on
/// appearance changes only.
///
/// Dragging (Stage 5) and click-through (Stage 7) behaviour are unchanged; the
/// global menu routes Click-Through/Hide/Exit/About through the app's existing
/// lifecycle without duplicating it.
/// </summary>
public partial class OverlayWindow : Window
{
    /// <summary>Fraction of the overlay that must stay visible to be reachable.</summary>
    private const double MinimumVisibleFraction = 0.25;

    private const double FontSizeSmall = 14;
    private const double FontSizeMedium = 19;
    private const double FontSizeLarge = 24;

    private const double OpacityHalf = 0.50;
    private const double OpacityThreeQuarter = 0.75;
    private const double OpacityFull = 1.00;

    /// <summary>The longest string SpeedFormatter can emit (the reserved stable value width).</summary>
    private const string ReservedMaxValueText = "999.99 GB/s";

    private const double ArrowColumnRightPad = 4;
    private const double ValueColumnRightPad = 2;

    /// <summary>Curated text colours (readable variants are used for Green/Blue).</summary>
    private static readonly Dictionary<string, WpfColor> ColourMap = new Dictionary<string, WpfColor>(StringComparer.OrdinalIgnoreCase)
    {
        ["White"] = Colors.White,
        ["Red"] = Colors.Red,
        ["Green"] = WpfColor.FromRgb(0, 201, 80),
        ["Blue"] = Colors.DodgerBlue,
        ["Yellow"] = Colors.Yellow,
        ["Cyan"] = Colors.Cyan,
    };

    /// <summary>Curated font families for the context menu.</summary>
    private static readonly string[] FontFamilyOptions =
    {
        "Segoe UI", "Cascadia Mono", "Consolas", "Arial", "Calibri", "Verdana", "Trebuchet MS",
    };

    private readonly NetworkMonitorService _monitor;
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private bool _clickThrough;

    /// <summary>Raised when the user toggles click-through in the overlay menu.</summary>
    public event Action<bool>? ClickThroughToggled;

    /// <summary>Raised when the user picks Hide Overlay in the overlay menu.</summary>
    public event EventHandler? HideRequested;

    /// <summary>Raised when the user picks About NetSpeed Live in the overlay menu.</summary>
    public event EventHandler? AboutRequested;

    /// <summary>Raised when the user picks Exit in the overlay menu.</summary>
    public event EventHandler? ExitRequested;

    public OverlayWindow(NetworkMonitorService monitor, AppSettings settings, SettingsService settingsService)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        ContextMenuOpening += (_, _) => UpdateMenuChecks();

        // Display Mode
        BothModeItem.Click += (_, _) => SetDisplayMode(OverlayDisplayMode.Both);
        UploadOnlyModeItem.Click += (_, _) => SetDisplayMode(OverlayDisplayMode.UploadOnly);
        DownloadOnlyModeItem.Click += (_, _) => SetDisplayMode(OverlayDisplayMode.DownloadOnly);

        // Layout Orientation
        OrientationHorizontalItem.Click += (_, _) => SetDisplayOrientation("Horizontal");
        OrientationVerticalItem.Click += (_, _) => SetDisplayOrientation("Vertical");

        // Text Colour
        ColourWhiteItem.Click += (_, _) => SetTextColour("White");
        ColourRedItem.Click += (_, _) => SetTextColour("Red");
        ColourGreenItem.Click += (_, _) => SetTextColour("Green");
        ColourBlueItem.Click += (_, _) => SetTextColour("Blue");
        ColourYellowItem.Click += (_, _) => SetTextColour("Yellow");
        ColourCyanItem.Click += (_, _) => SetTextColour("Cyan");

        // Font Family
        FontSegoeItem.Click += (_, _) => SetFontFamily("Segoe UI");
        FontCascadiaItem.Click += (_, _) => SetFontFamily("Cascadia Mono");
        FontConsolasItem.Click += (_, _) => SetFontFamily("Consolas");
        FontArialItem.Click += (_, _) => SetFontFamily("Arial");
        FontCalibriItem.Click += (_, _) => SetFontFamily("Calibri");
        FontVerdanaItem.Click += (_, _) => SetFontFamily("Verdana");
        FontTrebuchetItem.Click += (_, _) => SetFontFamily("Trebuchet MS");

        // Font Size
        FontSizeSmallItem.Click += (_, _) => SetFontSize(FontSizeSmall);
        FontSizeMediumItem.Click += (_, _) => SetFontSize(FontSizeMedium);
        FontSizeLargeItem.Click += (_, _) => SetFontSize(FontSizeLarge);

        // Font Weight
        FontWeightLightItem.Click += (_, _) => SetFontWeight("Light");
        FontWeightRegularItem.Click += (_, _) => SetFontWeight("Regular");
        FontWeightSemiBoldItem.Click += (_, _) => SetFontWeight("SemiBold");
        FontWeightBoldItem.Click += (_, _) => SetFontWeight("Bold");

        // Opacity
        Opacity50Item.Click += (_, _) => SetOpacity(OpacityHalf);
        Opacity75Item.Click += (_, _) => SetOpacity(OpacityThreeQuarter);
        Opacity100Item.Click += (_, _) => SetOpacity(OpacityFull);

        // Global lifecycle actions are reused via events - never duplicated.
        ClickThroughMenuItem.Click += (_, _) => ClickThroughToggled?.Invoke(ClickThroughMenuItem.IsChecked);
        HideMenuItem.Click += (_, _) => HideRequested?.Invoke(this, EventArgs.Empty);
        AboutMenuItem.Click += (_, _) => AboutRequested?.Invoke(this, EventArgs.Empty);
        ExitMenuItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        _monitor.SpeedUpdated += OnSpeedUpdated;
        Closed += (_, _) => _monitor.SpeedUpdated -= OnSpeedUpdated;

        ApplySavedPositionBeforeShow();
    }

    /// <summary>
    /// Runs on the monitor's thread-pool thread and updates the visible text on
    /// the UI thread. Both rows are always refreshed (even in single-line modes
    /// the hidden row stays current); no layout work happens here, so values can
    /// never resize or re-position the overlay.
    /// </summary>
    private void OnSpeedUpdated(object? sender, NetworkSpeedSample sample)
    {
        string upload = SpeedFormatter.Format(sample.UploadBytesPerSecond);
        string download = SpeedFormatter.Format(sample.DownloadBytesPerSecond);
        Dispatcher.InvokeAsync(() =>
        {
            UploadSpeed.Text = upload;
            DownloadSpeed.Text = download;
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySavedAppearance();
        ApplyClickThroughFromSettings();
        RestorePositionWithValidation();
    }

    /// <summary>Applies all persisted appearance settings, then fixes layout.</summary>
    private void ApplySavedAppearance()
    {
        ApplyDisplayOrientation();
        ApplyTypographyAndPalette();
        ApplyDisplayMode();
        ReMeasureAndClamp();
    }

    /// <summary>
    /// Applies the persisted font size/family/weight/spacing, text colour and
    /// opacity to both rows (arrows + values). All values are clamped/normalised
    /// so a corrupt settings file can never produce a broken render.
    /// </summary>
    private void ApplyTypographyAndPalette()
    {
        double size = Math.Clamp(_settings.FontSize, 10, 32);
        double opacity = Math.Clamp(_settings.Opacity, 0.35, 1.0);
        WpfFontFamily family = ResolveFontFamily(_settings.FontFamilyName);
        FontWeight weight = ParseFontWeight(_settings.FontWeight);
        SolidColorBrush brush = new SolidColorBrush(ResolveColour(_settings.TextColour));
        brush.Freeze();

        foreach (TextBlock speed in SpeedValueTextBlocks())
        {
            speed.FontFamily = family;
            speed.FontSize = size;
            speed.FontWeight = weight;
            speed.Foreground = brush;
            speed.Opacity = opacity;
        }

        foreach (TextBlock arrow in ArrowTextBlocks())
        {
            arrow.FontFamily = family;
            arrow.FontSize = size;
            arrow.FontWeight = weight;
            arrow.Foreground = brush;
            arrow.Opacity = opacity;
        }
    }

    private IEnumerable<TextBlock> SpeedValueTextBlocks()
    {
        yield return UploadSpeed;
        yield return DownloadSpeed;
    }

    private IEnumerable<TextBlock> ArrowTextBlocks()
    {
        yield return UploadArrow;
        yield return DownloadArrow;
    }

    /// <summary>Collapses the hidden row in single-line display modes.</summary>
    private void ApplyDisplayMode()
    {
        OverlayDisplayMode mode = ParseDisplayMode(_settings.DisplayMode);
        Visibility uploadVisibility = mode == OverlayDisplayMode.DownloadOnly ? Visibility.Collapsed : Visibility.Visible;
        Visibility downloadVisibility = mode == OverlayDisplayMode.UploadOnly ? Visibility.Collapsed : Visibility.Visible;

        UploadArrow.Visibility = uploadVisibility;
        UploadSpeed.Visibility = uploadVisibility;
        DownloadArrow.Visibility = downloadVisibility;
        DownloadSpeed.Visibility = downloadVisibility;
    }

    /// <summary>
    /// Parses the orientation setting; unknown values default to Horizontal.
    /// </summary>
    private static bool IsVertical(string? orientation) =>
        string.Equals(orientation, "Vertical", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Switches the Grid between a single horizontal row
    /// (UploadArrow UploadSpeed DownloadArrow DownloadSpeed) and the classic
    /// 2×2 vertical layout, reusing the same four TextBlocks.
    /// </summary>
    private void ApplyDisplayOrientation()
    {
        bool vertical = IsVertical(_settings.DisplayOrientation);

        if (vertical)
        {
            // 2 rows × 2 columns — download on top, upload below
            RootGrid.RowDefinitions.Clear();
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.ColumnDefinitions.Clear();
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetRow(DownloadArrow, 0); Grid.SetColumn(DownloadArrow, 0);
            Grid.SetRow(DownloadSpeed, 0); Grid.SetColumn(DownloadSpeed, 1);
            Grid.SetRow(UploadArrow, 1); Grid.SetColumn(UploadArrow, 0);
            Grid.SetRow(UploadSpeed, 1); Grid.SetColumn(UploadSpeed, 1);

            DownloadSpeed.TextAlignment = TextAlignment.Right;
            UploadSpeed.TextAlignment = TextAlignment.Right;
        }
        else
        {
            // 1 row × 4 columns — download first, then upload
            RootGrid.RowDefinitions.Clear();
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.ColumnDefinitions.Clear();
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetRow(DownloadArrow, 0); Grid.SetColumn(DownloadArrow, 0);
            Grid.SetRow(DownloadSpeed, 0); Grid.SetColumn(DownloadSpeed, 1);
            Grid.SetRow(UploadArrow, 0); Grid.SetColumn(UploadArrow, 2);
            Grid.SetRow(UploadSpeed, 0); Grid.SetColumn(UploadSpeed, 3);

            DownloadSpeed.TextAlignment = TextAlignment.Left;
            UploadSpeed.TextAlignment = TextAlignment.Left;
        }
    }

    private void SetDisplayOrientation(string orientation)
    {
        _settings.DisplayOrientation = orientation;
        _settingsService.Save(_settings);
        ApplyDisplayOrientation();
        ApplyDisplayMode();
        ReMeasureAndClamp();
    }

    /// <summary>
    /// Re-fixes the arrow and value column widths to the current typography and
    /// (after layout settles) clamps the window back into the work area and
    /// persists the final position. Called only on appearance changes, never on
    /// speed updates.
    /// </summary>
    private void ReMeasureAndClamp()
    {
        try
        {
            MeasureColumns();
        }
        catch (Exception)
        {
            // Keep the previous column widths if measurement fails.
        }

        Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (ActualWidth <= 0 || ActualHeight <= 0)
                {
                    return;
                }

                ClampIntoWorkArea();
                PersistPosition();
                UpdateMenuChecks();
            }),
            DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Reserves a stable width for the arrow column and the speed-value column.
    /// The value width is measured against the longest string the formatter can
    /// produce ("999.99 GB/s"), so unit transitions can never resize the window.
    /// </summary>
    private void MeasureColumns()
    {
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        bool vertical = IsVertical(_settings.DisplayOrientation);

        // Arrow column width (shared by both arrows).
        Typeface arrowFace = new Typeface(UploadArrow.FontFamily, UploadArrow.FontStyle, UploadArrow.FontWeight, UploadArrow.FontStretch);
        FormattedText arrowText = new FormattedText("\u2191", CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, arrowFace, UploadArrow.FontSize, WpfBrushes.Black, pixelsPerDip);
        double arrowWidth = Math.Ceiling(arrowText.Width) + ArrowColumnRightPad;

        // Speed-value column width (reserved for the longest formatted string).
        Typeface speedFace = new Typeface(UploadSpeed.FontFamily, UploadSpeed.FontStyle, UploadSpeed.FontWeight, UploadSpeed.FontStretch);
        FormattedText speedText = new FormattedText(ReservedMaxValueText, CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, speedFace, UploadSpeed.FontSize, WpfBrushes.Black, pixelsPerDip);
        double speedWidth = Math.Ceiling(speedText.Width) + ValueColumnRightPad;

        if (vertical)
        {
            // 2 columns: arrow + speed value
            ((ColumnDefinition)RootGrid.ColumnDefinitions[0]).Width = new GridLength(arrowWidth);
            ((ColumnDefinition)RootGrid.ColumnDefinitions[1]).Width = new GridLength(speedWidth);
        }
        else
        {
            // 4 columns: download-arrow, download-speed, upload-arrow, upload-speed.
            // Collapsed pairs get zero width so single-direction display modes
            // leave no empty gap.
            bool downloadVisible = DownloadArrow.Visibility == Visibility.Visible;
            bool uploadVisible = UploadArrow.Visibility == Visibility.Visible;
            ((ColumnDefinition)RootGrid.ColumnDefinitions[0]).Width = new GridLength(downloadVisible ? arrowWidth : 0);
            ((ColumnDefinition)RootGrid.ColumnDefinitions[1]).Width = new GridLength(downloadVisible ? speedWidth : 0);
            ((ColumnDefinition)RootGrid.ColumnDefinitions[2]).Width = new GridLength(uploadVisible ? arrowWidth : 0);
            ((ColumnDefinition)RootGrid.ColumnDefinitions[3]).Width = new GridLength(uploadVisible ? speedWidth : 0);
        }
    }

    private void SetDisplayMode(OverlayDisplayMode mode)
    {
        _settings.DisplayMode = DisplayModeStorage(mode);
        _settingsService.Save(_settings);
        ApplyDisplayMode();
        ReMeasureAndClamp();
    }

    private void SetTextColour(string name)
    {
        _settings.TextColour = name;
        _settingsService.Save(_settings);
        ApplyTypographyAndPalette();
        UpdateMenuChecks();
    }

    private void SetFontFamily(string name)
    {
        _settings.FontFamilyName = name;
        _settingsService.Save(_settings);
        ApplyTypographyAndPalette();
        ReMeasureAndClamp();
    }

    /// <summary>Sets the font size immediately and persists it (re-measure + clamp).</summary>
    private void SetFontSize(double size)
    {
        _settings.FontSize = size;
        _settingsService.Save(_settings);
        ApplyTypographyAndPalette();
        ReMeasureAndClamp();
    }

    private void SetFontWeight(string weight)
    {
        _settings.FontWeight = weight;
        _settingsService.Save(_settings);
        ApplyTypographyAndPalette();
        ReMeasureAndClamp();
    }

    /// <summary>
    /// Sets the opacity of the visible overlay content (text + shadow) and
    /// persists it. WPF hit-testing is unaffected by opacity, so mouse
    /// interaction remains reliable at every level.
    /// </summary>
    private void SetOpacity(double opacity)
    {
        _settings.Opacity = Math.Clamp(opacity, 0.35, 1.0);
        _settingsService.Save(_settings);
        ApplyTypographyAndPalette();
        UpdateMenuChecks();
    }

    /// <summary>
    /// Syncs all checkable menu items with the current settings, so the
    /// checkmarks always reflect reality whenever the menu opens.
    /// </summary>
    private void UpdateMenuChecks()
    {
        // Display mode
        OverlayDisplayMode mode = ParseDisplayMode(_settings.DisplayMode);
        BothModeItem.IsChecked = mode == OverlayDisplayMode.Both;
        UploadOnlyModeItem.IsChecked = mode == OverlayDisplayMode.UploadOnly;
        DownloadOnlyModeItem.IsChecked = mode == OverlayDisplayMode.DownloadOnly;

        // Layout orientation
        bool vertical = IsVertical(_settings.DisplayOrientation);
        OrientationHorizontalItem.IsChecked = !vertical;
        OrientationVerticalItem.IsChecked = vertical;

        // Text colour
        ColourWhiteItem.IsChecked = IsSetting(_settings.TextColour, "White");
        ColourRedItem.IsChecked = IsSetting(_settings.TextColour, "Red");
        ColourGreenItem.IsChecked = IsSetting(_settings.TextColour, "Green");
        ColourBlueItem.IsChecked = IsSetting(_settings.TextColour, "Blue");
        ColourYellowItem.IsChecked = IsSetting(_settings.TextColour, "Yellow");
        ColourCyanItem.IsChecked = IsSetting(_settings.TextColour, "Cyan");

        // Font family
        FontSegoeItem.IsChecked = IsSetting(_settings.FontFamilyName, "Segoe UI");
        FontCascadiaItem.IsChecked = IsSetting(_settings.FontFamilyName, "Cascadia Mono");
        FontConsolasItem.IsChecked = IsSetting(_settings.FontFamilyName, "Consolas");
        FontArialItem.IsChecked = IsSetting(_settings.FontFamilyName, "Arial");
        FontCalibriItem.IsChecked = IsSetting(_settings.FontFamilyName, "Calibri");
        FontVerdanaItem.IsChecked = IsSetting(_settings.FontFamilyName, "Verdana");
        FontTrebuchetItem.IsChecked = IsSetting(_settings.FontFamilyName, "Trebuchet MS");

        // Font size
        FontSizeSmallItem.IsChecked = Math.Abs(_settings.FontSize - FontSizeSmall) < 0.1;
        FontSizeMediumItem.IsChecked = Math.Abs(_settings.FontSize - FontSizeMedium) < 0.1;
        FontSizeLargeItem.IsChecked = Math.Abs(_settings.FontSize - FontSizeLarge) < 0.1;

        // Font weight
        FontWeightLightItem.IsChecked = IsSetting(_settings.FontWeight, "Light");
        FontWeightRegularItem.IsChecked = IsSetting(_settings.FontWeight, "Regular");
        FontWeightSemiBoldItem.IsChecked = IsSetting(_settings.FontWeight, "SemiBold");
        FontWeightBoldItem.IsChecked = IsSetting(_settings.FontWeight, "Bold");

        // Opacity
        Opacity50Item.IsChecked = Math.Abs(_settings.Opacity - OpacityHalf) < 0.01;
        Opacity75Item.IsChecked = Math.Abs(_settings.Opacity - OpacityThreeQuarter) < 0.01;
        Opacity100Item.IsChecked = _settings.Opacity >= OpacityFull;

        ClickThroughMenuItem.IsChecked = _clickThrough;
    }

    private static bool IsSetting(string? actual, string expected) =>
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    private enum OverlayDisplayMode
    {
        Both,
        UploadOnly,
        DownloadOnly,
    }

    private static OverlayDisplayMode ParseDisplayMode(string? value) =>
        IsSetting(value, "Only Upload") ? OverlayDisplayMode.UploadOnly :
        IsSetting(value, "Only Download") ? OverlayDisplayMode.DownloadOnly :
        OverlayDisplayMode.Both;

    private static string DisplayModeStorage(OverlayDisplayMode mode) =>
        mode == OverlayDisplayMode.UploadOnly ? "Only Upload" :
        mode == OverlayDisplayMode.DownloadOnly ? "Only Download" :
        "Both";

    /// <summary>Resolves a colour name; unknown names safely fall back to white.</summary>
    private static WpfColor ResolveColour(string? name) =>
        name is not null && ColourMap.TryGetValue(name, out WpfColor colour) ? colour : Colors.White;

    /// <summary>Resolves a font weight; unknown values fall back to Regular.</summary>
    private static FontWeight ParseFontWeight(string? weight) =>
        IsSetting(weight, "Light") ? FontWeights.Light :
        IsSetting(weight, "SemiBold") ? FontWeights.SemiBold :
        IsSetting(weight, "Bold") ? FontWeights.Bold :
        FontWeights.Regular;

    /// <summary>
    /// Resolves a font family against the installed system fonts; anything
    /// unavailable falls back to Segoe UI, so a missing font can never crash.
    /// </summary>
    private static WpfFontFamily ResolveFontFamily(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return new WpfFontFamily("Segoe UI");
        }

        try
        {
            foreach (WpfFontFamily candidate in Fonts.SystemFontFamilies)
            {
                if (string.Equals(candidate.Source, requested, StringComparison.OrdinalIgnoreCase))
                {
                    return new WpfFontFamily(requested);
                }
            }
        }
        catch (Exception)
        {
            // Fall through to the safe default.
        }

        return new WpfFontFamily("Segoe UI");
    }

    /// <summary>
    /// Clamps the overlay fully inside the primary work area after a size
    /// change so a larger font can never push it off-screen or behind the
    /// taskbar.
    /// </summary>
    private void ClampIntoWorkArea()
    {
        if (double.IsNaN(Left) || double.IsNaN(Top) || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        Rect work = SystemParameters.WorkArea;
        Left = Math.Min(Math.Max(Left, work.Left), Math.Max(work.Left, work.Right - ActualWidth));
        Top = Math.Min(Math.Max(Top, work.Top), Math.Max(work.Top, work.Bottom - ActualHeight));
    }

    /// <summary>Applies the persisted click-through state once the handle exists.</summary>
    private void ApplyClickThroughFromSettings()
    {
        SetClickThrough(_settings.ClickThroughEnabled);
    }

    /// <summary>
    /// Toggles click-through. When enabled, WS_EX_TRANSPARENT makes the OS
    /// deliver all mouse input to the windows underneath the overlay; WPF's
    /// own WS_EX_LAYERED (transparency) and the topmost flag are preserved.
    /// While active, dragging is refused in <see cref="OnMouseLeftButtonDown"/>.
    /// </summary>
    public void SetClickThrough(bool enabled)
    {
        _clickThrough = enabled;

        IntPtr handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            NativeMethods.SetClickThrough(handle, enabled);
        }
    }

    /// <summary>
    /// Click-and-hold anywhere on the visible text drags the overlay. DragMove
    /// runs a modal move loop and returns when the button is released; the
    /// final position is then persisted.
    /// </summary>
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_clickThrough)
        {
            // Click-through is active: the overlay must not react to the mouse.
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove is only legal while the left button is pressed.
            return;
        }

        PersistPosition();
    }

    /// <summary>Writes the current window position into the settings file.</summary>
    private void PersistPosition()
    {
        if (double.IsNaN(Left) || double.IsNaN(Top))
        {
            return;
        }

        _settings.OverlayX = Left;
        _settings.OverlayY = Top;
        _settingsService.Save(_settings);
    }

    /// <summary>
    /// Applies the saved position (if any) before the window is shown so the
    /// overlay does not visibly jump; validation happens after layout.
    /// </summary>
    private void ApplySavedPositionBeforeShow()
    {
        if (TryGetSavedPosition(out double x, out double y))
        {
            Left = x;
            Top = y;
        }
    }

    /// <summary>
    /// After layout, validates the restored position against the current
    /// monitor configuration and falls back to top-center when it is missing
    /// or unreachable.
    /// </summary>
    private void RestorePositionWithValidation()
    {
        if (double.IsNaN(ActualWidth) || ActualWidth <= 0 || ActualHeight <= 0)
        {
            // SizeToContent has not produced the final size yet; retry after
            // the layout pass completes.
            Dispatcher.BeginInvoke(
                new Action(RestorePositionWithValidation),
                DispatcherPriority.Loaded);
            return;
        }

        if (TryGetSavedPosition(out double x, out double y) && IsPositionReachable(x, y))
        {
            NudgeIntoPrimaryWorkAreaIfCovered(ref x, ref y);

            Left = x;
            Top = y;

            if (x != _settings.OverlayX || y != _settings.OverlayY)
            {
                PersistPosition();
            }

            return;
        }

        // No saved position, or it is unreachable on the current monitor
        // configuration: fall back to the top-center of the primary work area
        // and persist the corrected position.
        PositionAtTopCenterOfPrimaryMonitor();
        PersistPosition();
    }

    private bool TryGetSavedPosition(out double x, out double y)
    {
        x = 0;
        y = 0;

        if (_settings.OverlayX is double savedX && _settings.OverlayY is double savedY &&
            !double.IsNaN(savedX) && !double.IsInfinity(savedX) &&
            !double.IsNaN(savedY) && !double.IsInfinity(savedY))
        {
            x = savedX;
            y = savedY;
            return true;
        }

        return false;
    }

    /// <summary>
    /// A position is reachable when at least <see cref="MinimumVisibleFraction"/>
    /// of the overlay intersects the virtual screen (the union of all connected
    /// monitors). This keeps the overlay grabbable when monitors are
    /// disconnected or the resolution changes.
    /// </summary>
    private bool IsPositionReachable(double x, double y)
    {
        double width = ActualWidth;
        double height = ActualHeight;

        double virtualLeft = SystemParameters.VirtualScreenLeft;
        double virtualTop = SystemParameters.VirtualScreenTop;
        double virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        double virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

        double intersectLeft = Math.Max(x, virtualLeft);
        double intersectTop = Math.Max(y, virtualTop);
        double intersectRight = Math.Min(x + width, virtualRight);
        double intersectBottom = Math.Min(y + height, virtualBottom);

        return intersectRight - intersectLeft >= width * MinimumVisibleFraction &&
               intersectBottom - intersectTop >= height * MinimumVisibleFraction;
    }

    /// <summary>
    /// If the (otherwise reachable) position would land entirely inside
    /// taskbar territory on the primary monitor, clamp it back into the
    /// primary work area so it never hides behind the taskbar.
    /// </summary>
    private void NudgeIntoPrimaryWorkAreaIfCovered(ref double x, ref double y)
    {
        double width = ActualWidth;
        double height = ActualHeight;

        double primaryRight = SystemParameters.PrimaryScreenWidth;
        double primaryBottom = SystemParameters.PrimaryScreenHeight;

        // Only relevant when the overlay sits on the primary monitor.
        if (x + width <= 0 || y + height <= 0 || x >= primaryRight || y >= primaryBottom)
        {
            return;
        }

        Rect work = SystemParameters.WorkArea;

        bool intersectsWorkArea =
            x < work.Right && x + width > work.Left &&
            y < work.Bottom && y + height > work.Top;
        if (intersectsWorkArea)
        {
            return;
        }

        x = Math.Min(Math.Max(x, work.Left), Math.Max(work.Left, work.Right - width));
        y = Math.Min(Math.Max(y, work.Top), Math.Max(work.Top, work.Bottom - height));
    }

    /// <summary>
    /// Places the overlay at the top-center of the primary monitor's work area.
    /// The work area accounts for a docked taskbar, so the overlay never hides
    /// behind it.
    /// </summary>
    private void PositionAtTopCenterOfPrimaryMonitor()
    {
        Rect work = SystemParameters.WorkArea;
        Left = work.Left + Math.Max(0, (work.Width - ActualWidth) / 2);
        Top = work.Top;
    }
}