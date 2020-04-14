//-------------------------------------------------------------------------------------
// DirectXTexMisc.cpp
//  
// DirectX Texture Library - Misc image operations
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
    const XMVECTORF32 g_Gamma22 = { 2.2f, 2.2f, 2.2f, 1.f };

    //-------------------------------------------------------------------------------------
    HRESULT ComputeMSE_(
        const Image& image1,
        const Image& image2,
        float& mse,
        _Out_writes_opt_(4) float* mseV,
        DWORD flags)
    {
        if (!image1.pixels || !image2.pixels)
            return E_POINTER;

        assert(image1.width == image2.width && image1.height == image2.height);
        assert(!IsCompressed(image1.format) && !IsCompressed(image2.format));

        const size_t width = image1.width;

        ScopedAlignedArrayXMVECTOR scanline(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*width) * 2, 16)));
        if (!scanline)
            return E_OUTOFMEMORY;

        // Flags implied from image formats
        switch (image1.format)
        {
        case DXGI_FORMAT_B8G8R8X8_UNORM:
            flags |= CMSE_IGNORE_ALPHA;
            break;

        case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
            flags |= CMSE_IMAGE1_SRGB | CMSE_IGNORE_ALPHA;
            break;

        case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
        case DXGI_FORMAT_BC1_UNORM_SRGB:
        case DXGI_FORMAT_BC2_UNORM_SRGB:
        case DXGI_FORMAT_BC3_UNORM_SRGB:
        case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
        case DXGI_FORMAT_BC7_UNORM_SRGB:
            flags |= CMSE_IMAGE1_SRGB;
            break;
        }

        switch (image2.format)
        {
        case DXGI_FORMAT_B8G8R8X8_UNORM:
            flags |= CMSE_IGNORE_ALPHA;
            break;

        case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
            flags |= CMSE_IMAGE2_SRGB | CMSE_IGNORE_ALPHA;
            break;

        case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
        case DXGI_FORMAT_BC1_UNORM_SRGB:
        case DXGI_FORMAT_BC2_UNORM_SRGB:
        case DXGI_FORMAT_BC3_UNORM_SRGB:
        case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
        case DXGI_FORMAT_BC7_UNORM_SRGB:
            flags |= CMSE_IMAGE2_SRGB;
            break;
        }

        const uint8_t *pSrc1 = image1.pixels;
        const size_t rowPitch1 = image1.rowPitch;

        const uint8_t *pSrc2 = image2.pixels;
        const size_t rowPitch2 = image2.rowPitch;

        XMVECTOR acc = g_XMZero;
        static XMVECTORF32 two = { 2.0f, 2.0f, 2.0f, 2.0f };

        for (size_t h = 0; h < image1.height; ++h)
        {
            XMVECTOR* ptr1 = scanline.get();
            if (!_LoadScanline(ptr1, width, pSrc1, rowPitch1, image1.format))
                return E_FAIL;

            XMVECTOR* ptr2 = scanline.get() + width;
            if (!_LoadScanline(ptr2, width, pSrc2, rowPitch2, image2.format))
                return E_FAIL;

            for (size_t i = 0; i < width; ++i)
            {
                XMVECTOR v1 = *(ptr1++);
                if (flags & CMSE_IMAGE1_SRGB)
                {
                    v1 = XMVectorPow(v1, g_Gamma22);
                }
                if (flags & CMSE_IMAGE1_X2_BIAS)
                {
                    v1 = XMVectorMultiplyAdd(v1, two, g_XMNegativeOne);
                }

                XMVECTOR v2 = *(ptr2++);
                if (flags & CMSE_IMAGE2_SRGB)
                {
                    v2 = XMVectorPow(v2, g_Gamma22);
                }
                if (flags & CMSE_IMAGE2_X2_BIAS)
                {
                    v1 = XMVectorMultiplyAdd(v2, two, g_XMNegativeOne);
                }

                // sum[ (I1 - I2)^2 ]
                XMVECTOR v = XMVectorSubtract(v1, v2);
                if (flags & CMSE_IGNORE_RED)
                {
                    v = XMVectorSelect(v, g_XMZero, g_XMMaskX);
                }
                if (flags & CMSE_IGNORE_GREEN)
                {
                    v = XMVectorSelect(v, g_XMZero, g_XMMaskY);
                }
                if (flags & CMSE_IGNORE_BLUE)
                {
                    v = XMVectorSelect(v, g_XMZero, g_XMMaskZ);
                }
                if (flags & CMSE_IGNORE_ALPHA)
                {
                    v = XMVectorSelect(v, g_XMZero, g_XMMaskW);
                }

                acc = XMVectorMultiplyAdd(v, v, acc);
            }

            pSrc1 += rowPitch1;
            pSrc2 += rowPitch2;
        }

        // MSE = sum[ (I1 - I2)^2 ] / w*h
        XMVECTOR d = XMVectorReplicate(float(image1.width * image1.height));
        XMVECTOR v = XMVectorDivide(acc, d);
        if (mseV)
        {
            XMStoreFloat4(reinterpret_cast<XMFLOAT4*>(mseV), v);
            mse = mseV[0] + mseV[1] + mseV[2] + mseV[3];
        }
        else
        {
            XMFLOAT4 _mseV;
            XMStoreFloat4(&_mseV, v);
            mse = _mseV.x + _mseV.y + _mseV.z + _mseV.w;
        }

        return S_OK;
    }

    //-------------------------------------------------------------------------------------
    HRESULT EvaluateImage_(
        const Image& image,
        std::function<void __cdecl(_In_reads_(width) const XMVECTOR* pixels, size_t width, size_t y)>& pixelFunc)
    {
        if (!pixelFunc)
            return E_INVALIDARG;

        if (!image.pixels)
            return E_POINTER;

        assert(!IsCompressed(image.format));

        const size_t width = image.width;

        ScopedAlignedArrayXMVECTOR scanline(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*width), 16)));
        if (!scanline)
            return E_OUTOFMEMORY;

        const uint8_t *pSrc = image.pixels;
        const size_t rowPitch = image.rowPitch;

        for (size_t h = 0; h < image.height; ++h)
        {
            if (!_LoadScanline(scanline.get(), width, pSrc, rowPitch, image.format))
                return E_FAIL;

            pixelFunc(scanline.get(), width, h);

            pSrc += rowPitch;
        }

        return S_OK;
    }


    //-------------------------------------------------------------------------------------
    HRESULT TransformImage_(
        const Image& srcImage,
        std::function<void __cdecl(_Out_writes_(width) XMVECTOR* outPixels, _In_reads_(width) const XMVECTOR* inPixels, size_t width, size_t y)>& pixelFunc,
        const Image& destImage)
    {
        if (!pixelFunc)
            return E_INVALIDARG;

        if (!srcImage.pixels || !destImage.pixels)
            return E_POINTER;

        if (srcImage.width != destImage.width || srcImage.height != destImage.height || srcImage.format != destImage.format)
            return E_FAIL;

        const size_t width = srcImage.width;

        ScopedAlignedArrayXMVECTOR scanlines(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*width*2), 16)));
        if (!scanlines)
            return E_OUTOFMEMORY;

        XMVECTOR* sScanline = scanlines.get();
        XMVECTOR* dScanline = scanlines.get() + width;

        const uint8_t *pSrc = srcImage.pixels;
        const size_t spitch = srcImage.rowPitch;

        uint8_t *pDest = destImage.pixels;
        const size_t dpitch = destImage.rowPitch;

        for (size_t h = 0; h < srcImage.height; ++h)
        {
            if (!_LoadScanline(sScanline, width, pSrc, spitch, srcImage.format))
                return E_FAIL;

#ifdef _DEBUG
            memset(dScanline, 0xCD, sizeof(XMVECTOR)*width);
#endif

            pixelFunc(dScanline, sScanline, width, h);

            if (!_StoreScanline(pDest, destImage.rowPitch, destImage.format, dScanline, width))
                return E_FAIL;

            pSrc += spitch;
            pDest += dpitch;
        }

        return S_OK;
    }
};


