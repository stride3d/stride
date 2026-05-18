// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#ifndef DXT_WRAPPER_H
#define DXT_WRAPPER_H

#ifdef _MSC_VER
#define DXT_API __declspec(dllexport)
#else
#if __GNUC__ >= 4
	#define DXT_API __attribute__ ((visibility("default")))
#else
	#define DXT_API
#endif
#endif

#include "DirectXTex.h"

extern "C" {

	// Opaque handle wrapping DirectX::ScratchImage. Created by loaders or ops,
	// released with dxtRelease. C# treats this as IntPtr.
	struct DxtImageSet;

	// Lifecycle / queries -----------------------------------------------------
	DXT_API void                        dxtRelease(DxtImageSet* set);
	DXT_API const DirectX::TexMetadata* dxtGetMetadata(const DxtImageSet* set);
	DXT_API const DirectX::Image*       dxtGetImages(const DxtImageSet* set);
	DXT_API int                         dxtGetImageCount(const DxtImageSet* set);
	DXT_API uint8_t*                    dxtGetPixels(const DxtImageSet* set);
	DXT_API size_t                      dxtGetPixelsSize(const DxtImageSet* set);
	DXT_API bool                        dxtOverrideFormat(DxtImageSet* set, DXGI_FORMAT format);

	// I/O ---------------------------------------------------------------------
	// Paths are UTF-8; widened internally on Windows.
	DXT_API HRESULT dxtLoadDDS(const char* utf8Path, DirectX::DDS_FLAGS flags, DxtImageSet** outSet);
	DXT_API HRESULT dxtLoadTGA(const char* utf8Path, DirectX::TGA_FLAGS flags, DxtImageSet** outSet);
	DXT_API HRESULT dxtLoadHDR(const char* utf8Path,                           DxtImageSet** outSet);
#ifdef _WIN32
	DXT_API HRESULT dxtLoadWIC(const char* utf8Path, DirectX::WIC_FLAGS flags, DxtImageSet** outSet);
#endif
	DXT_API HRESULT dxtSaveDDS(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                           DirectX::DDS_FLAGS flags, const char* utf8Path);
	DXT_API HRESULT dxtSaveHDR(const DirectX::Image* image, const char* utf8Path);

	// Operations — read input images/metadata, allocate fresh set as output. --
	DXT_API HRESULT dxtCompress(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                            DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS flags, float alphaRef,
	                            DxtImageSet** outSet);
	DXT_API HRESULT dxtDecompress(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                              DXGI_FORMAT format,
	                              DxtImageSet** outSet);
	DXT_API HRESULT dxtConvert(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                           DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold,
	                           DxtImageSet** outSet);
	DXT_API HRESULT dxtResize(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                          int width, int height, DirectX::TEX_FILTER_FLAGS filter,
	                          DxtImageSet** outSet);
	DXT_API HRESULT dxtGenerateMips(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                                DirectX::TEX_FILTER_FLAGS filter, int levels,
	                                DxtImageSet** outSet);
	DXT_API HRESULT dxtGenerateMips3D(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                                  DirectX::TEX_FILTER_FLAGS filter, int levels,
	                                  DxtImageSet** outSet);
	DXT_API HRESULT dxtNormalMap(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                             DirectX::CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format,
	                             DxtImageSet** outSet);
	DXT_API HRESULT dxtPremultiplyAlpha(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
	                                    DirectX::TEX_PMALPHA_FLAGS flags,
	                                    DxtImageSet** outSet);

	// Mutate an existing set in place (preserve alpha-test coverage across mips).
	DXT_API HRESULT dxtScaleMipsAlphaForCoverage(DxtImageSet* set, int item, float alphaRef);

	// Utilities ---------------------------------------------------------------
	DXT_API HRESULT dxtComputePitch(DXGI_FORMAT fmt, int width, int height, DirectX::CP_FLAGS flags,
	                                size_t* rowPitch, size_t* slicePitch);
	DXT_API bool    dxtIsCompressed(DXGI_FORMAT fmt);
	DXT_API size_t  dxtBytesPerBlock(DXGI_FORMAT fmt);
	DXT_API int     dxtCalculateMipLevels(int width, int height);
	DXT_API int     dxtCalculateMipLevels3D(int width, int height, int depth);

} // extern "C"

#endif // DXT_WRAPPER_H
