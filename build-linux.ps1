#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds enet6 as a shared release library for linux-x86_64 inside a Docker container.
.DESCRIPTION
    Uses debian:bookworm-slim, which is the same base as
    mcr.microsoft.com/dotnet/runtime:10.0, so the produced libenet6.so is
    glibc-compatible with the .NET 10 runtime image.

    Output: build/linux/x86_64/release/libenet6.so
#>

$ErrorActionPreference = "Stop"

# Verify Docker is available.
$null = Get-Command docker -ErrorAction SilentlyContinue
if (-not $?) {
    throw "docker not found in PATH. Install Docker Desktop or run this script from a shell that has docker available."
}

$repoRoot = $PSScriptRoot

# Heredoc for the in-container build script. Single-quoted so PowerShell does no expansion;
# bash receives it verbatim.
$inContainer = @'
set -eo pipefail
export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends \
    build-essential \
    curl \
    ca-certificates \
    git \
    unzip \
    xz-utils
curl -fsSL https://xmake.io/shget.text | bash
# xmake's profile references FISH_VERSION unguarded, so don't run it under set -u.
source /root/.xmake/profile
xmake f -p linux -a x86_64 -k shared -m release --examples=false -y
xmake -y
'@

docker run --rm `
    -e XMAKE_ROOT=y `
    -v "${repoRoot}:/work" `
    -w /work `
    debian:bookworm-slim `
    bash -c ($inContainer -replace "`r`n", "`n")

if ($LASTEXITCODE -ne 0) {
    throw "Linux build failed (docker exit $LASTEXITCODE)"
}

Write-Host "Built: build/linux/x86_64/release/libenet6.so" -ForegroundColor Green
