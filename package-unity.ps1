#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stages a Unity-ready folder containing the C# bindings, native plugins
    (windows-x64 + android arm64-v8a, armeabi-v7a) and the matching .meta files.
.DESCRIPTION
    Output layout:

        dist/unity/ENet6/
            ENet6.cs
            ENet6.cs.meta
            Plugins.meta
            Plugins/
                Android.meta
                x86_64.meta
                Android/
                    libs.meta
                    libs/
                        arm64-v8a.meta
                        armeabi-v7a.meta
                        arm64-v8a/
                            libenet6.so
                            libenet6.so.meta
                        armeabi-v7a/
                            libenet6.so
                            libenet6.so.meta
                x86_64/
                    enet6.dll
                    enet6.dll.meta

    Bundling the .meta files (sourced from unity-meta/ in this repo) preserves
    the GUIDs and per-platform PluginImporter settings, so dropping the ENet6/
    folder into Assets/ keeps existing references intact and the binaries are
    correctly assigned to Editor/Win64/Android targets without manual setup.

    Run build-windows.ps1 and build-android.ps1 first.
#>

$ErrorActionPreference = "Stop"

$repoRoot  = $PSScriptRoot
$outDir    = Join-Path $repoRoot "dist\unity\ENet6"
$metaRoot  = Join-Path $repoRoot "unity-meta"

$winDll      = Join-Path $repoRoot "build\windows\x64\release\enet6.dll"
$andArm64    = Join-Path $repoRoot "build\android\arm64-v8a\release\libenet6.so"
$andArmv7    = Join-Path $repoRoot "build\android\armeabi-v7a\release\libenet6.so"
$bindings    = Join-Path $repoRoot "binding\cs\ENet6.cs"

foreach ($f in @($winDll, $andArm64, $andArmv7, $bindings)) {
    if (-not (Test-Path $f)) {
        throw "Missing artifact: $f`nRun the appropriate build script first."
    }
}

if (-not (Test-Path $metaRoot)) {
    throw "Missing meta source directory: $metaRoot"
}

if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Path $outDir | Out-Null

# Bindings.
Copy-Item $bindings (Join-Path $outDir "ENet6.cs")

# Native plugins.
$pluginsRoot = Join-Path $outDir "Plugins"
$winRoot     = Join-Path $pluginsRoot "x86_64"
$andArm64Dir = Join-Path $pluginsRoot "Android\libs\arm64-v8a"
$andArmv7Dir = Join-Path $pluginsRoot "Android\libs\armeabi-v7a"

New-Item -ItemType Directory -Path $winRoot     | Out-Null
New-Item -ItemType Directory -Path $andArm64Dir | Out-Null
New-Item -ItemType Directory -Path $andArmv7Dir | Out-Null

Copy-Item $winDll   (Join-Path $winRoot     "enet6.dll")
Copy-Item $andArm64 (Join-Path $andArm64Dir "libenet6.so")
Copy-Item $andArmv7 (Join-Path $andArmv7Dir "libenet6.so")

# Meta files (folders + binaries). GUIDs must match the consuming Unity project.
Copy-Item -Recurse -Force (Join-Path $metaRoot "*") $outDir

# Sanity check: every staged asset must have a sibling .meta and vice versa.
$assets = Get-ChildItem -Path $outDir -Recurse -File | Where-Object { $_.Extension -ne ".meta" }
foreach ($asset in $assets) {
    if (-not (Test-Path "$($asset.FullName).meta")) {
        throw "Missing .meta for asset: $($asset.FullName)"
    }
}
$folders = Get-ChildItem -Path $outDir -Recurse -Directory
foreach ($folder in $folders) {
    if (-not (Test-Path "$($folder.FullName).meta")) {
        throw "Missing .meta for folder: $($folder.FullName)"
    }
}
$metas = Get-ChildItem -Path $outDir -Recurse -File -Filter "*.meta"
foreach ($meta in $metas) {
    $target = $meta.FullName.Substring(0, $meta.FullName.Length - 5)
    if (-not (Test-Path $target)) {
        throw "Orphan .meta file (no matching asset/folder): $($meta.FullName)"
    }
}

Write-Host "Unity package staged at: $outDir" -ForegroundColor Green