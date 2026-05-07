# ENet6 (kafeijao fork)

A fork of [SirLynix/enet6](https://github.com/SirLynix/enet6) with PowerShell build and packaging scripts that produce
ready-to-drop-in artifacts for both .NET (win-x64 + linux-x64) and Unity (Editor/Win64 + Android arm64-v8a + Android
armeabi-v7a, including 16 KB page-aligned arm64 builds).

Source: https://github.com/kafeijao/enet6/tree/develop (branch: `develop`)

The build/package scripts live in the repo root:

- `build-windows.ps1`
- `build-linux.ps1`
- `build-android.ps1`
- `build-all.ps1`
- `package-dotnet.ps1`
- `package-unity.ps1`

## Prerequisites

| Tool                                                     | When you need it                                                                                                                                                  |
|----------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [xmake](https://xmake.io) on PATH                        | Windows and Android native builds                                                                                                                                 |
| [Docker](https://www.docker.com/products/docker-desktop) | Linux native build (runs inside `debian:bookworm-slim` so the resulting `libenet6.so` is glibc-compatible with the `mcr.microsoft.com/dotnet/runtime:10.0` image) |
| Android NDK r23c                                         | Android builds. Pass the path with `-NdkPath`; the scripts default to `F:\Downloads\android-ndk-r23c-windows\android-ndk-r23c`, override as needed                |
| PowerShell (5.1 or 7+)                                   | Running the `.ps1` scripts                                                                                                                                        |
| .NET 10 SDK                                              | Only when consuming the .NET package                                                                                                                              |
| Unity                                                    | Only when consuming the Unity package                                                                                                                             |

## Build

Clone and check out the `develop` branch:

```powershell
git clone https://github.com/kafeijao/enet6.git
cd enet6
git checkout develop
```

Then run whichever target you need:

```powershell
# Windows x64 -> build/windows/x64/release/enet6.dll
.\build-windows.ps1

# Linux x86_64 (in Docker) -> build/linux/x86_64/release/libenet6.so
.\build-linux.ps1

# Android arm64-v8a + armeabi-v7a -> build/android/<arch>/release/libenet6.so
# arm64-v8a is linked with -Wl,-z,max-page-size=16384 for 16 KB page alignment
# (mandatory for Android 15+ devices with 16 KB pages).
.\build-android.ps1 -NdkPath "C:\path\to\android-ndk-r23c"

# Everything (Windows + Linux + Android)
.\build-all.ps1 -NdkPath "C:\path\to\android-ndk-r23c"
```

## Package

### .NET

Requires `build-windows.ps1` and `build-linux.ps1` to have completed.

```powershell
.\package-dotnet.ps1
```

Output: `dist/dotnet/ENet6/`

```
dist/dotnet/ENet6/
    README.md
    ENet6.csproj         (net10.0 class library, AssemblyName: ENet6-CSharp)
    ENet6.cs             (P/Invoke bindings)
    runtimes/
        win-x64/native/enet6.dll
        linux-x64/native/libenet6.so
```

The `runtimes/{rid}/native/` layout is the standard .NET runtime-identifier convention: when the consuming app runs as
`win-x64` or `linux-x64`, the matching native library is picked automatically.

### Unity

Requires `build-windows.ps1` and `build-android.ps1` to have completed.

```powershell
.\package-unity.ps1
```

Output: `dist/unity/ENet6/`

```
dist/unity/ENet6/
    README.md
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
```

Meta files are sourced from `unity-meta/` in the repo, so GUIDs and per-platform `PluginImporter` settings (Editor/Win64
enabled for the DLL, Android-only for the `.so` files, `Is16KbAligned: true` for arm64-v8a) are preserved across builds.
Existing references in a Unity project that already used these GUIDs remain valid after re-import.

The script also sanity-checks the staged output: every asset must have a sibling `.meta`, every folder must have a
`.meta`, and there must be no orphan `.meta` files.

## Deploy

### .NET

Copy `dist/dotnet/ENet6/` somewhere inside your solution (for example `MySolution/external/ENet6/`) and add a project
reference from your app:

```xml
<ItemGroup>
  <ProjectReference Include="..\external\ENet6\ENet6.csproj" />
</ItemGroup>
```

Build/run as usual; the appropriate native library is copied next to the output binary on each platform thanks to the
`<Content>` items in `ENet6.csproj`.

If you prefer a NuGet-style flow you can `dotnet pack` the staged csproj and consume the resulting `.nupkg` instead.

### Unity

Copy the entire `dist/unity/ENet6/` folder into your Unity project's `Assets/` (anywhere under `Assets/`, for example
`Assets/Third Party/ENet6/`). Unity will pick up the bundled `.meta` files on import; do not let Unity regenerate them,
otherwise the GUIDs will change and any existing prefab/scene references will break.

After import:

- The Editor and Standalone Win64 builds use `Plugins/x86_64/enet6.dll`.
- Android builds use `Plugins/Android/libs/arm64-v8a/libenet6.so` and `Plugins/Android/libs/armeabi-v7a/libenet6.so` (
  Unity selects the right one per ABI at build time).

No additional Unity import settings need to be tweaked manually; everything is encoded in the bundled meta files.

## Notes

- Address handling: when a `Host` is created with `AddressType.Any`, ENet creates a dual-stack IPv6 socket (
  `IPV6_V6ONLY=0`). To talk to an IPv4 peer through such a host, call `Address.ConvertToIPV6()` after `SetIP`/`SetHost`
  so the destination is expressed as an IPv4-mapped IPv6 address (`::ffff:a.b.c.d`); otherwise the address family will
  mismatch the socket and sends will fail.
- The Linux build deliberately uses `debian:bookworm-slim` to match the glibc version shipped in the official
  `mcr.microsoft.com/dotnet/runtime:10.0` image. If you target a different runtime base image, rebuild on a matching
  distro.
- For 16 KB Android page alignment, only `arm64-v8a` is affected (32-bit Android still uses 4 KB pages); the linker flag
  is therefore scoped to that arch in `build-android.ps1`.