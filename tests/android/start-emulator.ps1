#!/usr/bin/env pwsh
# Launches the Android emulator with the same flags the CI workflow uses, so local runs
# and CI produce the same Vulkan renderer (Stride.Dependencies.Lavapipe via the host
# Vulkan loader, picked up from VK_DRIVER_FILES).
#
# Mirrors emulator-options + env in .github/workflows/test-android-game.yml.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$Port,                  # Emulator console port (even). adb uses Port+1.
    [string]$Avd = "stride",
    [switch]$Window,
    [string]$AndroidSdkRoot = $env:ANDROID_SDK_ROOT
)

if ($Port % 2 -ne 0) { throw "-Port must be even (got $Port)." }
function Test-PortFree([int]$p) {
    try { $l = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $p); $l.Start(); $l.Stop(); return $true }
    catch { return $false }
}
foreach ($p in @($Port, $Port + 1)) {
    if (-not (Test-PortFree $p)) { throw "Port $p already in use." }
}

$ErrorActionPreference = "Stop"

# $IsWindows only exists on PowerShell Core 6+; detect portably for Windows PowerShell 5.1.
$isWin = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)

# Locate the Lavapipe ICD shipped in Stride.Dependencies.Lavapipe and point the Vulkan
# loader at it. The package must already be restored — running `dotnet restore` on the
# main solution at least once populates ~/.nuget/packages.
$homeDir = if ($env:USERPROFILE) { $env:USERPROFILE } else { $HOME }
$nupkgRoot = [IO.Path]::Combine($homeDir, ".nuget", "packages", "stride.dependencies.lavapipe")
if (-not (Test-Path $nupkgRoot)) {
    Write-Error "Lavapipe NuGet package not found under $nupkgRoot. Run 'dotnet restore' first."
    exit 1
}
# Linux uses the no-dma_buf Lavapipe variant (runtimes/linux-x64-nodmabuf/native/): gfxstream
# aborts headless on an ICD that advertises VK_EXT_external_memory_dma_buf. The synthetic RID
# matches no real RID graph, so NuGet never auto-deploys it into consumers — but it sits under
# runtimes/ so the NuGet signing tool preserves it. Windows Lavapipe doesn't advertise dma_buf
# (no libdrm in that build), so it uses the normal runtimes/win-x64 one.
if ($isWin) {
    $icdName = "vulkan_lvp.dll"
    $pathPattern = "runtimes[\\/]win-x64[\\/]native"
} else {
    $icdName = "libvulkan_lvp.so"
    $pathPattern = "runtimes[\\/]linux-x64-nodmabuf[\\/]native"
}
$icd = Get-ChildItem -Path $nupkgRoot -Recurse -Filter $icdName |
    Where-Object { $_.FullName -match $pathPattern } |
    Select-Object -First 1
if (-not $icd) {
    Write-Error "Lavapipe driver not found ($pathPattern/$icdName) under $nupkgRoot."
    exit 1
}
$icdJson = Join-Path ([IO.Path]::GetTempPath()) "lvp_icd.json"
$libPath = if ($isWin) { $icd.FullName -replace '\\', '\\\\' } else { $icd.FullName }
"{`"file_format_version`":`"1.0.0`",`"ICD`":{`"library_path`":`"$libPath`",`"api_version`":`"1.3.0`"}}" | Set-Content -Path $icdJson -Encoding ASCII

$env:VK_DRIVER_FILES = $icdJson
# Empty bypasses gfxstream's built-in ICD override.
$env:ANDROID_EMU_VK_ICD = ""

# Stride Android-emulator helper Vulkan layer (see its layer.c for what it does).
# build-and-install.ps1 builds the dll; the manifest is (re)written here on each launch so
# its enable_environment var always matches this script.
$layerDir = [IO.Path]::Combine($PSScriptRoot, "stride_android_emu_helper_layer")
$libName = if ($isWin) { "VkLayer_stride_android_emu_helper.dll" } else { "libVkLayer_stride_android_emu_helper.so" }
$libPath = [IO.Path]::Combine($layerDir, $libName)
if (-not (Test-Path $libPath)) {
    & ([IO.Path]::Combine($layerDir, "build-and-install.ps1"))
}
$manifestPath = [IO.Path]::Combine($layerDir, "VkLayer_stride_android_emu_helper.json")
$manifestLibPath = if ($isWin) { $libPath -replace '\\', '\\' } else { $libPath }
@"
{
    "file_format_version": "1.0.0",
    "layer": {
        "name": "VK_LAYER_STRIDE_android_emu_helper",
        "type": "GLOBAL",
        "library_path": "$manifestLibPath",
        "api_version": "1.3.0",
        "implementation_version": "1",
        "description": "Stride Android emulator host helper (stamps host OS into deviceName)",
        "enable_environment": { "STRIDE_EMU_LAYER": "1" },
        "disable_environment": { "STRIDE_EMU_LAYER_DISABLE": "1" }
    }
}
"@ | Set-Content -Path $manifestPath -Encoding ASCII
$env:STRIDE_EMU_LAYER = "1"

if (-not $AndroidSdkRoot) {
    $AndroidSdkRoot = if ($isWin) { [IO.Path]::Combine($env:LOCALAPPDATA, "Android", "Sdk") } else { [IO.Path]::Combine($HOME, "Android", "Sdk") }
}
$emulator = [IO.Path]::Combine($AndroidSdkRoot, "emulator", $(if ($isWin) { "emulator.exe" } else { "emulator" }))
if (-not (Test-Path $emulator)) {
    Write-Error "Emulator binary not found at $emulator (set ANDROID_SDK_ROOT or -AndroidSdkRoot)"
    exit 1
}

# Vulkan-through-host-loader knob is platform-specific:
#   Windows emulator: -use-host-vulkan
#   Linux emulator:   -gpu host + -feature ForceGpuHost (no -use-host-vulkan in that build)
# Both routes make gfxstream call vulkan via the system loader so it honours
# VK_DRIVER_FILES.
$emuArgs = @("-avd", $Avd, "-port", $Port, "-no-snapshot-load", "-no-audio", "-no-boot-anim")
if ($isWin) {
    $emuArgs += "-gpu", "swiftshader_indirect", "-use-host-vulkan"
} else {
    $emuArgs += "-gpu", "host", "-feature", "ForceGpuHost"
}
if (-not $Window) { $emuArgs += "-no-window" }

Write-Host "VK_DRIVER_FILES=$($env:VK_DRIVER_FILES)"
Write-Host "EMULATOR_SERIAL=127.0.0.1:$($Port + 1)"
Write-Host "$emulator $emuArgs"
& $emulator @emuArgs
