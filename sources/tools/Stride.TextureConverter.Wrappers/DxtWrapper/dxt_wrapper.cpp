// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "dxt_wrapper.h"

#include <cstdlib>
#include <cstring>
#include <new>
#include <string>

struct DxtImageSet
{
	DirectX::ScratchImage scratch;
};

namespace
{
	// UTF-8 → wide. DirectXTex file APIs take wchar_t* on all platforms.
	// On Windows wchar_t is 16-bit; elsewhere it is 32-bit. mbstowcs handles both.
	std::wstring widen(const char* utf8)
	{
		if (!utf8) return std::wstring();
		std::mbstate_t state{};
		const char* src = utf8;
		size_t needed = std::mbsrtowcs(nullptr, &src, 0, &state);
		if (needed == static_cast<size_t>(-1)) {
			// Fallback: lossy char→wchar_t cast for paths the locale can't decode.
			std::wstring fallback;
			fallback.reserve(std::strlen(utf8));
			for (const char* p = utf8; *p; ++p) fallback.push_back(static_cast<wchar_t>(static_cast<unsigned char>(*p)));
			return fallback;
		}
		std::wstring out(needed, L'\0');
		src = utf8;
		state = {};
		std::mbsrtowcs(out.data(), &src, needed + 1, &state);
		return out;
	}

	template <typename Op>
	HRESULT runOp(DxtImageSet** outSet, Op&& op) noexcept
	{
		if (!outSet) return E_POINTER;
		auto* set = new (std::nothrow) DxtImageSet;
		if (!set) { *outSet = nullptr; return E_OUTOFMEMORY; }
		HRESULT hr = op(set->scratch);
		if (FAILED(hr)) { delete set; *outSet = nullptr; return hr; }
		*outSet = set;
		return S_OK;
	}
}

extern "C" {

// Lifecycle / queries --------------------------------------------------------
void                        dxtRelease(DxtImageSet* set)                                      { delete set; }
const DirectX::TexMetadata* dxtGetMetadata(const DxtImageSet* set)                            { return &set->scratch.GetMetadata(); }
const DirectX::Image*       dxtGetImages(const DxtImageSet* set)                              { return set->scratch.GetImages(); }
int                         dxtGetImageCount(const DxtImageSet* set)                          { return static_cast<int>(set->scratch.GetImageCount()); }
uint8_t*                    dxtGetPixels(const DxtImageSet* set)                              { return set->scratch.GetPixels(); }
size_t                      dxtGetPixelsSize(const DxtImageSet* set)                          { return set->scratch.GetPixelsSize(); }
bool                        dxtOverrideFormat(DxtImageSet* set, DXGI_FORMAT format)           { return set->scratch.OverrideFormat(format); }

// I/O ------------------------------------------------------------------------
HRESULT dxtLoadDDS(const char* utf8Path, DirectX::DDS_FLAGS flags, DxtImageSet** outSet)
{
	auto path = widen(utf8Path);
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::LoadFromDDSFile(path.c_str(), flags, nullptr, s);
	});
}

HRESULT dxtLoadTGA(const char* utf8Path, DirectX::TGA_FLAGS flags, DxtImageSet** outSet)
{
	auto path = widen(utf8Path);
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::LoadFromTGAFile(path.c_str(), flags, nullptr, s);
	});
}

HRESULT dxtLoadHDR(const char* utf8Path, DxtImageSet** outSet)
{
	auto path = widen(utf8Path);
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::LoadFromHDRFile(path.c_str(), nullptr, s);
	});
}

#ifdef _WIN32
HRESULT dxtLoadWIC(const char* utf8Path, DirectX::WIC_FLAGS flags, DxtImageSet** outSet)
{
	auto path = widen(utf8Path);
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::LoadFromWICFile(path.c_str(), flags, nullptr, s);
	});
}
#endif

HRESULT dxtSaveDDS(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                   DirectX::DDS_FLAGS flags, const char* utf8Path)
{
	auto path = widen(utf8Path);
	return DirectX::SaveToDDSFile(images, static_cast<size_t>(count), *metadata, flags, path.c_str());
}

