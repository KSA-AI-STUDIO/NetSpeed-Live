# GitHub Release Checklist — NetSpeed Live v1.1.0

Preparation list only. **No external publishing is performed from this
repository without explicit approval.**

## Before release

- [ ] Confirm version number is `1.1.0` everywhere (csproj, `.iss`, release
      folder, installer filename, release notes, README, CHANGELOG).
- [ ] Clean Release build: 0 warnings, 0 errors.
- [ ] `Build\Publish.ps1` succeeds; `Release\NetSpeedLive-1.1.0-win-x64\`
      contains the app + self-contained runtime.
- [ ] Installer compiled; `Release\NetSpeedLive-Setup-1.1.0.exe` exists.
- [ ] Installer SHA-256 verified against
      `Release\NetSpeedLive-Setup-1.1.0.exe.sha256`.
- [ ] Release notes reviewed (`Release\RELEASE-NOTES-1.1.0.md`).
- [ ] README reviewed (screenshots resolve; features accurate).
- [ ] Screenshots reviewed (`Assets\Screenshots\`).
- [ ] Code-signing status reviewed (`Build\CODE-SIGNING.md`).
- [ ] Test install (per-user) on a clean Windows account.
- [ ] Test launch of the installed app (overlay + tray + clean exit).
- [ ] Test uninstall (files removed, settings preserved).

## GitHub release

- [ ] Create tag `v1.1.0`.
- [ ] Create a release titled **NetSpeed Live v1.1.0**.
- [ ] Paste release notes (`RELEASE-NOTES-1.1.0.md`).
- [ ] Upload `NetSpeedLive-Setup-1.1.0.exe`.
- [ ] Upload `NetSpeedLive-Setup-1.1.0.exe.sha256`.
- [ ] (Optional) Upload `NetSpeedLive-1.1.0-win-x64.zip` + its `.sha256`.
- [ ] Publish release.

## After release

- [ ] Download the installer from the published release.
- [ ] Verify the SHA-256 of the downloaded file.
- [ ] Test installation from the downloaded asset (fresh machine if possible).
- [ ] Test launch, tray icon and clean exit on the installed copy.

> All of the above pre-release checks have been performed locally; the
> *GitHub release* and *After release* sections require repository/network
> actions and are **not** performed automatically.