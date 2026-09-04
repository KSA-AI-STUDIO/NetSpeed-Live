namespace NetPulseOverlay.Models;

/// <summary>
/// Persisted application settings. Serialized as JSON by
/// <see cref="Services.SettingsService"/>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Last overlay X position in WPF DIPs; <c>null</c> until a position has
    /// been placed (by the user dragging or by the top-center fallback).
    /// </summary>
    public double? OverlayX { get; set; }

    /// <summary>
    /// Last overlay Y position in WPF DIPs; <c>null</c> until a position has
    /// been placed.
    /// </summary>
    public double? OverlayY { get; set; }

    /// <summary>Whether mouse input passes through the overlay (Stage 7).</summary>
    public bool ClickThroughEnabled { get; set; }

    /// <summary>Whether the app is registered to start with Windows (Stage 8).</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Overlay font size in DIPs.</summary>
    public double FontSize { get; set; } = 19;

    /// <summary>Overlay opacity, clamped to [0.35, 1] when applied (Stage 9).</summary>
    public double Opacity { get; set; } = 1.0;

    // ─── Stage 11 — display appearance & content customization ───
    // All new values are stored as strings for robust JSON round-tripping and
    // parsed with fallbacks, so missing/corrupt values always resolve to the
    // safe defaults below without ever crashing.

    /// <summary>Display mode: "Both", "Only Upload" or "Only Download".</summary>
    public string DisplayMode { get; set; } = "Both";

    /// <summary>Layout orientation: "Horizontal" (single line) or "Vertical" (two rows).</summary>
    public string DisplayOrientation { get; set; } = "Horizontal";

    /// <summary>Text colour: "White", "Red", "Green", "Blue", "Yellow" or "Cyan".</summary>
    public string TextColour { get; set; } = "White";

    /// <summary>Font family name (curated list from the context menu).</summary>
    public string FontFamilyName { get; set; } = "Segoe UI";

    /// <summary>Font weight: "Light", "Regular", "SemiBold" or "Bold".</summary>
    public string FontWeight { get; set; } = "Regular";
}