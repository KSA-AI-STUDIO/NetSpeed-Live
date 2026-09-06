# Changelog

All notable changes to NetSpeed Live are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.0] — 2026-09-03 (Stage 16B — Traffic Color Update)

### Changed
- **Traffic colors updated**: the download arrow is now **green**
  (`#4ADE80`→`#16A34A`) and the upload arrow is now **orange**
  (`#FB923C`→`#EA580C`), in both the Signal Radar icon and all regenerated
  release assets. Download remains **first** (left / top) and upload
  **second** (right / bottom) — arrow shapes, directions, order, radar motif
  and background are unchanged. The overlay itself is unaffected (its text
  colour is a single user setting, not per-direction colours).

### Changed (assets)
- `Assets/app.ico` and `Assets/IconSource/NetSpeedLive-Icon-Master.png`
  regenerated from the updated design; all nine ICO frames preserved
  (16–48 px DIB, 64–256 px PNG); 16×16 legibility re-verified.
- Release binaries, installers and checksums for **both** win-x64 and win-x86
  rebuilt; portable ZIP regenerated.

## [1.1.0] — 2026-09-03 (Stage 15 — Public Release Preparation & Distribution)

### Added
- **Release assets** under `Release/`:
  - `RELEASE-NOTES-1.1.0.md` — versioned, GitHub-ready release notes
    (highlights, peak features, installation, system requirements, what's-new,
    known notes).
  - `GITHUB-RELEASE-CHECKLIST.md` — before/release/after checklist for the
    future v1.1.0 GitHub release.
  - `NetSpeedLive-Setup-1.1.0.exe.sha256` — SHA-256 checksum for the installer.
  - `NetSpeedLive-1.1.0-win-x64.zip` (+ `.sha256`) — optional portable package
    of the self-contained build for advanced users; clearly a manual/portable
    option (the installer remains the primary artifact).
- **Screenshots** under `Assets/Screenshots/`: `overlay-horizontal.png`,
  `overlay-vertical.png`, `context-menu.png`, `about-window.png`,
  `donation-window.png` — captured from the real running application on a
  clean, non-personal dark backdrop; integrated into the README with
  repository-relative paths.
- **`Build/CODE-SIGNING.md`** — code-signing readiness analysis: installer is
  currently unsigned (`Get-AuthenticodeSignature` → NotSigned), why signing
  matters, future options, and a future signing pipeline (build → publish →
  installer → sign → timestamp → verify → checksum → release).
- **`LICENSE-STATUS.md`** — states that no open-source license has been
  selected yet; distribution is currently proprietary © 2026 KSA AI STUDIO.
- **README rewritten** for a public GitHub audience: overview (download-first),
  screenshots, features, installation, usage, settings & controls, start with
  Windows, building from source, release verification, privacy, donations,
  license, credits.

### Changed
- README structure expanded from build-focused to end-user + contributor
  focused (accurate, no invented features).

### Verified
- SHA-256 checksums generated with `Get-FileHash` and re-computed to confirm
  they match the installer and the ZIP.
- Version consistency audit: `1.1.0` aligned across `NetPulseOverlay.csproj`,
  `Installer/NetSpeedLive.iss`, publish folder name, installer filename,
  release notes, CHANGELOG and README.
- Code-signing inspection: installer is **unsigned** (Status: NotSigned).
- Git inspection: the project folder is **not** a Git repository and has no
  remote; **no external publishing was performed**.
- Clean Release build: 0 warnings, 0 errors.
- Gross UI authenticity of screenshots confirmed by the capture pipeline; exact
  pixel verification was limited by unavailable image rendering during this
  stage, so the honest statement is that screenshots were captured from the
  real app and integrated, not individually pixel-audited.

## [1.1.0] — 2026-09-03 (Stage 14 — Release Packaging & Windows Installer)

### Added
- **Repeatable production publish process** (`Build/Publish.ps1`): wipes and
  recreates `Release/NetSpeedLive-<version>-win-x64/`, reading the version from
  `NetPulseOverlay.csproj` (single version source). Publishes
  **self-contained win-x64** (`PublishSingleFile=false`,
  `PublishReadyToRun=false`, no trimming) so end users do not need the .NET
  Desktop Runtime; verifies the output (exe + host runtime files) and reports
  success/failure.
