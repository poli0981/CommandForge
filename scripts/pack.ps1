#requires -Version 7
<#
.SYNOPSIS
    Publishes CommandForge self-contained and packages it with Velopack (vpk) for GitHub Releases.

.DESCRIPTION
    Phase 4 release helper (no CI — that is Phase 6). Produces, under Releases/:
      CommandForge-win-Setup.exe, RELEASES, *-full.nupkg, *-delta.nupkg
    Update is per-user (no UAC). The Elevator is copied into the publish dir by the
    CopyElevatorOnPublish MSBuild target, so vpk packages it automatically.

.PARAMETER Version
    SemVer package version, e.g. 0.3.0 (no leading 'v'). Should match the git tag vX.Y.Z.

.EXAMPLE
    ./scripts/pack.ps1 -Version 0.3.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $Runtime = 'win-x64',

    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish/$Runtime"
$wpfProject = Join-Path $repoRoot 'src/CommandForge.Wpf'

Write-Host "Publishing CommandForge $Version ($Runtime, $Configuration)..." -ForegroundColor Cyan
dotnet publish $wpfProject `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=false -o $publishDir

# vpk is the Velopack CLI: dotnet tool install -g vpk
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    throw "vpk not found. Install it with: dotnet tool install -g vpk"
}

Write-Host "Packing with Velopack..." -ForegroundColor Cyan
vpk pack `
    --packId CommandForge `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe CommandForge.exe

Write-Host "Done. Artifacts are in $(Join-Path $repoRoot 'Releases')." -ForegroundColor Green
