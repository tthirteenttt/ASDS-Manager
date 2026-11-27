<#
.SYNOPSIS
    Build ASDSManager.dll, optionally deploy GameData/ASDSManager to a KSP instance,
    and optionally package a clean CKAN-compliant release ZIP.
#>

param(
    [string]$Configuration = "Release",
    [string]$TargetKSP = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program",
    [switch]$PackageRelease,
    [string]$Version = "1.0.1",
    [string]$OutputDir = ""
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

Write-Host "[1/3] Building ASDSManager ($Configuration) against KSP: $TargetKSP ..." -ForegroundColor Yellow
& $msbuild $csproj "/p:Configuration=$Configuration" "/p:KSP_DIR=$TargetKSP" "/nologo" "/verbosity:minimal"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
}
Write-Host "[1/3] Build succeeded." -ForegroundColor Green

if (Test-Path $TargetKSP) {
    $targetGameData = Join-Path $TargetKSP "GameData\ASDSManager"
    New-Item -ItemType Directory -Path $targetGameData -Force | Out-Null
    Copy-Item -Path "$gameDataSrc\*" -Destination $targetGameData -Recurse -Force
    Write-Host "[2/3] Deployed to $targetGameData" -ForegroundColor Green
} else {
    Write-Warning "Target KSP path not found ($TargetKSP); skipped deployment."
}

if ($PackageRelease) {
    if ([string]::IsNullOrWhiteSpace($OutputDir)) {
        $OutputDir = Join-Path $projectRoot "Build"
    }
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

    $zipPath = Join-Path $OutputDir "ASDS-Manager-v$Version.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    $stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ASDSManager_pkg_" + [System.Guid]::NewGuid().ToString("N"))
    $stagingMod = Join-Path $stagingRoot "GameData\ASDSManager"
    New-Item -ItemType Directory -Path $stagingMod -Force | Out-Null

    # Copy strictly GameData/ASDSManager contents + LICENSE & README inside GameData/ASDSManager
    Copy-Item -Path "$gameDataSrc\*" -Destination $stagingMod -Recurse -Force
    foreach ($doc in @("LICENSE", "README.md", "CHANGELOG.md")) {
        $docPath = Join-Path $projectRoot $doc
        if (Test-Path $docPath) {
            Copy-Item -Path $docPath -Destination $stagingMod -Force
        }
    }

    # Ensure forward-slash POSIX paths inside ZIP (CKAN/Linux/macOS friendly)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $files = Get-ChildItem -Path $stagingRoot -Recurse -File | Sort-Object FullName
        foreach ($f in $files) {
            $relPath = $f.FullName.Substring($stagingRoot.Length + 1).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $relPath, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
        Remove-Item $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    Write-Host "[3/3] Packaged CKAN-compliant release ZIP: $zipPath" -ForegroundColor Cyan
} else {
    Write-Host "[3/3] Done (pass -PackageRelease to build a CKAN release ZIP)." -ForegroundColor Cyan
}
