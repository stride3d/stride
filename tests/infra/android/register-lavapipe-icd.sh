#!/usr/bin/env bash
# Registers Stride's Lavapipe as the Vulkan ICD of the job through VK_DRIVER_FILES. Run from the repository root.
#
# Use the no-dma_buf Lavapipe variant (runtimes/linux-x64-nodmabuf/native/): gfxstream
# aborts headless on an ICD advertising VK_EXT_external_memory_dma_buf. AssetCompiler
# needs a Vulkan device at build time (Skybox/IBL etc.) and is indifferent to dma_buf,
# so the same ICD serves both; it then drives the emulator's Vulkan path via -gpu host
# -feature ForceGpuHost (gfxstream forwards Vulkan to the host loader, honouring
# VK_DRIVER_FILES).
set -e

dotnet restore sources/Stride.Tests.Game.Runtime.slnf \
  -p:StridePlatforms=Linux \
  -p:StrideGraphicsApis=Vulkan
LIB=$(find ~/.nuget/packages/stride.dependencies.lavapipe -path "*/runtimes/linux-x64-nodmabuf/native/libvulkan_lvp.so" | head -1)
if [ -z "$LIB" ]; then
  echo "::error::Lavapipe libvulkan_lvp.so (no-dma_buf variant) not found in NuGet cache"
  exit 1
fi
LIB_ABS=$(readlink -f "$LIB")
ICD_JSON="$PWD/lvp_icd.json"
echo "{\"file_format_version\":\"1.0.0\",\"ICD\":{\"library_path\":\"$LIB_ABS\",\"api_version\":\"1.3.0\"}}" > "$ICD_JSON"
echo "VK_DRIVER_FILES=$ICD_JSON" >> $GITHUB_ENV
