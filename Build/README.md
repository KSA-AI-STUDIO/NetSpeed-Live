# Build folder — Release pipeline

## Publish.ps1

Produces the distributable, **self-contained Windows x64** build of
NetSpeed Live:

```powershell
powershell -ExecutionPolicy Bypass -File Build\Publish.ps1
```

- Reads the version from `NetPulseOverlay.csproj` (`<Version>` is the single
  authoritative version source).
- Output: `Release\NetSpeedLive-<version>-win-x64\`
- Self-contained (`--self-contained true`): end users do **not** need the
  .NET Desktop Runtime.
- `PublishSingleFile=false`, `PublishReadyToRun=false`: reliability is
  prioritised over file size; no trimming is used (WPF is not trim-safe).

The folder is wiped and recreated on every run, so output is deterministic.

## Installer

The Inno Setup script in `Installer\` packs the published output into
`Release\NetSpeedLive-Setup-<version>.exe`. See `Installer\README.md`.