HRESULT dxtSaveHDR(const DirectX::Image* image, const char* utf8Path)
{
	auto path = widen(utf8Path);
	return DirectX::SaveToHDRFile(*image, path.c_str());
}

// Operations -----------------------------------------------------------------
HRESULT dxtCompress(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                    DXGI_FORMAT format, DirectX::TEX_COMPRESS_FLAGS flags, float alphaRef,
                    DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::Compress(images, static_cast<size_t>(count), *metadata, format, flags, alphaRef, s);
	});
}

HRESULT dxtDecompress(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                      DXGI_FORMAT format,
                      DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::Decompress(images, static_cast<size_t>(count), *metadata, format, s);
	});
}

HRESULT dxtConvert(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                   DXGI_FORMAT format, DirectX::TEX_FILTER_FLAGS filter, float threshold,
                   DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::Convert(images, static_cast<size_t>(count), *metadata, format, filter, threshold, s);
	});
}

HRESULT dxtResize(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                  int width, int height, DirectX::TEX_FILTER_FLAGS filter,
                  DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::Resize(images, static_cast<size_t>(count), *metadata,
		                       static_cast<size_t>(width), static_cast<size_t>(height), filter, s);
	});
}

HRESULT dxtGenerateMips(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                        DirectX::TEX_FILTER_FLAGS filter, int levels,
                        DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::GenerateMipMaps(images, static_cast<size_t>(count), *metadata,
		                                filter, static_cast<size_t>(levels), s);
	});
}

HRESULT dxtGenerateMips3D(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                          DirectX::TEX_FILTER_FLAGS filter, int levels,
                          DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::GenerateMipMaps3D(images, static_cast<size_t>(count), *metadata,
		                                  filter, static_cast<size_t>(levels), s);
	});
}

HRESULT dxtNormalMap(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                     DirectX::CNMAP_FLAGS flags, float amplitude, DXGI_FORMAT format,
                     DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::ComputeNormalMap(images, static_cast<size_t>(count), *metadata,
		                                 flags, amplitude, format, s);
	});
}

HRESULT dxtPremultiplyAlpha(const DirectX::Image* images, int count, const DirectX::TexMetadata* metadata,
                            DirectX::TEX_PMALPHA_FLAGS flags,
                            DxtImageSet** outSet)
{
	return runOp(outSet, [&](DirectX::ScratchImage& s) {
		return DirectX::PremultiplyAlpha(images, static_cast<size_t>(count), *metadata, flags, s);
	});
}

HRESULT dxtScaleMipsAlphaForCoverage(DxtImageSet* set, int item, float alphaRef)
{
	if (!set) return E_POINTER;
	return DirectX::ScaleMipMapsAlphaForCoverage(
	    set->scratch.GetImages(), set->scratch.GetImageCount(), set->scratch.GetMetadata(),
	    static_cast<size_t>(item), alphaRef, set->scratch);
}

// Utilities ------------------------------------------------------------------
HRESULT dxtComputePitch(DXGI_FORMAT fmt, int width, int height, DirectX::CP_FLAGS flags,
                        size_t* rowPitch, size_t* slicePitch)
{
	size_t r = 0, s = 0;
	HRESULT hr = DirectX::ComputePitch(fmt, static_cast<size_t>(width), static_cast<size_t>(height), r, s, flags);
	if (rowPitch)   *rowPitch   = r;
	if (slicePitch) *slicePitch = s;
	return hr;
}

bool dxtIsCompressed(DXGI_FORMAT fmt) { return DirectX::IsCompressed(fmt); }

size_t dxtBytesPerBlock(DXGI_FORMAT fmt) { return DirectX::BytesPerBlock(fmt); }

int dxtCalculateMipLevels(int width, int height)
{
	size_t levels = 0;
	DirectX::CalculateMipLevels(static_cast<size_t>(width), static_cast<size_t>(height), levels);
	return static_cast<int>(levels);
}

int dxtCalculateMipLevels3D(int width, int height, int depth)
{
	size_t levels = 0;
	DirectX::CalculateMipLevels3D(static_cast<size_t>(width), static_cast<size_t>(height),
	                              static_cast<size_t>(depth), levels);
	return static_cast<int>(levels);
}

} // extern "C"