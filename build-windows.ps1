#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds enet6 as a shared release library for windows-x64.
.DESCRIPTION
    Output: build/windows/x64/release/enet6.dll
#>

$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    xmake f -p windows -a x64 -k shared -m release --examples=false
    if ($LASTEXITCODE -ne 0) { throw "xmake config failed" }

    xmake
    if ($LASTEXITCODE -ne 0) { throw "xmake build failed" }
}
finally {
    Pop-Location
}

Write-Host "Built: build/windows/x64/release/enet6.dll" -ForegroundColor Green
