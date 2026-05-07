#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stages a Unity-ready folder containing the C# bindings and native plugins
    for windows-x64 + android (arm64-v8a, armeabi-v7a).
.DESCRIPTION
    Output layout (Unity auto-detects platforms by path on import):

        dist/unity/ENet6/
            ENet6.cs
            Plugins/
                x86_64/
                    enet6.dll
                Android/
                    libs/
                        arm64-v8a/libenet6.so
                        armeabi-v7a/libenet6.so

    Drop the ENet6/ folder into your Unity project's Assets/ (or anywhere under
    Assets/), and Unity will generate the appropriate .meta files on import.

    Run build-windows.ps1 and build-android.ps1 first.
#>

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$outDir   = Join-Path $repoRoot "dist\unity\ENet6"

$winDll      = Join-Path $repoRoot "build\windows\x64\release\enet6.dll"
$andArm64    = Join-Path $repoRoot "build\android\arm64-v8a\release\libenet6.so"
$andArmv7    = Join-Path $repoRoot "build\android\armeabi-v7a\release\libenet6.so"
$bindings    = Join-Path $repoRoot "binding\cs\ENet6.cs"

foreach ($f in @($winDll, $andArm64, $andArmv7, $bindings)) {
    if (-not (Test-Path $f)) {
        throw "Missing artifact: $f`nRun the appropriate build script first."
    }
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

Write-Host "Unity package staged at: $outDir" -ForegroundColor Green