- **Inno Setup installer source** (`Installer/NetSpeedLive.iss`), branded
  **NetSpeed Live / KSA AI STUDIO / © 2026** with the Signal Radar icon
  (`Assets/app.ico`) on the setup executable, uninstall entry and shortcuts:
  - per-user install to `%LOCALAPPDATA%\Programs\NetSpeed Live\` (no admin);
  - Start Menu shortcut (always) and an **optional** desktop shortcut task;
  - optional **launch after installation** (finish page, skipped in silent
    installs);
  - uninstaller removes the application files and installer-created shortcuts;
    **user settings under `%APPDATA%\NetPulseOverlay\` are preserved**;
  - never writes a startup registry entry — "Start with Windows" stays under
    the application's control (`StartupService`, HKCU Run).
- `Build/README.md` and `Installer/README.md` documenting the pipeline.

### Verified
- Release build: 0 warnings, 0 errors.
- Publish: `Release/NetSpeedLive-1.1.0-win-x64/` — 465 files, ~160 MB,
  self-contained host runtime files present.
- Installer: `Release/NetSpeedLive-Setup-1.1.0.exe` (48.8 MB) compiled with
  correct version metadata (Product "NetSpeed Live" 1.1.0, KSA AI STUDIO).
- Silent install completed (exit 0): all files installed, Start Menu shortcut
  and desktop shortcut (task enabled) created, per-user uninstall entry
  present in Installed Apps.
- Installed application launched successfully (self-contained runtime), the
  overlay appeared with the verified download-first layout (296×37), the
  Signal Radar tray icon appeared, and the application exited cleanly.
- Silent uninstall completed (exit 0): install directory, both shortcuts and
  the uninstall registry entry removed; `%APPDATA%\NetPulseOverlay\settings.json`
  preserved.

## [1.1.0] — 2026-09-03 (Stage 13 — Icon Production & Integration)

### Added
- **Signal Radar application icon** (Concept 7): deep-navy rounded-square tile
  with cyan radar arcs and a centre dot (live monitoring) in the upper portion,
  a green **download arrow first on the left**, and an orange **upload arrow
  second on the right** — matching the app's download-first display order.
- `Assets/IconSource/` source assets: `NetSpeedLive-Icon.svg` (vector master)
  and `NetSpeedLive-Icon-Master.png` (256×256 raster master).
- **Multi-resolution `Assets/app.ico`** with 9 embedded frames:
  16/20/24/32/40/48 px as 32-bit BGRA DIB frames, 64/128/256 px as
  PNG-compressed frames. Sub-32 px renders simplify the radar to one arc + dot
  and thicken the arrows so the 16×16 tray size stays legible.

### Changed
- `TrayService.LoadAppIcon()` now requests the frame at the shell's
  small-icon size (`SystemInformation.SmallIconSize`) so the tray renders the
  native 16×16 (or 32×32 at 200% DPI) frame instead of downscaling the 32×32
  default frame. Loading path (pack URI `Assets/app.ico` with a system-icon
  fallback) is otherwise unchanged.

### Verified
- Release build: 0 warnings, 0 errors.
- ICO header parsed: reserved=0, type=1, 9 frames at the intended sizes and
  formats (DIB ≤48 px, PNG ≥64 px); exe icon extracted and visually confirmed
  as the Signal Radar design.
- Runtime (Release): application launches with no startup crash; the tray
  icon appears (found via the Windows 11 "Show Hidden Icons" overflow flyout)
  and a screen capture of the live tray icon visually matches the design;
  embedded tray resource confirmed at `assets/app.ico` inside
  `NetPulseOverlay.g.resources`; clean exit via the overlay context menu.

## [1.1.0] — 2026-08-31 (Stage 11 completion)

### Added
- **Layout Orientation** submenu (Horizontal / Vertical) with a persisted
  `DisplayOrientation` setting (default Horizontal). Both orientations reuse the
  same four TextBlocks by rearranging grid rows/columns; supported in every
  display mode (Both / Only Upload / Only Download).
- **WPF About window wiring**: the About entry in both the tray menu and the
  overlay context menu now open `Views/AboutWindow` (assembly metadata read
  dynamically + Donate via UPI). The old WinForms `Services/AboutDialog.cs` was
  removed (zero references after rewiring).
- **Default layout flash fix**: the XAML grid now starts in the default
  Horizontal orientation (one row, four columns) so no vertical flash occurs
  before `ApplyDisplayOrientation()` runs.
- Changelog + README rewritten for NetSpeed Live.

### Changed
- **Display order changed to download-first**: the overlay now shows
  `↓ Download` before `↑ Upload` in both orientations (Horizontal:
  `↓ 14.82 MB/s  ↑ 2.35 MB/s`; Vertical: download row on top). Monitoring
  logic and upload/download data mapping are unchanged; only the visual
  placement of the existing four TextBlocks moved. Collapsed single-direction
  modes leave no empty gap (hidden column pairs get zero reserved width).
- Renamed `Views/DonateWindow.xaml(.cs)` → `Views/DonationWindow.xaml(.cs)` so
  the file name matches the `DonationWindow` class (`x:Class` updated to match).
  No stale `DonateWindow` references remain in source or docs.
- Corrected stale XML doc comments (About dialog title reference updated to
  "About NetSpeed Live"; removed references to the deleted CharacterSpacing
  feature). README project structure reflects `DonationWindow.xaml(.cs)`.
- **Character Spacing submenu removed** (WPF `TextBlock` has no
  `CharacterSpacing` property; the Tight/Normal options were non-functional).
  Dead code (`SetCharacterSpacing`, `ParseCharacterSpacing`, the setting
  property) was removed. Rendering uses natural WPF font spacing with no
  artificial tracking. Existing `settings.json` files containing an orphaned
  `CharacterSpacing` value load fine (unknown properties are ignored).
- **About entry** in the overlay context menu updated to "About NetSpeed Live".

### Fixed
- **About/Donation feature is now reachable** — previously the new AboutWindow
  and DonationWindow were implemented but the running app still used the old
  AboutDialog, so the Donate via UPI section was unreachable.

### Verified (automated UIA runtime pass, Release build)
- **Orientation switching works in both directions** via the overlay context
  menu: Horizontal (296×37) ↔ Vertical (152×69); geometry changes immediately,
  `DisplayOrientation` persists to `settings.json`, and is restored correctly
  on app restart in both directions.
- **About window** opens from the overlay context menu (475×447) and exposes
  the Donate via UPI entry.
- **Donation window** opens from the About dialog (400×669 with the offline
  QRCoder-generated QR code rendered on load).
- **Copy UPI ID** copies `ashutosh.praharaj1@ybl` to the clipboard, switches
  the button label to "Copied!" with the button disabled for 1.5 s, then
  reverts to "Copy UPI ID" (confirmed via UIA Name + IsEnabled properties).
- **Clean exit** terminates the process with no orphaned instances.
- Release build compiles with 0 warnings and 0 errors (.NET 8.0.424).

## [1.1.0] — 2026-08-31 (Stage 11 — Display Appearance & Content Customization)

### Added
- **Vertical two-row overlay layout**: upload and download speeds now display one
  above the other (`↑ 2.35 MB/s` / `↓ 14.82 MB/s`) instead of a single horizontal line.
- **Display Mode** submenu (Both / Upload Only / Download Only) with persisted selection;
  hidden rows collapse cleanly and the overlay re-clamps into the work area.
- **Text Colour** submenu: White (default), Green, Blue, Yellow, Red, Cyan — readable
  shades, persisted, applied to the speed text and drop shadow.
- **Font Family** submenu: Segoe UI (default), Segoe UI Semibold, Arial, Calibri,
  Consolas, Cascadia Mono — persisted, with safe fallback to Segoe UI when unavailable.
- **Font Weight** submenu: Light / Regular / SemiBold / Bold — persisted.
- **Donate via UPI**: About dialog gained a Support Development section with a
  Donate button that opens a donation dialog showing a locally generated UPI QR code
  (QRCoder, offline — no network use), the UPI ID, a Copy UPI ID button with
  "Copied!" feedback, and a Close button.

### Changed
- **Public branding**: NetPulse Overlay → **NetSpeed Live** (window title, tray tooltip,
  tray menu, About dialog, assembly/product metadata, README). Internal identifiers
  intentionally remain `NetPulseOverlay` for build stability.
- **Version** 1.0.0 → **1.1.0** (Product Version 1.1.0, File/Assembly Version 1.1.0.0).
- **Contact email** → `ksaaistudio@outlook.com`; the old address
  was removed from all active source and metadata.
- **Typography**: natural font spacing by default; the artificial double-spaced
  `  /  ` separator is gone with the horizontal layout.

### Fixed
- **Layout fluctuation**: the arrow column and the speed-value column now have fixed
  measured widths (via `FormattedText`), so ordinary speed updates swap text only —
  the overlay no longer re-sizes or shifts horizontally as values or units change.

## [1.0.0] — Stages 1–10

### Added
- Real-time per-adapter network monitor (1 s deltas, filtering, churn safety).
- Borderless always-on-top transparent overlay with drag positioning and
  monitor-configuration-safe position restore.
- System tray integration: Show/Hide Overlay, Start with Windows (HKCU Run,
  dev-guarded), Click-Through toggle, About, Exit; close-to-tray behaviour.
- Click-through mode (WS_EX_TRANSPARENT toggle preserving WPF layering).
- Overlay context menu: Font Size (Small/Medium/Large), Opacity (50/75/100%),
  Click-Through toggle, Hide Overlay, About, Exit — all persisted.
- About dialog reading dynamic assembly metadata.
