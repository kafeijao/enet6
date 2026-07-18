#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stages a .NET 10 library project containing the C# bindings and native
    binaries for win-x64 + linux-x64.
.DESCRIPTION
    Output layout:

        dist/dotnet/ENet6/
            ENet6.csproj         (net10.0 class library)
            ENet6.cs             (bindings)
            runtimes/
                win-x64/native/enet6.dll
                linux-x64/native/libenet6.so

    The runtimes/{rid}/native/ layout is the standard one .NET understands:
    when the consuming app runs as win-x64 or linux-x64 (e.g. inside the
    mcr.microsoft.com/dotnet/runtime:10.0 image), it picks the matching native
    automatically. Reference this csproj from your app, or `dotnet build` it
    standalone.

    Run build-windows.ps1 and build-linux.ps1 first.
#>

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$outDir   = Join-Path $repoRoot "dist\dotnet\ENet6"

$winDll   = Join-Path $repoRoot "build\windows\x64\release\enet6.dll"
$linSo    = Join-Path $repoRoot "build\linux\x86_64\release\libenet6.so"
$bindings = Join-Path $repoRoot "binding\cs\ENet6.cs"
$readme   = Join-Path $repoRoot "package-README.md"
$license  = Join-Path $repoRoot "LICENSE"

foreach ($f in @($winDll, $linSo, $bindings, $readme, $license)) {
    if (-not (Test-Path $f)) {
        throw "Missing artifact: $f`nRun the appropriate build script first."
    }
}

if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Path $outDir | Out-Null

# Bindings.
Copy-Item $bindings (Join-Path $outDir "ENet6.cs")

# README + LICENSE (shipped next to the csproj).
Copy-Item $readme  (Join-Path $outDir "README.md")
Copy-Item $license (Join-Path $outDir "LICENSE.md")

# Native libs in the standard runtimes/{rid}/native layout.
$winNativeDir = Join-Path $outDir "runtimes\win-x64\native"
$linNativeDir = Join-Path $outDir "runtimes\linux-x64\native"
New-Item -ItemType Directory -Path $winNativeDir | Out-Null
New-Item -ItemType Directory -Path $linNativeDir | Out-Null

Copy-Item $winDll (Join-Path $winNativeDir "enet6.dll")
Copy-Item $linSo  (Join-Path $linNativeDir "libenet6.so")

# csproj.
$csproj = @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.1;net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
    <RootNamespace>ENet6</RootNamespace>
    <AssemblyName>ENet6-CSharp</AssemblyName>
  </PropertyGroup>

  <ItemGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
    <Content Include="runtimes\win-x64\native\enet6.dll">
      <Pack>true</Pack>
      <Link>enet6.dll</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
  <ItemGroup Condition="$([MSBuild]::IsOSPlatform('Linux'))">
    <Content Include="runtimes\linux-x64\native\libenet6.so">
      <Pack>true</Pack>
      <Link>libenet6.so</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
'@
Set-Content -Path (Join-Path $outDir "ENet6.csproj") -Value $csproj -Encoding UTF8

Write-Host ".NET package staged at: $outDir" -ForegroundColor Green
