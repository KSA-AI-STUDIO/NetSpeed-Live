# NetSpeed Live — Project Context

> Public product name: **NetSpeed Live** · Internal identifier: **NetPulseOverlay** (kept for
> build stability; do not rename namespaces/folders/assembly unless explicitly asked).
> Company: **KSA AI STUDIO** · Founder: **Ashutosh Praharaj** · Contact: **ksaaistudio@outlook.com**

## 1. What this project is

A lightweight, always-on-top Windows desktop overlay that shows real-time network
upload/download speed (vertically, one row per direction), built with **C# / .NET 8 / WPF**.
It lives in the system tray, supports drag positioning, click-through, opacity/typography
customization, autostart, and a UPI donation dialog with a locally generated QR code.

## 2. Technology baseline

| Aspect | Value |
|---|---|
| Framework | .NET 8 (`net8.0-windows`), WPF (`UseWPF`) |
| Tray stack | WinForms `NotifyIcon` + `ContextMenuStrip` (`UseWindowsForms` is enabled *only* for this) |
| NuGet dependencies | `QRCoder` (offline UPI QR generation — no network use at runtime) |
| Settings | JSON at `%APPDATA%\NetPulseOverlay\settings.json` (atomic temp+move writes) |
| Autostart | `HKCU\...\CurrentVersion\Run` value `NetPulseOverlay` (per-user, no admin) |
| Build SDK | .NET 8 SDK installed **user-locally** at `%USERPROFILE%\.dotnet` (system-PATH dotnet has no SDK). Use `& "$env:USERPROFILE\.dotnet\dotnet.exe" build …`; set `DOTNET_ROOT` only for direct exe launches of framework-dependent builds |
| .git | Local Git repository (initialized for public release preparation). No remote is added unless the owner authorizes publishing |

## 3. Architecture (verified from source)

```
App.xaml.cs  (composition root; owns service lifetimes; ShutdownMode=OnExplicitShutdown)
    ├── NetworkMonitorService   1 s timer → per-adapter byte deltas (GetIPStatistics),
    │     virtual/loopback/tunnel filtering, zero-clamped deltas, adapter-churn safe,
    │     exception-wrapped ticks; raises NetworkSpeedSample on a thread-pool thread
    ├── SettingsService          JSON load/save; atomic; corrupt → defaults; never throws
    ├── StartupService           HKCU Run registration; bin-output dev guard; never throws
    ├── TrayService              NotifyIcon menu: Show/Hide Overlay, Start with Windows,
    │     Click-Through, About NetSpeed Live (opens AboutWindow), Exit
    └── Views/OverlayWindow      borderless topmost transparent overlay
          ├── grid: single-row (Horizontal default) or two-row (Vertical)
          │   layout with a fixed arrow column + fixed-width value column
          │   (measured via FormattedText; recomputed only on appearance
          │   changes, never on speed ticks → no layout fluctuation)
          ├── context menu: Display Mode / Layout Orientation / Text Colour /
          │   Font Family / Font Size / Font Weight / Opacity / Click-Through /
          │   Hide / About / Exit (checkmarks synced on ContextMenuOpening)
          ├── drag (MouseLeftButtonDown → DragMove; blocked while click-through)
          └── click-through via NativeMethods (GWL_EXSTYLE WS_EX_TRANSPARENT toggle
              that preserves WS_EX_LAYERED → transparency/topmost unaffected)
    Views/AboutWindow            About dialog (assembly metadata + Donate via UPI;
                                opened by both tray About and overlay context-menu About)
    Views/DonationWindow         UPI donation dialog (QR + Copy UPI ID + Close)
```

**Event flow (no duplicated logic):** OverlayWindow raises `ClickThroughToggled`,
`HideRequested`, `AboutRequested`, `ExitRequested`; App applies them through the same
lifecycle paths the tray uses (`SetClickThrough` persists + applies, `HideOverlay` keeps
the monitor running, `ExitApplication` → `Shutdown()`; `OnExit` disposes tray → monitor
→ settings in dependency order, never throws).

## 4. Settings model (`AppSettings`)

`OverlayX`/`OverlayY` (double?, DIPs) · `ClickThroughEnabled` · `StartWithWindows` ·
`FontSize` (double, default 19) · `Opacity` (double, default 1.0, clamped [0.35, 1]) ·
`DisplayMode` ("Both" | "Upload Only" | "Download Only", default Both) ·
`DisplayOrientation` ("Horizontal" | "Vertical", default Horizontal) ·
`TextColour` ("White"/"Green"/"Blue"/"Yellow"/"Red"/"Cyan", default White) ·
`FontFamilyName` (default "Segoe UI") · `FontWeight` ("Light"/"Regular"/"SemiBold"/"Bold").

Backward compatible: older settings.json files load fine; missing properties fall back to
initializer defaults; corrupt files yield defaults.

## 5. Verification conventions used in this project

Runtime verification is done with a PowerShell harness: `System.Windows.Automation`
(`AutomationElement.FromHandle`) for overlay text/menus, Win32 `EnumWindows`/`GetWindowRect`
for geometry, injected mouse input for drag/menu interactions, `WindowFromPoint` for
click-through hit-testing, and direct registry/file reads for persistence checks.

## 6. Stage history

| Stage | Scope | State |
|---|---|---|
| 1–5 | Project, tray, monitor, live text, drag + position persistence | Done, verified |
| 6–8 | Tray menu, click-through, Start with Windows (real registry) | Done, verified |
| 9 | Overlay context menu (font size, opacity, click-through, hide, about, exit) | Done, verified |
| 10 | Release-quality verification + self-contained publish | Done, verified |
| 11 | Display appearance & content customization; NetSpeed Live branding; UPI donation | In progress |

## 7. Known conventions / constraints

* Public name is **NetSpeed Live**; internal identifiers remain **NetPulseOverlay**.
  Never rename namespaces/folders/assembly casually.
* Do not place the overlay inside the Windows taskbar/system tray — permanently dropped.
* The tray icon belongs to the notification area; it is not a taskbar widget.
* No telemetry, no cloud services, no runtime network use except the speed monitor itself.
* Donation feature must stay isolated and easy to review for future Store packaging.
