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

foreach ($f in @($winDll, $linSo, $bindings)) {
    if (-not (Test-Path $f)) {
        throw "Missing artifact: $f`nRun the appropriate build script first."
    }
}

if (Test-Path $outDir) { Remove-Item -Recurse -Force $outDir }
New-Item -ItemType Directory -Path $outDir | Out-Null

# Bindings.
Copy-Item $bindings (Join-Path $outDir "ENet6.cs")

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
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>
    <LangVersion>latest</LangVersion>
    <RootNamespace>ENet6</RootNamespace>
    <AssemblyName>ENet6</AssemblyName>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="runtimes\win-x64\native\enet6.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Pack>true</Pack>
      <PackagePath>runtimes\win-x64\native\</PackagePath>
    </Content>
    <Content Include="runtimes\linux-x64\native\libenet6.so">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <Pack>true</Pack>
      <PackagePath>runtimes\linux-x64\native\</PackagePath>
    </Content>
  </ItemGroup>

</Project>
'@
Set-Content -Path (Join-Path $outDir "ENet6.csproj") -Value $csproj -Encoding UTF8

Write-Host ".NET package staged at: $outDir" -ForegroundColor Green
