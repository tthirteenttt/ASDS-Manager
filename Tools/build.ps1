<#
.SYNOPSIS
    Build ASDSManager.dll and optionally deploy GameData/ASDSManager to a KSP instance.
#>

param(
    [string]$Configuration = "Release",
    [string]$TargetKSP = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceDir = Join-Path $projectRoot "Source"
$gameDataSrc = Join-Path $projectRoot "GameData\ASDSManager"
$csproj = Join-Path $sourceDir "ASDSManager.csproj"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found at: $msbuild"
}

Write-Host "[1/2] Building ASDSManager ($Configuration) against KSP: $TargetKSP ..." -ForegroundColor Yellow
& $msbuild $csproj "/p:Configuration=$Configuration" "/p:KSP_DIR=$TargetKSP" "/nologo" "/verbosity:minimal"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
}
Write-Host "[1/2] Build succeeded." -ForegroundColor Green

if (Test-Path $TargetKSP) {
    $targetGameData = Join-Path $TargetKSP "GameData\ASDSManager"
    New-Item -ItemType Directory -Path $targetGameData -Force | Out-Null
    Copy-Item -Path "$gameDataSrc\*" -Destination $targetGameData -Recurse -Force
    Write-Host "[2/2] Deployed to $targetGameData" -ForegroundColor Green
} else {
    Write-Warning "Target KSP path not found ($TargetKSP); skipped deployment."
}
