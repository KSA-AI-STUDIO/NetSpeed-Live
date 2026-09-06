# NetSpeed Live v1.1.0

Real-time download and upload speed overlay for Windows — compact, always-on-top,
unobtrusive.

- **Real-time network speed** shown as a compact overlay, updated ~1 s.
- **Download-first** display, matching how you read network speeds:
  `⬇ Download → ⬆ Upload` (horizontal) or download on top (vertical).
- Two layouts — Horizontal and Vertical.
- Display modes — Both / Only Upload / Only Download, with no empty gap.
- Custom appearance — font, size, weight, colour and opacity.
- System tray control, click-through mode, and **Start with Windows**.
- About window and optional **UPI donation** (offline QR generation).
- **Signal Radar** application icon across the executable, tray, installer and
  shortcuts.

## Highlights

- **Download first, upload second** in every layout and mode.
- Compact, draggable, always-on-top overlay with position persistence and
  multi-monitor-safe restoring.
- Truly per-user installer — no administrator privileges required.
- Self-contained — the installer bundles the .NET runtime; no separate install.
- **Both Windows architectures**: x64 and 32-bit (x86) installers.

## Installation

1. Download the installer for your system:
   - `NetSpeedLive-Setup-1.1.0.exe` — **64-bit (x64)** Windows.
   - `NetSpeedLive-Setup-1.1.0-x86.exe` — **32-bit (x86)** Windows.
2. Run the installer and follow the wizard (per-user install, no admin needed).
3. Launch **NetSpeed Live** from the Start Menu, the installer's finish page, or
   the optional desktop shortcut.
4. Uninstall from **Windows Settings → Apps → Installed apps → NetSpeed Live**
   (your settings are preserved).

> The (optional) portable package `NetSpeedLive-1.1.0-win-x64.zip` contains the
> self-contained build if you prefer to run without the installer.

## System requirements

- **64-bit Windows** (x64).
- **Windows 10 or Windows 11**.
- No .NET run-time requirements — the installer includes everything needed.

## What's new in 1.1.0

- Download-first display order (Horizontal and Vertical).
- Multiple-resolution **Signal Radar** application icon (executable, tray,
  installer, shortcuts).
- Self-contained Windows x64 release pipeline and a per-user Inno Setup
  installer (Start Menu shortcut, optional desktop shortcut, launch after
  install, clean uninstall).
- (Earlier in 1.x) vertical layout, display modes, appearance customization,
  WPF About window, and optional UPI donation with offline QR.

## Known notes

- The 1.1.0 installer is currently **not digitally signed**. On Windows you may
  see a SmartScreen / reputation prompt ("Windows protected your PC").
  See `Build/CODE-SIGNING.md` for the future signing path.
- Donations are optional and processed through UPI, not through Microsoft.

---

**KSA AI STUDIO** · © 2026 KSA AI STUDIO