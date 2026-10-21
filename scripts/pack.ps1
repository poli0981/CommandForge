#requires -Version 7
<#
.SYNOPSIS
    Publishes CommandForge self-contained and packages it with Velopack (vpk) for GitHub Releases.

.DESCRIPTION
    Produces, under Releases/:
      CommandForge-win-Setup.exe, CommandForge-win-Portable.zip, RELEASES, releases.win.json,
      *-full.nupkg, *-delta.nupkg
    Update is per-user (no UAC). The Elevator is published into the same folder by the
    PublishElevatorOnPublish MSBuild target, so vpk packages it automatically.

    vpk is pinned as a local .NET tool (.config/dotnet-tools.json) and must match the Velopack
    library version referenced by CommandForge.Infrastructure.csproj and CommandForge.Wpf.csproj.

.PARAMETER Version
    SemVer package version, e.g. 2.0.0 (no leading 'v'). Should match the git tag vX.Y.Z.

.PARAMETER SkipPack
    Publish only (no vpk pack) — used by the CI packaging smoke test when vpk is not wanted.

.EXAMPLE
    ./scripts/pack.ps1 -Version 2.0.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $Runtime = 'win-x64',

    [string] $Configuration = 'Release',

    [switch] $SkipPack
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish/$Runtime"
$wpfProject = Join-Path $repoRoot 'src/CommandForge.Wpf'

Write-Host "Publishing CommandForge $Version ($Runtime, $Configuration)..." -ForegroundColor Cyan
# Stamp the assembly version so the in-app "App version" (Settings -> About, Report Bug) matches
# the released package version. ReadyToRun precompiles the app's own assemblies for faster startup
# (the self-contained framework assemblies are already precompiled).
dotnet publish $wpfProject `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=true `
    -p:Version=$Version -p:FileVersion=$Version -p:InformationalVersion=$Version `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# The elevated helper must ship next to the app (BrokerElevationService spawns it from there).
foreach ($file in 'CommandForge.Wpf.exe', 'CommandForge.Elevator.exe', 'CommandForge.Elevator.dll') {
    if (-not (Test-Path (Join-Path $publishDir $file))) {
        throw "Publish output is missing $file"
    }
}

if ($SkipPack) {
    Write-Host "Publish OK (pack skipped). Output: $publishDir" -ForegroundColor Green
    return
}

Push-Location $repoRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed with exit code $LASTEXITCODE" }

    Write-Host "Packing with Velopack..." -ForegroundColor Cyan
    # --runtime selects native x64 Update.exe/Setup.exe helpers (vpk otherwise defaults to x86);
    # the update channel stays the default "win", so existing installs keep receiving updates.
    dotnet vpk pack `
        --runtime $Runtime `
        --packId CommandForge `
        --packTitle CommandForge `
        --packAuthors poli0981 `
        --packVersion $Version `
        --packDir $publishDir `
        --mainExe CommandForge.Wpf.exe
    if ($LASTEXITCODE -ne 0) { throw "vpk pack failed with exit code $LASTEXITCODE" }
}
finally {
    Pop-Location
}

Write-Host "Done. Artifacts are in $(Join-Path $repoRoot 'Releases')." -ForegroundColor Green