//=====================================================================================
// Entry points
//=====================================================================================
        
//-------------------------------------------------------------------------------------
// Copies a rectangle from one image into another
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::CopyRectangle(
    const Image& srcImage,
    const Rect& srcRect,
    const Image& dstImage,
    DWORD filter,
    size_t xOffset,
    size_t yOffset)
{
    if (!srcImage.pixels || !dstImage.pixels)
        return E_POINTER;

    if (IsCompressed(srcImage.format) || IsCompressed(dstImage.format)
        || IsPlanar(srcImage.format) || IsPlanar(dstImage.format)
        || IsPalettized(srcImage.format) || IsPalettized(dstImage.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    // Validate rectangle/offset
    if (!srcRect.w || !srcRect.h || ((srcRect.x + srcRect.w) > srcImage.width) || ((srcRect.y + srcRect.h) > srcImage.height))
    {
        return E_INVALIDARG;
    }

    if (((xOffset + srcRect.w) > dstImage.width) || ((yOffset + srcRect.h) > dstImage.height))
    {
        return E_INVALIDARG;
    }

    // Compute source bytes-per-pixel
    size_t sbpp = BitsPerPixel(srcImage.format);
    if (!sbpp)
        return E_FAIL;

    if (sbpp < 8)
    {
        // We don't support monochrome (DXGI_FORMAT_R1_UNORM)
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    const uint8_t* pEndSrc = srcImage.pixels + srcImage.rowPitch*srcImage.height;
    const uint8_t* pEndDest = dstImage.pixels + dstImage.rowPitch*dstImage.height;

    // Round to bytes
    sbpp = (sbpp + 7) / 8;

    const uint8_t* pSrc = srcImage.pixels + (srcRect.y * srcImage.rowPitch) + (srcRect.x * sbpp);

    if (srcImage.format == dstImage.format)
    {
        // Direct copy case (avoid intermediate conversions)
        uint8_t* pDest = dstImage.pixels + (yOffset * dstImage.rowPitch) + (xOffset * sbpp);
        const size_t copyW = srcRect.w * sbpp;
        for (size_t h = 0; h < srcRect.h; ++h)
        {
            if (((pSrc + copyW) > pEndSrc) || (pDest > pEndDest))
                return E_FAIL;

            memcpy_s(pDest, pEndDest - pDest, pSrc, copyW);

            pSrc += srcImage.rowPitch;
            pDest += dstImage.rowPitch;
        }

        return S_OK;
    }

    // Compute destination bytes-per-pixel (not the same format as source)
    size_t dbpp = BitsPerPixel(dstImage.format);
    if (!dbpp)
        return E_FAIL;

    if (dbpp < 8)
    {
        // We don't support monochrome (DXGI_FORMAT_R1_UNORM)
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    // Round to bytes
    dbpp = (dbpp + 7) / 8;

    uint8_t* pDest = dstImage.pixels + (yOffset * dstImage.rowPitch) + (xOffset * dbpp);

    ScopedAlignedArrayXMVECTOR scanline(reinterpret_cast<XMVECTOR*>(_aligned_malloc((sizeof(XMVECTOR)*srcRect.w), 16)));
    if (!scanline)
        return E_OUTOFMEMORY;

    const size_t copyS = srcRect.w * sbpp;
    const size_t copyD = srcRect.w * dbpp;

    for (size_t h = 0; h < srcRect.h; ++h)
    {
        if (((pSrc + copyS) > pEndSrc) || ((pDest + copyD) > pEndDest))
            return E_FAIL;

        if (!_LoadScanline(scanline.get(), srcRect.w, pSrc, copyS, srcImage.format))
            return E_FAIL;

        _ConvertScanline(scanline.get(), srcRect.w, dstImage.format, srcImage.format, filter);

        if (!_StoreScanline(pDest, copyD, dstImage.format, scanline.get(), srcRect.w))
            return E_FAIL;

        pSrc += srcImage.rowPitch;
        pDest += dstImage.rowPitch;
    }

    return S_OK;
}

    
//-------------------------------------------------------------------------------------
// Computes the Mean-Squared-Error (MSE) between two images
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::ComputeMSE(
    const Image& image1,
    const Image& image2,
    float& mse,
    float* mseV,
    DWORD flags)
{
    if (!image1.pixels || !image2.pixels)
        return E_POINTER;

    if (image1.width != image2.width || image1.height != image2.height)
        return E_INVALIDARG;

    if (!IsValid(image1.format) || !IsValid(image2.format))
        return E_INVALIDARG;

    if (IsPlanar(image1.format) || IsPlanar(image2.format)
        || IsPalettized(image1.format) || IsPalettized(image2.format)
        || IsTypeless(image1.format) || IsTypeless(image2.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    if (IsCompressed(image1.format))
    {
        if (IsCompressed(image2.format))
        {
            // Case 1: both images are compressed, expand to RGBA32F
            ScratchImage temp1;
            HRESULT hr = Decompress(image1, DXGI_FORMAT_R32G32B32A32_FLOAT, temp1);
            if (FAILED(hr))
                return hr;

            ScratchImage temp2;
            hr = Decompress(image2, DXGI_FORMAT_R32G32B32A32_FLOAT, temp2);
            if (FAILED(hr))
                return hr;

            const Image* img1 = temp1.GetImage(0, 0, 0);
            const Image* img2 = temp2.GetImage(0, 0, 0);
            if (!img1 || !img2)
                return E_POINTER;

            return ComputeMSE_(*img1, *img2, mse, mseV, flags);
        }
        else
        {
            // Case 2: image1 is compressed, expand to RGBA32F
            ScratchImage temp;
            HRESULT hr = Decompress(image1, DXGI_FORMAT_R32G32B32A32_FLOAT, temp);
            if (FAILED(hr))
                return hr;

            const Image* img = temp.GetImage(0, 0, 0);
            if (!img)
                return E_POINTER;

            return ComputeMSE_(*img, image2, mse, mseV, flags);
        }
    }
    else
    {
        if (IsCompressed(image2.format))
        {
            // Case 3: image2 is compressed, expand to RGBA32F
            ScratchImage temp;
            HRESULT hr = Decompress(image2, DXGI_FORMAT_R32G32B32A32_FLOAT, temp);
            if (FAILED(hr))
                return hr;

            const Image* img = temp.GetImage(0, 0, 0);
            if (!img)
                return E_POINTER;

            return ComputeMSE_(image1, *img, mse, mseV, flags);
        }
        else
        {
            // Case 4: neither image is compressed
            return ComputeMSE_(image1, image2, mse, mseV, flags);
        }
    }
}


//-------------------------------------------------------------------------------------
// Evaluates a user-supplied function for all the pixels in the image
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::EvaluateImage(
    const Image& image,
    std::function<void __cdecl(_In_reads_(width) const XMVECTOR* pixels, size_t width, size_t y)> pixelFunc)
{
    if (image.width > UINT32_MAX
        || image.height > UINT32_MAX)
        return E_INVALIDARG;

    if (!IsValid(image.format))
        return E_INVALIDARG;

    if (IsPlanar(image.format) || IsPalettized(image.format) || IsTypeless(image.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    if (IsCompressed(image.format))
    {
        ScratchImage temp;
        HRESULT hr = Decompress(image, DXGI_FORMAT_R32G32B32A32_FLOAT, temp);
        if (FAILED(hr))
            return hr;

        const Image* img = temp.GetImage(0, 0, 0);
        if (!img)
            return E_POINTER;

        return EvaluateImage_(*img, pixelFunc);
    }
    else
    {
        return EvaluateImage_(image, pixelFunc);
    }
}

_Use_decl_annotations_
HRESULT DirectX::EvaluateImage(
    const Image* images,
    size_t nimages,
    const TexMetadata& metadata,
    std::function<void __cdecl(_In_reads_(width) const XMVECTOR* pixels, size_t width, size_t y)> pixelFunc)
{
    if (!images || !nimages)
        return E_INVALIDARG;

    if (!IsValid(metadata.format))
        return E_INVALIDARG;

    if (IsPlanar(metadata.format) || IsPalettized(metadata.format) || IsTypeless(metadata.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    if (metadata.width > UINT32_MAX
        || metadata.height > UINT32_MAX)
        return E_INVALIDARG;

    if (metadata.IsVolumemap() && metadata.depth > UINT16_MAX)
        return E_INVALIDARG;

    ScratchImage temp;
    DXGI_FORMAT format = metadata.format;
    if (IsCompressed(format))
    {
        HRESULT hr = Decompress(images, nimages, metadata, DXGI_FORMAT_R32G32B32A32_FLOAT, temp);
        if (FAILED(hr))
            return hr;

        if (nimages != temp.GetImageCount())
            return E_UNEXPECTED;

        images = temp.GetImages();
        format = DXGI_FORMAT_R32G32B32A32_FLOAT;
    }

    switch (metadata.dimension)
    {
    case TEX_DIMENSION_TEXTURE1D:
    case TEX_DIMENSION_TEXTURE2D:
        for (size_t index = 0; index < nimages; ++index)
        {
            const Image& img = images[index];
            if (img.format != format)
                return E_FAIL;

            if ((img.width > UINT32_MAX) || (img.height > UINT32_MAX))
                return E_FAIL;

            HRESULT hr = EvaluateImage_(img, pixelFunc);
            if (FAILED(hr))
                return hr;
        }
        break;

    case TEX_DIMENSION_TEXTURE3D:
    {
        size_t index = 0;
        size_t d = metadata.depth;
        for (size_t level = 0; level < metadata.mipLevels; ++level)
        {
            for (size_t slice = 0; slice < d; ++slice, ++index)
            {
                if (index >= nimages)
                    return E_FAIL;

                const Image& img = images[index];
                if (img.format != format)
                    return E_FAIL;

                if ((img.width > UINT32_MAX) || (img.height > UINT32_MAX))
                    return E_FAIL;

                HRESULT hr = EvaluateImage_(img, pixelFunc);
                if (FAILED(hr))
                    return hr;
            }

            if (d > 1)
                d >>= 1;
        }
    }
    break;

    default:
        return E_FAIL;
    }

    return S_OK;
}


//-------------------------------------------------------------------------------------
// Use a user-supplied function to compute a new image from an input image
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::TransformImage(
    const Image& image,
    std::function<void __cdecl(_Out_writes_(width) XMVECTOR* outPixels, _In_reads_(width) const XMVECTOR* inPixels, size_t width, size_t y)> pixelFunc,
    ScratchImage& result)
{
    if (image.width > UINT32_MAX
        || image.height > UINT32_MAX)
        return E_INVALIDARG;

    if (IsPlanar(image.format) || IsPalettized(image.format) || IsCompressed(image.format) || IsTypeless(image.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    HRESULT hr = result.Initialize2D(image.format, image.width, image.height, 1, 1);
    if (FAILED(hr))
        return hr;

    const Image* dimg = result.GetImage(0, 0, 0);
    if (!dimg)
    {
        result.Release();
        return E_POINTER;
    }

    hr = TransformImage_(image, pixelFunc, *dimg);
    if (FAILED(hr))
    {
        result.Release();
        return hr;
    }
    
    return S_OK;
}

_Use_decl_annotations_
HRESULT DirectX::TransformImage(
    const Image* srcImages,
    size_t nimages, const TexMetadata& metadata,
    std::function<void __cdecl(_Out_writes_(width) XMVECTOR* outPixels, _In_reads_(width) const XMVECTOR* inPixels, size_t width, size_t y)> pixelFunc,
    ScratchImage& result)
{
    if (!srcImages || !nimages)
        return E_INVALIDARG;

    if (IsPlanar(metadata.format) || IsPalettized(metadata.format) || IsCompressed(metadata.format) || IsTypeless(metadata.format))
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

    if (metadata.width > UINT32_MAX
        || metadata.height > UINT32_MAX)
        return E_INVALIDARG;

    if (metadata.IsVolumemap() && metadata.depth > UINT16_MAX)
        return E_INVALIDARG;

    HRESULT hr = result.Initialize(metadata);
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

    switch (metadata.dimension)
    {
    case TEX_DIMENSION_TEXTURE1D:
    case TEX_DIMENSION_TEXTURE2D:
        for (size_t index = 0; index < nimages; ++index)
        {
            const Image& src = srcImages[index];
            if (src.format != metadata.format)
            {
                result.Release();
                return E_FAIL;
            }

            if ((src.width > UINT32_MAX) || (src.height > UINT32_MAX))
            {
                result.Release();
                return E_FAIL;
            }

            const Image& dst = dest[index];

            if (src.width != dst.width || src.height != dst.height)
            {
                result.Release();
                return E_FAIL;
            }

            hr = TransformImage_(src, pixelFunc, dst);
            if (FAILED(hr))
            {
                result.Release();
                return hr;
            }
        }
        break;

    case TEX_DIMENSION_TEXTURE3D:
    {
        size_t index = 0;
        size_t d = metadata.depth;
        for (size_t level = 0; level < metadata.mipLevels; ++level)
        {
            for (size_t slice = 0; slice < d; ++slice, ++index)
            {
                if (index >= nimages)
                {
                    result.Release();
                    return E_FAIL;
                }

                const Image& src = srcImages[index];
                if (src.format != metadata.format)
                {
                    result.Release();
                    return E_FAIL;
                }

                if ((src.width > UINT32_MAX) || (src.height > UINT32_MAX))
                {
                    result.Release();
                    return E_FAIL;
                }

                const Image& dst = dest[index];

                if (src.width != dst.width || src.height != dst.height)
                {
                    result.Release();
                    return E_FAIL;
                }

                hr = TransformImage_(src, pixelFunc, dst);
                if (FAILED(hr))
                {
                    result.Release();
                    return hr;
                }
            }

            if (d > 1)
                d >>= 1;
        }
    }
    break;

    default:
        result.Release();
        return E_FAIL;
    }

    return S_OK;
}
