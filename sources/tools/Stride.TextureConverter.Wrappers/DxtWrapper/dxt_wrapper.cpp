// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "dxt_wrapper.h"
#include <string>

// Utilities functions
void dxtComputePitch( DXGI_FORMAT fmt, int width, int height, int& rowPitch, int& slicePitch, DirectX::CP_FLAGS flags = DirectX::CP_FLAGS_NONE )
{
	size_t rowPitchT, slicePitchT;
	DirectX::ComputePitch(fmt, width, height, rowPitchT, slicePitchT, flags);
	rowPitch = rowPitchT;
	slicePitch = slicePitchT;
}

// For handling different encodings
const wchar_t* narrowToWideString(const char* szFile)
{
	std::string nstr(szFile);
    std::wstring wstr = std::wstring(nstr.begin(), nstr.end());
    wchar_t* filePath = new wchar_t[wstr.size() + 1];
    std::copy(wstr.begin(), wstr.end(), filePath);
    filePath[wstr.size()] = L'\0'; // Null-terminate the wide string
    return filePath;
}

bool dxtIsCompressed(DXGI_FORMAT fmt) { return DirectX::IsCompressed(fmt); }

HRESULT dxtConvert( const DirectX::Image& srcImage, DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold, DirectX::ScratchImage& cImage )
{
	return DirectX::Convert(srcImage, format, filter, threshold, cImage);
}

HRESULT dxtConvertArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold, DirectX::ScratchImage& cImage )
{
	return DirectX::Convert(srcImages, nimages, metadata, format, filter, threshold, cImage);
}

HRESULT dxtCompress( const DirectX::Image& srcImage, DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float alphaRef, DirectX::ScratchImage& cImage )
{
	return DirectX::Compress(srcImage, format, compress, alphaRef, cImage);
}

HRESULT dxtCompressArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS compress, float alphaRef, DirectX::ScratchImage& cImages )
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

HRESULT dxtGenerateMipMaps( const DirectX::Image& baseImage, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain, bool allow1D = false)
{
	return DirectX::GenerateMipMaps(baseImage, filter, levels, mipChain, allow1D);
}

HRESULT dxtGenerateMipMapsArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain )
{
	return DirectX::GenerateMipMaps(srcImages, nimages, metadata, filter, levels, mipChain);
}

HRESULT dxtGenerateMipMaps3D( const DirectX::Image* baseImages, int depth, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain )
{
	return DirectX::GenerateMipMaps3D(baseImages, depth, filter, levels, mipChain);
}

HRESULT dxtGenerateMipMaps3DArray( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::TEX_FILTER_FLAGS filter, int levels, DirectX::ScratchImage& mipChain )
{
	return DirectX::GenerateMipMaps3D(srcImages, nimages, metadata, filter, levels, mipChain);
}

HRESULT dxtResize( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, int width, int height, DirectX::TEX_FILTER_FLAGS filter, DirectX::ScratchImage& result )
{
	return DirectX::Resize(srcImages, nimages, metadata, width, height, filter, result);
}

HRESULT dxtComputeNormalMap( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format, DirectX::ScratchImage& normalMaps )
{
	return DirectX::ComputeNormalMap(srcImages, nimages, metadata, flags, amplitude, format, normalMaps);
}

HRESULT dxtPremultiplyAlpha( const DirectX::Image* srcImages, int nimages, const DirectX::TexMetadata& metadata, DirectX::TEX_PMALPHA_FLAGS flags, DirectX::ScratchImage& result )
{
	return DirectX::PremultiplyAlpha(srcImages, nimages, metadata, flags, result);
}


// I/O functions
HRESULT dxtLoadDDSFile( const char* szFile, DirectX::DDS_FLAGS flags, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image)
{
	const wchar_t* filePath = narrowToWideString(szFile);
	auto result = DirectX::LoadFromDDSFile(filePath, flags, metadata, image);
	delete[] filePath;
	return result;
}


HRESULT dxtLoadTGAFile( const char* szFile, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image)
{
	const wchar_t* filePath = narrowToWideString(szFile);
	auto result = DirectX::LoadFromTGAFile(filePath, metadata, image);
	delete[] filePath;
	return result;
}

HRESULT dxtLoadWICFile(LPCWSTR szFile, int flags, DirectX::TexMetadata* metadata, DirectX::ScratchImage& image)
{
	return DirectX::LoadFromWICFile(szFile, flags, metadata, image);
}

HRESULT dxtSaveToDDSFile( const DirectX::Image& image, DirectX::DDS_FLAGS flags, const char* szFile )
{
	const wchar_t* filePath = narrowToWideString(szFile);
	auto result = DirectX::SaveToDDSFile(image, flags, filePath);
	delete[] filePath;
	return result;
}

HRESULT dxtSaveToDDSFileArray( const DirectX::Image* images, int nimages, const DirectX::TexMetadata& metadata, DirectX::DDS_FLAGS flags, const char* szFile )
{
	const wchar_t* filePath = narrowToWideString(szFile);
	auto result = DirectX::SaveToDDSFile(images, nimages, metadata, flags, filePath);
	delete[] filePath;
	return result;
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
