// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "dxt_wrapper.h"

// Utilities functions
void dxtComputePitch( DXGI_FORMAT fmt, int width, int height, int& rowPitch, int& slicePitch, int flags = DirectX::CP_FLAGS_NONE )
{
	size_t rowPitchT, slicePitchT;
	DirectX::ComputePitch(fmt, width, height, rowPitchT, slicePitchT, flags);
	rowPitch = rowPitchT;
	slicePitch = slicePitchT;
}

bool dxtIsCompressed(DXGI_FORMAT fmt) { return DirectX::IsCompressed(fmt); }

HRESULT dxtConvert( const DirectX::Image& srcImage, DXGI_FORMAT format, int filter, float threshold, DirectX::ScratchImage& cImage )
{
	return DirectX::Convert(srcImage, format, filter, threshold, cImage);
}

HRESULT dxtConvertArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, int filter, float threshold, DirectX::ScratchImage& cImage )
{
	return DirectX::Convert(srcImages, nimages, metadata, format, filter, threshold, cImage);
}

HRESULT dxtCompress( const DirectX::Image& srcImage, DXGI_FORMAT format, int compress, float alphaRef, DirectX::ScratchImage& cImage )
{
	return DirectX::Compress(srcImage, format, compress, alphaRef, cImage);
}

HRESULT dxtCompressArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, int compress, float alphaRef, DirectX::ScratchImage& cImages )
{
	return DirectX::Compress(srcImages, nimages, metadata, format, compress, alphaRef, cImages);
}

HRESULT dxtDecompress( const DirectX::Image& cImage, DXGI_FORMAT format, DirectX::ScratchImage& image )
{
	return DirectX::Decompress(cImage, format, image);
}

HRESULT dxtDecompressArray( const DirectX::Image* cImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, DirectX::ScratchImage& images )
{
	return DirectX::Decompress(cImages,  nimages, metadata, format, images);
}

HRESULT dxtGenerateMipMaps( const DirectX::Image& baseImage, int filter, int levels, DirectX::ScratchImage& mipChain, bool allow1D = false)
{
	return DirectX::GenerateMipMaps(baseImage, filter, levels, mipChain, allow1D);
}

HRESULT dxtGenerateMipMapsArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, int filter, int levels, DirectX::ScratchImage& mipChain )
{
	return DirectX::GenerateMipMaps(srcImages, nimages, metadata, filter, levels, mipChain);
}

HRESULT dxtGenerateMipMaps3D( const DirectX::Image* baseImages, int depth, int filter, int levels, DirectX::ScratchImage& mipChain )
{
	return DirectX::GenerateMipMaps3D(baseImages, depth, filter, levels, mipChain);
}

HRESULT dxtGenerateMipMaps3DArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, int filter, int levels, DirectX::ScratchImage& mipChain )
{
	return DirectX::GenerateMipMaps3D(srcImages, nimages, metadata, filter, levels, mipChain);
}

HRESULT dxtResize(const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, int width, int height, int filter, DirectX::ScratchImage& result )
{
	return DirectX::Resize(srcImages, nimages, metadata, width, height, filter, result);
}

HRESULT dxtComputeNormalMap( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, int flags, float amplitude, DXGI_FORMAT format, DirectX::ScratchImage& normalMaps )
{
	return DirectX::ComputeNormalMap(srcImages, nimages, metadata, flags, amplitude, format, normalMaps);
}

HRESULT dxtPremultiplyAlpha( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, int flags, DirectX::ScratchImage& result )
{
	return DirectX::PremultiplyAlpha(srcImages, nimages, metadata, flags, result);
}


// I/O functions
HRESULT dxtLoadDDSFile(LPCWSTR szFile, int flags, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image)
{
	return DirectX::LoadFromDDSFile(szFile, flags, metadata, image);
}

HRESULT dxtLoadTGAFile(LPCWSTR szFile, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image)
{
	return DirectX::LoadFromTGAFile(szFile, metadata, image);
}

HRESULT dxtLoadWICFile(LPCWSTR szFile, int flags, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image)
{
	return DirectX::LoadFromWICFile(szFile, flags, metadata, image);
}

HRESULT dxtSaveToDDSFile( const DirectX::Image& image, int flags, LPCWSTR szFile )
{
	return DirectX::SaveToDDSFile(image, flags, szFile);
}

HRESULT dxtSaveToDDSFileArray( const DirectX::Image* images, int nimages, const DirectX::TexMetadata& metadata, int flags, LPCWSTR szFile )
{
	return DirectX::SaveToDDSFile(images, nimages, metadata, flags, szFile);
}

// Scratch Image
DirectX::ScratchImage * dxtCreateScratchImage()
{
	return new DirectX::ScratchImage();
}

void dxtDeleteScratchImage(DirectX::ScratchImage * img) { delete img; }

HRESULT dxtInitialize(DirectX::ScratchImage * img, const DirectX::TexMetadata& mdata ) { return img->Initialize(mdata); }

HRESULT dxtInitialize1D(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int length,  int arraySize,  int mipLevels ) { return img->Initialize1D(fmt, length, arraySize, mipLevels); }
HRESULT dxtInitialize2D(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int width,  int height,  int arraySize,  int mipLevels ) { return img->Initialize2D(fmt, width, height, arraySize, mipLevels); }
HRESULT dxtInitialize3D(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int width,  int height,  int depth,  int mipLevels ) { return img->Initialize3D(fmt, width, height, depth, mipLevels); }
HRESULT dxtInitializeCube(DirectX::ScratchImage * img, DXGI_FORMAT fmt,  int width,  int height,  int nCubes,  int mipLevels ) { return img->InitializeCube(fmt, width, height, nCubes, mipLevels); }

HRESULT dxtInitializeFromImage(DirectX::ScratchImage * img, const DirectX::Image& srcImage, bool allow1D) { return img->InitializeFromImage(srcImage, allow1D); }
HRESULT dxtInitializeArrayFromImages(DirectX::ScratchImage * img, const DirectX::Image* images, int nImages, bool allow1D ) { return img->InitializeArrayFromImages(images, nImages, allow1D); }
HRESULT dxtInitializeCubeFromImages(DirectX::ScratchImage * img, const DirectX::Image* images,  int nImages ) { return img->InitializeCubeFromImages(images, nImages); }
HRESULT dxtInitialize3DFromImages(DirectX::ScratchImage * img, const DirectX::Image* images,  int depth ) { return img->Initialize3DFromImages(images, depth); }


void dxtRelease(DirectX::ScratchImage * img) { img->Release(); }

bool dxtOverrideFormat(DirectX::ScratchImage * img, DXGI_FORMAT f ) { return img->OverrideFormat(f); }

const DirectX::TexMetadata& dxtGetMetadata(const DirectX::ScratchImage * img) { return img->GetMetadata(); }
const DirectX::Image* dxtGetImage(const DirectX::ScratchImage * img, int mip,  int item,  int slice)  { return img->GetImage(mip, item, slice); }

const DirectX::Image* dxtGetImages(const DirectX::ScratchImage * img) { return img->GetImages(); }
int dxtGetImageCount(const DirectX::ScratchImage * img) { return img->GetImageCount(); }

uint8_t* dxtGetPixels(const DirectX::ScratchImage * img) { return img->GetPixels(); }
int dxtGetPixelsSize(const DirectX::ScratchImage * img) { return img->GetPixelsSize(); }
