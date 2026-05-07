#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds enet6 as a shared release library for Android (arm64-v8a and armeabi-v7a).
.PARAMETER NdkPath
    Path to the Android NDK. Defaults to the local r23c install.
.DESCRIPTION
    Outputs:
        build/android/arm64-v8a/release/libenet6.so
        build/android/armeabi-v7a/release/libenet6.so
#>

param(
    [string]$NdkPath = "F:\Downloads\android-ndk-r23c-windows\android-ndk-r23c"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $NdkPath)) {
    throw "Android NDK not found at: $NdkPath"
}

$archs = @("arm64-v8a", "armeabi-v7a")

Push-Location $PSScriptRoot
try {
    foreach ($arch in $archs) {
        Write-Host "==> Building Android $arch" -ForegroundColor Cyan

        xmake f -p android -a $arch -k shared -m release --examples=false --ndk="$NdkPath"
        if ($LASTEXITCODE -ne 0) { throw "xmake config failed for $arch" }

        xmake
        if ($LASTEXITCODE -ne 0) { throw "xmake build failed for $arch" }
    }
}
finally {
    Pop-Location
}

foreach ($arch in $archs) {
    Write-Host "Built: build/android/$arch/release/libenet6.so" -ForegroundColor Green
}
