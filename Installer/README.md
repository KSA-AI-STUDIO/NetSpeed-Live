# Installer — NetSpeed Live (Inno Setup)

Two Inno Setup scripts build the branded Windows installers:

| Script | Target | Output |
|---|---|---|
| `NetSpeedLive.iss` | x64 (`win-x64` publish) | `NetSpeedLive-Setup-<version>.exe` |
| `NetSpeedLive-x86.iss` | x86 (`win-x86` publish) | `NetSpeedLive-Setup-<version>-x86.exe` |

## Compiling

Requires [Inno Setup 6.3+](https://jrsoftware.org/isinfo.php) (the machine used
for this project has it installed per-user at
`%LOCALAPPDATA%\Programs\Inno Setup 6`):

```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" Installer\NetSpeedLive.iss
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" Installer\NetSpeedLive-x86.iss
```

Or simply run `Build\Publish.ps1` (optionally `-Runtime win-x86`) first, then
compile the script — each installer packs the published output from
`Release\NetSpeedLive-<version>-win-x64\` / `-win-x86\`.

## Behaviour

| Aspect | Behaviour |
|---|---|
| Scope | Per-user (`PrivilegesRequired=lowest`), no admin required |
| Install location | `%LOCALAPPDATA%\Programs\NetSpeed Live\` |
| Start Menu | `NetSpeed Live` shortcut (always created) |
| Desktop shortcut | Optional task (unchecked by default) |
| Launch after install | Optional checkbox on the finish page (skipped in silent installs) |
| Uninstall entry | `NetSpeed Live` (x64) / `NetSpeed Live (x86)` in Windows Installed Apps (HKCU) |
| User settings | `%APPDATA%\NetPulseOverlay\` is **never** modified by install or uninstall |
| Startup registration | Never written by the installer — owned by the app (`StartupService`, HKCU Run) |
| Executable | `NetPulseOverlay.exe` (internal assembly name preserved); all user-facing surfaces branded **NetSpeed Live** |

Architecture notes:

- Both scripts use **distinct `AppId`s**, so the two architectures have
  independent uninstall identities and never collide.
- The x86 script uses `ArchitecturesAllowed=x86compatible` — the documented
  default for a 32-bit Setup program, which runs on 32-bit Windows and on x64
  Windows via WOW64. The x86 payload is genuine x86 throughout (verified via
  PE machine types).

## Version sync

`<Version>` in `NetPulseOverlay.csproj` is authoritative. Keep the
`#define MyAppVersion` in both `.iss` files at the same value.
