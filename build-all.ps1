#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds enet6 for windows-x64, android-arm64-v8a, android-armeabi-v7a, and linux-x86_64.
.PARAMETER NdkPath
    Path to the Android NDK (forwarded to build-android.ps1).
#>

param(
    [string]$NdkPath = "F:\Downloads\android-ndk-r23c-windows\android-ndk-r23c"
)

$ErrorActionPreference = "Stop"

& "$PSScriptRoot\build-windows.ps1"
& "$PSScriptRoot\build-android.ps1" -NdkPath $NdkPath
& "$PSScriptRoot\build-linux.ps1"

Write-Host "All builds complete." -ForegroundColor Green
