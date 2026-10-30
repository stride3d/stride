#!/usr/bin/env pwsh
# Builds the Stride Android-emulator helper Vulkan layer (see layer.c) and installs
# its manifest so the emulator's bundled Vulkan loader picks it up when
# STRIDE_EMU_LAYER=1 is set.
#   Windows: HKCU\Software\Khronos\Vulkan\ImplicitLayers
#   Linux:   $HOME/.local/share/vulkan/implicit_layer.d
# Explicit-layer activation via VK_INSTANCE_LAYERS is silently rejected by
# gfxstream's loader code path; enable_environment activates reliably.
$ErrorActionPreference = "Stop"

$isWin = [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)
$here = $PSScriptRoot
$libName = if ($isWin) { "VkLayer_stride_android_emu_helper.dll" } else { "libVkLayer_stride_android_emu_helper.so" }
$lib = Join-Path $here $libName

# Drop stale registrations of other Stride layer names so only this one activates.
if ($isWin) {
    $regPath = "HKCU:\Software\Khronos\Vulkan\ImplicitLayers"
    if (Test-Path $regPath) {
        foreach ($v in (Get-Item $regPath).Property) {
            if ($v -match 'VkLayer_strip_dmabuf\.json$|VkLayer_stride_android\.json$') {
                Remove-ItemProperty -Path $regPath -Name $v -ErrorAction SilentlyContinue
            }
        }
    }
} else {
    $manifestDir = Join-Path $HOME ".local/share/vulkan/implicit_layer.d"
    Remove-Item -ErrorAction SilentlyContinue `
        (Join-Path $manifestDir "VkLayer_strip_dmabuf.json"), `
        (Join-Path $manifestDir "VkLayer_stride_android.json")
}

if ($isWin) {
    # Locate Vulkan SDK headers (need vulkan.h + vk_layer.h)
    $vulkanInclude = $null
    if ($env:VULKAN_SDK -and (Test-Path "$env:VULKAN_SDK\Include\vulkan\vulkan.h")) {
        $vulkanInclude = "$env:VULKAN_SDK\Include"
    } else {
        $hit = Get-ChildItem -Path "C:\VulkanSDK" -Directory -ErrorAction SilentlyContinue |
            Where-Object { Test-Path "$($_.FullName)\Include\vulkan\vulkan.h" } |
            Sort-Object Name -Descending | Select-Object -First 1
        if ($hit) { $vulkanInclude = "$($hit.FullName)\Include" }
    }
    if (-not $vulkanInclude) {
        Write-Error "Vulkan SDK not found. Install from https://vulkan.lunarg.com/ or set VULKAN_SDK."
        exit 1
    }

    # Prefer cl.exe (Dev Cmd) → bootstrap from VsDevCmd.bat via vswhere → fall back to clang-cl.
    function Resolve-Compiler([string]$name) {
        $c = Get-Command $name -ErrorAction SilentlyContinue
        if ($c) { return $c.Source }
        return $null
    }
    $compiler = Resolve-Compiler 'cl.exe'
    if (-not $compiler) {
        $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        if (Test-Path $vswhere) {
            $vsRoot = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
            $vsDevCmd = Join-Path $vsRoot "Common7\Tools\VsDevCmd.bat"
            if (Test-Path $vsDevCmd) {
                $envOut = & cmd /c "`"$vsDevCmd`" -arch=amd64 -host_arch=amd64 >NUL && set"
                foreach ($line in $envOut) {
                    if ($line -match '^([^=]+)=(.*)$') {
                        [Environment]::SetEnvironmentVariable($matches[1], $matches[2], 'Process')
                    }
                }
                $compiler = Resolve-Compiler 'cl.exe'
            }
        }
    }
    if (-not $compiler) { $compiler = Resolve-Compiler 'clang-cl.exe' }
    if (-not $compiler) {
        Write-Error "No MSVC/clang-cl on PATH and VsDevCmd.bat not found. Install Visual Studio Build Tools or LLVM."
        exit 1
    }
    Write-Host "Compiler: $compiler"

    Push-Location $here
    try {
        # /EXPORT at link time — adding __declspec(dllexport) at the definitions
        # collides with the Vulkan SDK header's predeclarations (C2375).
        & $compiler /nologo /LD /O2 /I"$vulkanInclude" "layer.c" "/Fe:$lib" /link /OPT:REF /OPT:ICF /EXPORT:vkNegotiateLoaderLayerInterfaceVersion /EXPORT:vkGetInstanceProcAddr=Layer_vkGetInstanceProcAddr /EXPORT:vkGetDeviceProcAddr=Layer_vkGetDeviceProcAddr
        if ($LASTEXITCODE -ne 0) { throw "compile failed (exit $LASTEXITCODE)" }
    } finally {
        Pop-Location
    }
} else {
    & gcc -shared -fPIC -O2 -o $lib (Join-Path $here "layer.c")
    if ($LASTEXITCODE -ne 0) { throw "gcc failed (exit $LASTEXITCODE)" }
}

# Manifest. On Windows the library_path needs backslash-escaped JSON; on Linux it
# stays as a literal Unix path.
$manifestName = "VkLayer_stride_android_emu_helper.json"
if ($isWin) {
    $manifest = Join-Path $here $manifestName
    $libPathJson = $lib -replace '\\', '\\'
} else {
    $manifest = Join-Path $manifestDir $manifestName
    New-Item -ItemType Directory -Force -Path $manifestDir | Out-Null
    $libPathJson = $lib
}
@"
{
    "file_format_version": "1.0.0",
    "layer": {
        "name": "VK_LAYER_STRIDE_android_emu_helper",
        "type": "GLOBAL",
        "library_path": "$libPathJson",
        "api_version": "1.3.0",
        "implementation_version": "1",
        "description": "Stride Android emulator host helper (stamps host OS into deviceName)",
        "enable_environment": { "STRIDE_EMU_LAYER": "1" },
        "disable_environment": { "STRIDE_EMU_LAYER_DISABLE": "1" }
    }
}
"@ | Set-Content -Path $manifest -Encoding ASCII

if ($isWin) {
    if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }
    New-ItemProperty -Path $regPath -Name $manifest -PropertyType DWord -Value 0 -Force | Out-Null
}

Write-Host ""
Write-Host "Installed layer:"
Write-Host "  lib:      $lib"
Write-Host "  manifest: $manifest"
if ($isWin) { Write-Host "  registry: $regPath -> ""$manifest"" = 0" }
Write-Host ""
Write-Host "Activate with: STRIDE_EMU_LAYER=1"
Write-Host "Suppress with: STRIDE_EMU_LAYER_DISABLE=1"
