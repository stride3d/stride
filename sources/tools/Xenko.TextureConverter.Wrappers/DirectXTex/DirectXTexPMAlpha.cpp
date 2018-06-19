//-------------------------------------------------------------------------------------
// DirectXTexPMAlpha.cpp
//  
// DirectX Texture Library - Premultiplied alpha operations
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// http://go.microsoft.com/fwlink/?LinkId=248926
//-------------------------------------------------------------------------------------

#include "directxtexp.h"

using namespace DirectX;

namespace
{
    //---------------------------------------------------------------------------------
    // NonPremultiplied alpha -> Premultiplied alpha
    HRESULT PremultiplyAlpha_(const Image& srcImage, const Image& destImage)
    {
        assert(srcImage.width == destImage.width);
        assert(srcImage.height == destImage.height);

        ScopedAlignedArrayXMVECTOR scanline(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*srcImage.width), 16)));
        if (!scanline)
            return E_OUTOFMEMORY;

        const uint8_t *pSrc = srcImage.pixels;
        uint8_t *pDest = destImage.pixels;
        if (!pSrc || !pDest)
            return E_POINTER;

        for (size_t h = 0; h < srcImage.height; ++h)
        {
            if (!_LoadScanline(scanline.get(), srcImage.width, pSrc, srcImage.rowPitch, srcImage.format))
                return E_FAIL;

            XMVECTOR* ptr = scanline.get();
            for (size_t w = 0; w < srcImage.width; ++w)
            {
                XMVECTOR v = *ptr;
                XMVECTOR alpha = XMVectorSplatW(*ptr);
                alpha = XMVectorMultiply(v, alpha);
                *(ptr++) = XMVectorSelect(v, alpha, g_XMSelect1110);
            }

            if (!_StoreScanline(pDest, destImage.rowPitch, destImage.format, scanline.get(), srcImage.width))
                return E_FAIL;

            pSrc += srcImage.rowPitch;
            pDest += destImage.rowPitch;
        }

        return S_OK;
    }

    HRESULT PremultiplyAlphaLinear(const Image& srcImage, DWORD flags, const Image& destImage)
    {
        assert(srcImage.width == destImage.width);
        assert(srcImage.height == destImage.height);

        static_assert(TEX_PMALPHA_SRGB_IN == TEX_FILTER_SRGB_IN, "TEX_PMALHPA_SRGB* should match TEX_FILTER_SRGB*");
        static_assert(TEX_PMALPHA_SRGB_OUT == TEX_FILTER_SRGB_OUT, "TEX_PMALHPA_SRGB* should match TEX_FILTER_SRGB*");
        static_assert(TEX_PMALPHA_SRGB == TEX_FILTER_SRGB, "TEX_PMALHPA_SRGB* should match TEX_FILTER_SRGB*");
        flags &= TEX_PMALPHA_SRGB;

        ScopedAlignedArrayXMVECTOR scanline(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*srcImage.width), 16)));
        if (!scanline)
            return E_OUTOFMEMORY;

        const uint8_t *pSrc = srcImage.pixels;
        uint8_t *pDest = destImage.pixels;
        if (!pSrc || !pDest)
            return E_POINTER;

        for (size_t h = 0; h < srcImage.height; ++h)
        {
            if (!_LoadScanlineLinear(scanline.get(), srcImage.width, pSrc, srcImage.rowPitch, srcImage.format, flags))
                return E_FAIL;

            XMVECTOR* ptr = scanline.get();
            for (size_t w = 0; w < srcImage.width; ++w)
            {
                XMVECTOR v = *ptr;
                XMVECTOR alpha = XMVectorSplatW(*ptr);
                alpha = XMVectorMultiply(v, alpha);
                *(ptr++) = XMVectorSelect(v, alpha, g_XMSelect1110);
            }

            if (!_StoreScanlineLinear(pDest, destImage.rowPitch, destImage.format, scanline.get(), srcImage.width, flags))
                return E_FAIL;

            pSrc += srcImage.rowPitch;
            pDest += destImage.rowPitch;
        }

        return S_OK;
    }

    //---------------------------------------------------------------------------------
    // Premultiplied alpha -> NonPremultiplied alpha (a.k.a. Straight alpha)
    HRESULT DemultiplyAlpha(const Image& srcImage, const Image& destImage)
    {
        assert(srcImage.width == destImage.width);
        assert(srcImage.height == destImage.height);

        ScopedAlignedArrayXMVECTOR scanline(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*srcImage.width), 16)));
        if (!scanline)
            return E_OUTOFMEMORY;

        const uint8_t *pSrc = srcImage.pixels;
        uint8_t *pDest = destImage.pixels;
        if (!pSrc || !pDest)
            return E_POINTER;

        for (size_t h = 0; h < srcImage.height; ++h)
        {
            if (!_LoadScanline(scanline.get(), srcImage.width, pSrc, srcImage.rowPitch, srcImage.format))
                return E_FAIL;

            XMVECTOR* ptr = scanline.get();
            for (size_t w = 0; w < srcImage.width; ++w)
            {
                XMVECTOR v = *ptr;
                XMVECTOR alpha = XMVectorSplatW(*ptr);
                alpha = XMVectorDivide(v, alpha);
                *(ptr++) = XMVectorSelect(v, alpha, g_XMSelect1110);
            }

            if (!_StoreScanline(pDest, destImage.rowPitch, destImage.format, scanline.get(), srcImage.width))
                return E_FAIL;

            pSrc += srcImage.rowPitch;
            pDest += destImage.rowPitch;
        }

        return S_OK;
    }

    HRESULT DemultiplyAlphaLinear(const Image& srcImage, DWORD flags, const Image& destImage)
    {
        assert(srcImage.width == destImage.width);
        assert(srcImage.height == destImage.height);

        static_assert(TEX_PMALPHA_SRGB_IN == TEX_FILTER_SRGB_IN, "TEX_PMALHPA_SRGB* should match TEX_FILTER_SRGB*");
        static_assert(TEX_PMALPHA_SRGB_OUT == TEX_FILTER_SRGB_OUT, "TEX_PMALHPA_SRGB* should match TEX_FILTER_SRGB*");
        static_assert(TEX_PMALPHA_SRGB == TEX_FILTER_SRGB, "TEX_PMALHPA_SRGB* should match TEX_FILTER_SRGB*");
        flags &= TEX_PMALPHA_SRGB;

        ScopedAlignedArrayXMVECTOR scanline(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*srcImage.width), 16)));
        if (!scanline)
            return E_OUTOFMEMORY;

        const uint8_t *pSrc = srcImage.pixels;
        uint8_t *pDest = destImage.pixels;
        if (!pSrc || !pDest)
            return E_POINTER;

        for (size_t h = 0; h < srcImage.height; ++h)
        {
            if (!_LoadScanlineLinear(scanline.get(), srcImage.width, pSrc, srcImage.rowPitch, srcImage.format, flags))
                return E_FAIL;

            XMVECTOR* ptr = scanline.get();
            for (size_t w = 0; w < srcImage.width; ++w)
            {
                XMVECTOR v = *ptr;
                XMVECTOR alpha = XMVectorSplatW(*ptr);
                alpha = XMVectorDivide(v, alpha);
                *(ptr++) = XMVectorSelect(v, alpha, g_XMSelect1110);
            }

            if (!_StoreScanlineLinear(pDest, destImage.rowPitch, destImage.format, scanline.get(), srcImage.width, flags))
                return E_FAIL;

            pSrc += srcImage.rowPitch;
            pDest += destImage.rowPitch;
        }

        return S_OK;
    }
}


