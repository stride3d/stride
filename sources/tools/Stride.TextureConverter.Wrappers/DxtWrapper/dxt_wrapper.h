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
#include <string>

extern "C" {

	// Utilities functions
	DXT_API void dxtComputePitch( DXGI_FORMAT fmt, int width, int height, int& rowPitch, int& slicePitch, DirectX::CP_FLAGS flags );
	DXT_API bool dxtIsCompressed(DXGI_FORMAT fmt);
	DXT_API HRESULT dxtConvert( const DirectX::Image& srcImage, DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold, DirectX::ScratchImage& cImage );
	DXT_API HRESULT dxtConvertArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold, DirectX::ScratchImage& cImage );
	DXT_API HRESULT dxtCompress( const DirectX::Image& srcImage, DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float alphaRef, DirectX::ScratchImage& cImage );
    DXT_API HRESULT dxtCompressArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float alphaRef, DirectX::ScratchImage& cImages );
    DXT_API HRESULT dxtDecompress( const DirectX::Image& cImage, DXGI_FORMAT format, DirectX::ScratchImage& image );
    DXT_API HRESULT dxtDecompressArray( const DirectX::Image* cImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, DirectX::ScratchImage& images );
	DXT_API HRESULT dxtGenerateMipMaps( const DirectX::Image& baseImage, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain, bool allow1D);
    DXT_API HRESULT dxtGenerateMipMapsArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain );
    DXT_API HRESULT dxtGenerateMipMaps3D( const DirectX::Image* baseImages, int depth, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain );
    DXT_API HRESULT dxtGenerateMipMaps3DArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain );
	DXT_API HRESULT dxtResize(const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, int width, int height, DirectX::TEX_FILTER_FLAGS filter, DirectX::ScratchImage& result );
	DXT_API HRESULT dxtComputeNormalMap( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format, DirectX::ScratchImage& normalMaps );
	DXT_API HRESULT dxtPremultiplyAlpha( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::TEX_PMALPHA_FLAGS flags, DirectX::ScratchImage& result );

	// I/O functions
	DXT_API HRESULT dxtLoadTGAFile(const char* szFile, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image);
	DXT_API HRESULT dxtLoadDDSFile(const char* szFile, DirectX::DDS_FLAGS flags, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image);
	DXT_API HRESULT dxtSaveToDDSFile( const DirectX::Image& image, DirectX::DDS_FLAGS flags, const char* szFile );
    DXT_API HRESULT dxtSaveToDDSFileArray( const DirectX::Image* images, int nimages, const DirectX::TexMetadata& metadata, DirectX::DDS_FLAGS flags, const char* szFile );

	// Scratch Image
	DXT_API DirectX::ScratchImage * dxtCreateScratchImage();
	DXT_API void dxtDeleteScratchImage(DirectX::ScratchImage * img);
	DXT_API HRESULT dxtInitialize(DirectX::ScratchImage * img, const DirectX::TexMetadata& mdata );

	DXT_API HRESULT dxtInitialize1D(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int length,  int arraySize,  int mipLevels );
	DXT_API HRESULT dxtInitialize2D(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int width,  int height,  int arraySize,  int mipLevels );
	DXT_API HRESULT dxtInitialize3D(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int width,  int height,  int depth,  int mipLevels );
	DXT_API HRESULT dxtInitializeCube(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int width,  int height,  int nCubes,  int mipLevels );

	DXT_API HRESULT dxtInitializeFromImage(DirectX::ScratchImage * img, const DirectX::Image& srcImage, bool allow1D);
	DXT_API HRESULT dxtInitializeArrayFromImages(DirectX::ScratchImage * img, const DirectX::Image* images, int nImages, bool allow1D );
	DXT_API HRESULT dxtInitializeCubeFromImages(DirectX::ScratchImage * img, const DirectX::Image* images,  int nImages );
    DXT_API HRESULT dxtInitialize3DFromImages(DirectX::ScratchImage * img, const DirectX::Image* images,  int depth );

	DXT_API void dxtRelease(DirectX::ScratchImage * img);

	DXT_API bool dxtOverrideFormat(DirectX::ScratchImage * img, DXGI_FORMAT f );

	DXT_API const DirectX::TexMetadata& dxtGetMetadata(const DirectX::ScratchImage * img);
	DXT_API const DirectX::Image* dxtGetImage(const DirectX::ScratchImage * img, int mip,  int item,  int slice);

	DXT_API const DirectX::Image* dxtGetImages(const DirectX::ScratchImage * img);
	DXT_API int dxtGetImageCount(const DirectX::ScratchImage * img);

	DXT_API uint8_t* dxtGetPixels(const DirectX::ScratchImage * img);
	DXT_API int dxtGetPixelsSize(const DirectX::ScratchImage * img);

} // extern "C"



#endif // DXT_WRAPPER_H
