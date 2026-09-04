using System;
using System.Runtime.InteropServices;

namespace NetPulseOverlay.Helpers;

/// <summary>
/// Win32 interop declarations used by the overlay window.
/// </summary>
internal static class NativeMethods
{
    /// <summary>Window-style index for Get/SetWindowLong: extended styles.</summary>
    public const int GWL_EXSTYLE = -20;

    /// <summary>Extended style: mouse input passes through the window.</summary>
    public const int WS_EX_TRANSPARENT = 0x00000020;

    /// <summary>
    /// Extended style: layered window (per-pixel alpha). WPF sets this itself
    /// for AllowsTransparency windows — it must never be removed, or WPF
    /// transparency breaks.
    /// </summary>
    public const int WS_EX_LAYERED = 0x00080000;

    // GWL_EXSTYLE holds a 32-bit value, so the 32-bit Get/SetWindowLong APIs are
    // correct on both x86 and x64; the W variants are bound explicitly.
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>
    /// Toggles click-through on a window. Enabling adds <see cref="WS_EX_TRANSPARENT"/>
    /// (plus <see cref="WS_EX_LAYERED"/>, which WPF already sets for transparent
    /// windows); disabling removes ONLY <see cref="WS_EX_TRANSPARENT"/>, leaving
    /// WPF's own layering — and therefore transparency and topmost — untouched.
    /// </summary>
    public static void SetClickThrough(IntPtr hWnd, bool enabled)
    {
        int style = GetWindowLong(hWnd, GWL_EXSTYLE);
        int newStyle = enabled
            ? style | WS_EX_TRANSPARENT | WS_EX_LAYERED
            : style & ~WS_EX_TRANSPARENT;
        SetWindowLong(hWnd, GWL_EXSTYLE, newStyle);
    }
}