//=====================================================================================
// Entry-points
//=====================================================================================

//-------------------------------------------------------------------------------------
// Converts to/from a premultiplied alpha version of the texture
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::PremultiplyAlpha(
    const Image& srcImage,
    DWORD flags,
    ScratchImage& image)
{
    if (!srcImage.pixels)
        return E_POINTER;

    if (IsCompressed(srcImage.format)
        || IsPlanar(srcImage.format)
        || IsPalettized(srcImage.format)
        || IsTypeless(srcImage.format)
        || !HasAlpha(srcImage.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    if ((srcImage.width > UINT32_MAX) || (srcImage.height > UINT32_MAX))
        return E_INVALIDARG;

    HRESULT hr = image.Initialize2D(srcImage.format, srcImage.width, srcImage.height, 1, 1);
    if (FAILED(hr))
        return hr;

    const Image *rimage = image.GetImage(0, 0, 0);
    if (!rimage)
    {
        image.Release();
        return E_POINTER;
    }

    if (flags & TEX_PMALPHA_REVERSE)
    {
        hr = (flags & TEX_PMALPHA_IGNORE_SRGB) ? DemultiplyAlpha(srcImage, *rimage) : DemultiplyAlphaLinear(srcImage, flags, *rimage);
    }
    else
    {
        hr = (flags & TEX_PMALPHA_IGNORE_SRGB) ? PremultiplyAlpha_(srcImage, *rimage) : PremultiplyAlphaLinear(srcImage, flags, *rimage);
    }
    if (FAILED(hr))
    {
        image.Release();
        return hr;
    }

    return S_OK;
}


//-------------------------------------------------------------------------------------
// Converts to/from a premultiplied alpha version of the texture (complex)
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::PremultiplyAlpha(
    const Image* srcImages,
    size_t nimages,
    const TexMetadata& metadata,
    DWORD flags,
    ScratchImage& result)
{
    if (!srcImages || !nimages)
        return E_INVALIDARG;

    if (IsCompressed(metadata.format)
        || IsPlanar(metadata.format)
        || IsPalettized(metadata.format)
        || IsTypeless(metadata.format)
        || !HasAlpha(metadata.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    if ((metadata.width > UINT32_MAX) || (metadata.height > UINT32_MAX))
        return E_INVALIDARG;

    if (metadata.IsPMAlpha() != ((flags & TEX_PMALPHA_REVERSE) != 0))
        return E_FAIL;

    TexMetadata mdata2 = metadata;
    mdata2.SetAlphaMode((flags & TEX_PMALPHA_REVERSE) ? TEX_ALPHA_MODE_STRAIGHT : TEX_ALPHA_MODE_PREMULTIPLIED);
    HRESULT hr = result.Initialize(mdata2);
    if (FAILED(hr))
        return hr;

    if (nimages != result.GetImageCount())
    {
        result.Release();
        return E_FAIL;
    }

    const Image* dest = result.GetImages();
    if (!dest)
    {
        result.Release();
        return E_POINTER;
    }

    for (size_t index = 0; index < nimages; ++index)
    {
        const Image& src = srcImages[index];
        if (src.format != metadata.format)
        {
            result.Release();
            return E_FAIL;
        }

        if ((src.width > UINT32_MAX) || (src.height > UINT32_MAX))
            return E_FAIL;

        const Image& dst = dest[index];
        assert(dst.format == metadata.format);

        if (src.width != dst.width || src.height != dst.height)
        {
            result.Release();
            return E_FAIL;
        }

        if (flags & TEX_PMALPHA_REVERSE)
        {
            hr = (flags & TEX_PMALPHA_IGNORE_SRGB) ? DemultiplyAlpha(src, dst) : DemultiplyAlphaLinear(src, flags, dst);
        }
        else
        {
            hr = (flags & TEX_PMALPHA_IGNORE_SRGB) ? PremultiplyAlpha_(src, dst) : PremultiplyAlphaLinear(src, flags, dst);
        }
        if (FAILED(hr))
        {
            result.Release();
            return hr;
        }
    }

    return S_OK;
}
