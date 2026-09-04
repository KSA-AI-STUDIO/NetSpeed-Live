# Installer — NetSpeed Live (Inno Setup)

`NetSpeedLive.iss` builds the branded Windows installer.

## Compiling

Requires [Inno Setup 6.3+](https://jrsoftware.org/isinfo.php) (the machine used
for this project has it installed per-user at
`%LOCALAPPDATA%\Programs\Inno Setup 6`):

```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" Installer\NetSpeedLive.iss
```

Or simply run `Build\Publish.ps1` first, then compile the script — the
installer packs the published output from
`Release\NetSpeedLive-<version>-win-x64\`.

Output: `Release\NetSpeedLive-Setup-<version>.exe`

## Behaviour

| Aspect | Behaviour |
|---|---|
| Scope | Per-user (`PrivilegesRequired=lowest`), no admin required |
| Install location | `%LOCALAPPDATA%\Programs\NetSpeed Live\` |
| Start Menu | `NetSpeed Live` shortcut (always created) |
| Desktop shortcut | Optional task (unchecked by default) |
| Launch after install | Optional checkbox on the finish page (skipped in silent installs) |
| Uninstall entry | `NetSpeed Live` in Windows Installed Apps (HKCU) |
| User settings | `%APPDATA%\NetPulseOverlay\` is **never** modified by install or uninstall |
| Startup registration | Never written by the installer — owned by the app (`StartupService`, HKCU Run) |
| Executable | `NetPulseOverlay.exe` (internal assembly name preserved); all user-facing surfaces branded **NetSpeed Live** |

## Version sync

`<Version>` in `NetPulseOverlay.csproj` is authoritative. Keep the
`#define MyAppVersion` in the `.iss` at the same value.
