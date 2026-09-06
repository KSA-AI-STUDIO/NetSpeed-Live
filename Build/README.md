# Build folder — Release pipeline

## Publish.ps1

Produces the distributable, **self-contained Windows** builds of
NetSpeed Live. Defaults to x64; pass `-Runtime win-x86` for the 32-bit build:

```powershell
powershell -ExecutionPolicy Bypass -File Build\Publish.ps1                  # win-x64
powershell -ExecutionPolicy Bypass -File Build\Publish.ps1 -Runtime win-x86 # win-x86
```

- Reads the version from `NetPulseOverlay.csproj` (`<Version>` is the single
  authoritative version source).
- Output: `Release\NetSpeedLive-<version>-win-x64\` or
  `Release\NetSpeedLive-<version>-win-x86\`
- Self-contained (`--self-contained true`): end users do **not** need the
  .NET Desktop Runtime.
- `PublishSingleFile=false`, `PublishReadyToRun=false`: reliability is
  prioritised over file size; no trimming is used (WPF is not trim-safe).

The folder is wiped and recreated on every run, so output is deterministic.

## Installer

The Inno Setup scripts in `Installer\` pack the published outputs:

| Script | Packages | Output |
|---|---|---|
| `Installer\NetSpeedLive.iss` | win-x64 publish | `Release\NetSpeedLive-Setup-<version>.exe` |
| `Installer\NetSpeedLive-x86.iss` | win-x86 publish | `Release\NetSpeedLive-Setup-<version>-x86.exe` |

See `Installer\README.md`.
