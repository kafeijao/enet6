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

        $configArgs = @(
            "f", "-p", "android", "-a", $arch, "-k", "shared", "-m", "release",
            "--examples=false", "--ndk=$NdkPath"
        )

        # 16KB page alignment is required for 64-bit Android on devices with
        # 16KB pages (Android 15+, becoming mandatory for new apps in late 2025).
        # The flag is a no-op for the 32-bit armeabi-v7a target so we scope it.
        if ($arch -eq "arm64-v8a") {
            $configArgs += "--ldflags=-Wl,-z,max-page-size=16384"
        }

        xmake @configArgs
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
