This is the cubemap used when creating a new game.

# How they were generated

Requirement: texconv.exe from https://github.com/Microsoft/DirectXTex

## HDR

* Open hemisphere_cube_32_glay.psd
* Resize to 128
* Hide sun layer
* Export as HDR
* Open in PTgui=>Tools=>Convert to QTVR/Cubic
** Choose Cylindrical
** Size 256, Cubeface strip 6x1, hdr
** Save 
* Open in Photoshop again
* Fill the holes (bottom/top)
* Swap faces so that order is: Side, Side, Top, Bottom, Side, Side (Top and Bottom were at the end)
* Export to RGBA32 with NVIDIA DDS plugin
* Use texconv.exe to convert to F16: texconv.exe test128.dds -o converted -f R16G16B16A16_FLOAT -m 8

## LDR

* Open hemisphere_cube_32_glay.psd
* Resize to 128
* Hide sun layer
* Photoshop: Image=>Mode=>8 Bits/Channel
  * When asked to merge layers, choose "Merge"
  * Choose "Exposure and Gamma" HDR toning method
* Export to RGB8 with NVIDIA DDS plugin
* Use texconv.exe to convert to BC7
