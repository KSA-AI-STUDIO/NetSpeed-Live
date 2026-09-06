# NetSpeed Live production publish script.
#
# Produces a self-contained Windows build so end users do not need the .NET
# Desktop Runtime installed. Defaults to the primary x64 target; pass
# -Runtime win-x86 to produce a 32-bit x86 build. Version is read from
# NetPulseOverlay.csproj, which is the single authoritative version source.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File Build\Publish.ps1            # win-x64
#   powershell -ExecutionPolicy Bypass -File Build\Publish.ps1 -Runtime win-x86
param(
    [string]$Configuration = 'Release',
    [ValidateSet('win-x64', 'win-x86')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# Prefer the portable per-user SDK; fall back to dotnet on PATH.
$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

# Version source of truth: <Version> in the .csproj.
$csprojPath = Join-Path $root 'NetPulseOverlay.csproj'
$match = Select-String -Path $csprojPath -Pattern '<Version>([^<]+)</Version>'
if (-not $match) { Write-Error 'Could not read <Version> from NetPulseOverlay.csproj'; exit 1 }
$version = $match.Matches[0].Groups[1].Value

$outDir = Join-Path $root "Release\NetSpeedLive-$version-$Runtime"

Write-Output "NetSpeed Live $version ($Configuration, $Runtime, self-contained)"
Write-Output "Publish output: $outDir"

# Recreate the output folder safely.
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

& $dotnet publish (Join-Path $root 'NetPulseOverlay.csproj') `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -o $outDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish FAILED (dotnet exit code $LASTEXITCODE)"
    exit 1
}

# Sanity checks: the app executable and the self-contained host runtime files.
$exe = Join-Path $outDir 'NetPulseOverlay.exe'
if (-not (Test-Path $exe)) { Write-Error 'Publish output missing NetPulseOverlay.exe'; exit 1 }
$required = 'NetPulseOverlay.dll', 'hostfxr.dll', 'coreclr.dll', 'wpfgfx_cor3.dll', 'PresentationNative_cor3.dll'
foreach ($f in $required) {
    if (-not (Test-Path (Join-Path $outDir $f))) {
        Write-Error "Publish output missing $f (self-contained output incomplete)"
        exit 1
    }
}

$fileCount = (Get-ChildItem $outDir -Recurse -File | Measure-Object).Count
$sizeMb = [math]::Round((Get-ChildItem $outDir -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Output "Publish OK: $fileCount files, $sizeMb MB ($Runtime)"
Write-Output "SUCCESS"