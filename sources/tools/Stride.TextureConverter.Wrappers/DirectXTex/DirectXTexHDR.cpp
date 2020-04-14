//-------------------------------------------------------------------------------------
// DirectXTexHDR.cpp
//  
// DirectX Texture Library - Radiance HDR (RGBE) file format reader/writer
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

//
// In theory HDR (RGBE) Radiance files can have any of the following data orientations
//
//      +X width +Y height
//      +X width -Y height
//      -X width +Y height
//      -X width -Y height
//      +Y height +X width 
//      -Y height +X width 
//      +Y height -X width 
//      -Y height -X width 
//
// All HDR files we've encountered are always written as "-Y height +X width", so
// we support only that one as that's what other Radiance parsing code does as well.
//

//Uncomment to disable the use of adapative RLE encoding when writing an HDR. Used for testing only.
//#define DISABLE_COMPRESS

//Uncomment to use "old colors" standard RLE encoding when writing an HDR. Used for testing only.
//#define WRITE_OLD_COLORS

using namespace DirectX;

namespace
{
    const char g_Signature[] = "#?RADIANCE";
    const char g_Format[] = "FORMAT=";
    const char g_Exposure[] = "EXPOSURE=";

    const char g_sRGBE[] = "32-bit_rle_rgbe";
    const char g_sXYZE[] = "32-bit_rle_xyze";

    const char g_Header[] =
        "#?RADIANCE\n"\
        "FORMAT=32-bit_rle_rgbe\n"\
        "\n"\
        "-Y %u +X %u\n";

    inline size_t FindEOL(const char* str, size_t maxlen)
    {
        size_t pos = 0;

        while (pos < maxlen)
        {
            if (str[pos] == '\n')
                return pos;
            else if (str[pos] == '\0')
                return size_t(-1);
            ++pos;
        }

        return 0;
    }

    //-------------------------------------------------------------------------------------
    // Decodes HDR header
    //-------------------------------------------------------------------------------------
    HRESULT DecodeHDRHeader(
        _In_reads_bytes_(size) const void* pSource,
        size_t size,
        _Out_ TexMetadata& metadata,
        size_t& offset,
        float& exposure)
    {
        if (!pSource)
            return E_INVALIDARG;

        memset(&metadata, 0, sizeof(TexMetadata));

        exposure = 1.f;
        
        if (size < sizeof(g_Signature))
        {
            return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }

        // Verify magic signature
        if (memcmp(pSource, g_Signature, sizeof(g_Signature) - 1) != 0)
        {
            return E_FAIL;
        }

        // Process first part of header
        bool formatFound = false;
        const char* info = reinterpret_cast<const char*>(pSource);
        while (size > 0)
        {
            if (*info == '\n')
            {
                ++info;
                --size;
                break;
            }

            const size_t formatLen = sizeof(g_Format) - 1;
            const size_t exposureLen = sizeof(g_Exposure) - 1;
            if ((size > formatLen) && memcmp(info, g_Format, formatLen) == 0)
            {
                info += formatLen;
                size -= formatLen;

                // Trim whitespace
                while (*info == ' ' || *info == '\t')
                {
                    if (--size == 0)
                        return E_FAIL;
                    ++info;
                }

                static_assert(sizeof(g_sRGBE) == sizeof(g_sXYZE), "Format strings length mismatch");

                const size_t encodingLen = sizeof(g_sRGBE) - 1;

                if (size < encodingLen)
                {
                    return E_FAIL;
                }

                if (memcmp(info, g_sRGBE, encodingLen) != 0 && memcmp(info, g_sXYZE, encodingLen) != 0)
                {
                    return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
                }

                formatFound = true;

                size_t len = FindEOL(info, size);
                if (len == size_t(-1))
                {
                    return E_FAIL;
                }

                info += len + 1;
                size -= len + 1;
            }
            else if ((size > exposureLen) && memcmp(info, g_Exposure, exposureLen) == 0)
            {
                info += exposureLen;
                size -= exposureLen;

                // Trim whitespace
                while (*info == ' ' || *info == '\t')
                {
                    if (--size == 0)
                        return E_FAIL;
                    ++info;
                }

                size_t len = FindEOL(info, size);
                if (len == size_t(-1)
                    || len < 1)
                {
                    return E_FAIL;
                }

                char buff[32] = {};
                strncpy_s(buff, info, std::min<size_t>(31, len));

                float newExposure = static_cast<float>(atof(buff));
                if ((newExposure >= 1e-12) && (newExposure <= 1e12))
                {
                    // Note that we ignore strange exposure values (like EXPOSURE=0)
                    exposure *= newExposure;
                }

                info += len + 1;
                size -= len + 1;
            }
            else
            {
                size_t len = FindEOL(info, size);
                if (len == size_t(-1))
                {
                    return E_FAIL;
                }

                info += len + 1;
                size -= len + 1;
            }
        }

        if (!formatFound)
        {
            return E_FAIL;
        }

        // Get orientation
        char orient[256] = {};

        size_t len = FindEOL(info, std::min<size_t>(sizeof(orient), size - 1));
        if (len == size_t(-1)
            || len <= 2)
        {
            return E_FAIL;
        }

        strncpy_s(orient, info, len);

        if (orient[0] != '-' && orient[1] != 'Y')
        {
            // We only support the -Y +X orientation (see top of file)
            return HRESULT_FROM_WIN32(
                ((orient[0] == '+' || orient[0] == '-') && (orient[1] == 'X' || orient[1] == 'Y'))
                ? ERROR_NOT_SUPPORTED : ERROR_INVALID_DATA
            );
        }

        uint32_t height = 0;
        if (sscanf_s(orient + 2, "%u", &height) != 1)
        {
            return E_FAIL;
        }

        const char* ptr = orient + 2;
        while (*ptr != 0 && *ptr != '-' && *ptr != '+')
            ++ptr;

        if (*ptr == 0)
        {
            return E_FAIL;
        }
        else if (*ptr != '+')
        {
            // We only support the -Y +X orientation (see top of file)
            return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        }

        ++ptr;
        if (*ptr == 0 || (*ptr != 'X' && *ptr != 'Y'))
        {
            return E_FAIL;
        }
        else if (*ptr != 'X')
        {
            // We only support the -Y +X orientation (see top of file)
            return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        }

        ++ptr;
        uint32_t width;
        if (sscanf_s(ptr, "%u", &width) != 1)
        {
            return E_FAIL;
        }

        info += len + 1;
        size -= len + 1;

        if (!width || !height)
        {
            return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }

        if (size == 0)
        {
            return E_FAIL;
        }

        offset = info - reinterpret_cast<const char*>(pSource);

        metadata.width = width;
        metadata.height = height;
        metadata.depth = metadata.arraySize = metadata.mipLevels = 1;
        metadata.format = DXGI_FORMAT_R32G32B32A32_FLOAT;
        metadata.dimension = TEX_DIMENSION_TEXTURE2D;

        return S_OK;
    }

    //-------------------------------------------------------------------------------------
    // FloatToRGBE
    //-------------------------------------------------------------------------------------
    inline void FloatToRGBE(_Out_writes_(width*4) uint8_t* pDestination, _In_reads_(width*fpp) const float* pSource, size_t width, _In_range_(3, 4) int fpp)
    {
        auto ePtr = pSource + width * fpp;

        for (size_t j = 0; j < width; ++j)
        {
            if (pSource + 2 >= ePtr) break;
            float r = pSource[0] >= 0.f ? pSource[0] : 0.f;
            float g = pSource[1] >= 0.f ? pSource[1] : 0.f;
            float b = pSource[2] >= 0.f ? pSource[2] : 0.f;
            pSource += fpp;

            const float max_xy = (r > g) ? r : g;
            float max_xyz = (max_xy > b) ? max_xy : b;

            if (max_xyz > 1e-32)
            {
                int e;
                max_xyz = frexpf(max_xyz, &e) * 256.f / max_xyz;
                e += 128;

                uint8_t red = uint8_t(r * max_xyz);
                uint8_t green = uint8_t(g * max_xyz);
                uint8_t blue = uint8_t(b * max_xyz);

                pDestination[0] = red;
                pDestination[1] = green;
                pDestination[2] = blue;
                pDestination[3] = (red || green || blue) ? uint8_t(e & 0xff) : 0;
            }
            else
            {
                pDestination[0] = pDestination[1] = pDestination[2] = pDestination[3] = 0;
            }

            pDestination += 4;
        }
    }

    //-------------------------------------------------------------------------------------
    // Encode using Adapative RLE
    //-------------------------------------------------------------------------------------
    _Success_(return > 0)
        size_t EncodeRLE(_Out_writes_(width * 4) uint8_t* enc, _In_reads_(width * 4) const uint8_t* rgbe, size_t rowPitch, size_t width)
    {
        if (width < 8 || width > 32767)
        {
            // Don't try to compress too narrow or too wide scan-lines
            return 0;
        }

#ifdef WRITE_OLD_COLORS
        size_t encSize = 0;

        const uint8_t* scanPtr = rgbe;
        for (size_t pixelCount = 0; pixelCount < width;)
        {
            size_t spanLen = 1;
            const uint32_t* spanPtr = reinterpret_cast<const uint32_t*>(scanPtr);
            while (pixelCount + spanLen < width && spanLen < 32767)
            {
                if (spanPtr[spanLen] == *spanPtr)
                {
                    ++spanLen;
                }
                else
                    break;
            }

            if (spanLen > 2)
            {
                if (scanPtr[0] == 1 && scanPtr[1] == 1 && scanPtr[2] == 1)
                {
                    return 0;
                }

                if (encSize + 8 > rowPitch)
                    return 0;

                uint8_t rleLen = static_cast<uint8_t>(std::min<size_t>(spanLen - 1, 255));

                enc[0] = scanPtr[0];
                enc[1] = scanPtr[1];
                enc[2] = scanPtr[2];
                enc[3] = scanPtr[3];
                enc[4] = 1;
                enc[5] = 1;
                enc[6] = 1;
                enc[7] = rleLen;
                enc += 8;
                encSize += 8;

                size_t remaining = spanLen - 1 - rleLen;

                if (remaining > 0)
                {
                    rleLen = static_cast<uint8_t>(remaining >> 8);

                    if (rleLen > 0)
                    {
                        if (encSize + 4 > rowPitch)
                            return 0;

                        enc[0] = 1;
                        enc[1] = 1;
                        enc[2] = 1;
                        enc[3] = rleLen;
                        enc += 4;
                        encSize += 4;

                        remaining -= (rleLen << 8);
                    }

                    while (remaining > 0)
                    {
                        if (encSize + 4 > rowPitch)
                            return 0;

                        enc[0] = scanPtr[0];
                        enc[1] = scanPtr[1];
                        enc[2] = scanPtr[2];
                        enc[3] = scanPtr[3];
                        enc += 4;
                        encSize += 4;

                        --remaining;
                    }
                }

                scanPtr += spanLen * 4;
                pixelCount += spanLen;
            }
            else if (scanPtr[0] == 1 && scanPtr[1] == 1 && scanPtr[2] == 1)
            {
                return 0;
            }
            else
            {
                if (encSize + 4 > rowPitch)
                    return 0;

                enc[0] = scanPtr[0];
                enc[1] = scanPtr[1];
                enc[2] = scanPtr[2];
                enc[3] = scanPtr[3];
                enc += 4;
                encSize += 4;
                ++pixelCount;
                scanPtr += 4;
            }
        }

        return encSize;
#else
        enc[0] = 2;
        enc[1] = 2;
        enc[2] = uint8_t(width >> 8);
        enc[3] = uint8_t(width & 0xff);
        enc += 4;
        size_t encSize = 4;

        uint8_t scan[128] = {};

        for (int channel = 0; channel < 4; ++channel)
        {
            const uint8_t* spanPtr = rgbe + channel;
            for (size_t pixelCount = 0; pixelCount < width;)
            {
                uint8_t spanLen = 1;
                while (pixelCount + spanLen < width && spanLen < 127)
                {
                    if (spanPtr[spanLen * 4] == *spanPtr)
                    {
                        ++spanLen;
                    }
                    else
                        break;
                }

                if (spanLen > 1)
                {
                    if (encSize + 2 > rowPitch)
                        return 0;

                    enc[0] = 128 + spanLen;
                    enc[1] = *spanPtr;
                    enc += 2;
                    encSize += 2;
                    spanPtr += spanLen * 4;
                    pixelCount += spanLen;
                }
                else
                {
                    uint8_t runLen = 1;
                    scan[0] = *spanPtr;
                    while (pixelCount + runLen < width && runLen < 127)
                    {
                        if (spanPtr[(runLen - 1) * 4] != spanPtr[runLen * 4])
                        {
                            scan[runLen++] = spanPtr[runLen * 4];
                        }
                        else
                            break;
                    }

                    if (encSize + runLen + 1 > rowPitch)
                        return 0;

                    *enc++ = runLen;
                    memcpy(enc, scan, runLen);
                    enc += runLen;
                    encSize += runLen + 1;
                    spanPtr += runLen * 4;
                    pixelCount += runLen;
                }
            }
        }

        return encSize;
#endif
    }
}


//=====================================================================================
// Entry-points
//=====================================================================================

//-------------------------------------------------------------------------------------
// Obtain metadata from HDR file in memory/on disk
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::GetMetadataFromHDRMemory(const void* pSource, size_t size, TexMetadata& metadata)
{
    if (!pSource || size == 0)
        return E_INVALIDARG;

    size_t offset;
    float exposure;
    return DecodeHDRHeader(pSource, size, metadata, offset, exposure);
}

_Use_decl_annotations_
HRESULT DirectX::GetMetadataFromHDRFile(const wchar_t* szFile, TexMetadata& metadata)
{
    if (!szFile)
        return E_INVALIDARG;

#if (_WIN32_WINNT >= _WIN32_WINNT_WIN8)
    ScopedHandle hFile(safe_handle(CreateFile2(szFile, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, nullptr)));
#else
    ScopedHandle hFile(safe_handle(CreateFileW(szFile, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING,
        FILE_FLAG_SEQUENTIAL_SCAN, nullptr)));
#endif
    if (!hFile)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // Get the file size
    FILE_STANDARD_INFO fileInfo;
    if (!GetFileInformationByHandleEx(hFile.get(), FileStandardInfo, &fileInfo, sizeof(fileInfo)))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // File is too big for 32-bit allocation, so reject read (4 GB should be plenty large enough for a valid HDR file)
    if (fileInfo.EndOfFile.HighPart > 0)
    {
        return HRESULT_FROM_WIN32(ERROR_FILE_TOO_LARGE);
    }

    // Need at least enough data to fill the standard header to be a valid HDR
    if (fileInfo.EndOfFile.LowPart < sizeof(g_Signature))
    {
        return E_FAIL;
    }

    // Read the first part of the file to find the header
    uint8_t header[8192];
    DWORD bytesRead = 0;
    if (!ReadFile(hFile.get(), header, std::min<DWORD>(sizeof(header), fileInfo.EndOfFile.LowPart), &bytesRead, nullptr))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    size_t offset;
    float exposure;
    return DecodeHDRHeader(header, bytesRead, metadata, offset, exposure);
}


//-------------------------------------------------------------------------------------
// Load a HDR file in memory
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::LoadFromHDRMemory(const void* pSource, size_t size, TexMetadata* metadata, ScratchImage& image)
{
    if (!pSource || size == 0)
        return E_INVALIDARG;

    image.Release();

    size_t offset;
    float exposure;
    TexMetadata mdata;
    HRESULT hr = DecodeHDRHeader(pSource, size, mdata, offset, exposure);
    if (FAILED(hr))
        return hr;

    if (offset > size)
        return E_FAIL;

    size_t remaining = size - offset;
    if (remaining == 0)
        return E_FAIL;

    hr = image.Initialize2D(mdata.format, mdata.width, mdata.height, 1, 1);
    if (FAILED(hr))
        return hr;

    // Copy pixels
    auto sourcePtr = reinterpret_cast<const uint8_t*>(pSource) + offset;

    size_t pixelLen = remaining;

    const Image* img = image.GetImage(0, 0, 0);
    if (!img)
    {
        image.Release();
        return E_POINTER;
    }

    auto destPtr = img->pixels;

#ifdef _DEBUG
    memset(img->pixels, 0xFF, img->rowPitch * img->height);
#endif

    for (size_t scan = 0; scan < mdata.height; ++scan)
    {
        if (pixelLen < 4)
        {
            image.Release();
            return E_FAIL;
        }

        uint8_t inColor[4];
        memcpy(inColor, sourcePtr, 4);
        sourcePtr += 4;
        pixelLen -= 4;

        auto scanLine = reinterpret_cast<float*>(destPtr);
        
        if (inColor[0] == 2 && inColor[1] == 2 && inColor[2] < 128)
        {
            // Adaptive Run Length Encoding (RLE)
            if (size_t((inColor[2] << 8) + inColor[3]) != mdata.width)
            {
                image.Release();
                return E_FAIL;
            }

            for (int channel = 0; channel < 4; ++channel)
            {
                auto pixelLoc = scanLine + channel;
                for(size_t pixelCount = 0; pixelCount < mdata.width;)
                {
                    if (pixelLen < 2)
                    {
                        image.Release();
                        return E_FAIL;
                    }

                    assert(sourcePtr < (reinterpret_cast<const uint8_t*>(pSource) + size));

                    uint8_t runLen = *sourcePtr;
                    if (runLen > 128)
                    {
                        runLen &= 127;
                        if (pixelCount + runLen > mdata.width)
                        {
                            image.Release();
                            return E_FAIL;
                        }

                        float val = static_cast<float>(sourcePtr[1]);
                        for (uint8_t j = 0; j < runLen; ++j)
                        {
                            *pixelLoc = val;
                            pixelLoc += 4;
                        }
                        pixelCount += runLen;
                        sourcePtr += 2;
                        pixelLen -= 2;
                    }
                    else if ((size < size_t(runLen + 1)) || ((pixelCount + runLen) > mdata.width))
                    {
                        image.Release();
                        return E_FAIL;
                    }
                    else
                    {
                        ++sourcePtr;
                        for (uint8_t j = 0; j < runLen; ++j)
                        {
                            float val = static_cast<float>(*sourcePtr++);
                            *pixelLoc = val;
                            pixelLoc += 4;
                        }
                        pixelCount += runLen;
                        pixelLen -= runLen + 1;
                    }
                }
            }
        }
        else
        {
            auto pixelLoc = scanLine;

            float prevColor[4];
            prevColor[0] = inColor[0];
            prevColor[1] = inColor[1];
            prevColor[2] = inColor[2];
            prevColor[3] = inColor[3];

            int bitShift = 0;
            for (size_t pixelCount = 0; pixelCount < mdata.width;)
            {
                if (inColor[0] == 1 && inColor[1] == 1 && inColor[2] == 1)
                {
                    if (bitShift > 24)
                    {
                        image.Release();
                        return E_FAIL;
                    }

                    // "Standard" Run Length Encoding
                    size_t spanLen = size_t(inColor[3]) << bitShift;
                    if (spanLen + pixelCount > mdata.width)
                    {
                        image.Release();
                        return E_FAIL;
                    }

                    for (size_t j = 0; j < spanLen; ++j)
                    {
                        pixelLoc[0] = prevColor[0];
                        pixelLoc[1] = prevColor[1];
                        pixelLoc[2] = prevColor[2];
                        pixelLoc[3] = prevColor[3];
                        pixelLoc += 4;
                    }
                    pixelCount += spanLen;
                    bitShift += 8;
                }
                else
                {
                    // Uncompressed
                    pixelLoc[0] = prevColor[0] = inColor[0];
                    pixelLoc[1] = prevColor[1] = inColor[1];
                    pixelLoc[2] = prevColor[2] = inColor[2];
                    pixelLoc[3] = prevColor[3] = inColor[3];
                    bitShift = 0;
                    ++pixelCount;
                    pixelLoc += 4;
                }

                if (pixelCount >= mdata.width)
                    break;

                if (pixelLen < 4)
                {
                    image.Release();
                    return E_FAIL;
                }

                memcpy(inColor, sourcePtr, 4);
                sourcePtr += 4;
                pixelLen -= 4;
            }
        }

        destPtr += img->rowPitch;
    }

    // Transform values
    {
        auto fdata = reinterpret_cast<float*>(image.GetPixels());

        for (size_t j = 0; j < image.GetPixelsSize(); j += 16)
        {
            int exponent = static_cast<int>(fdata[3]);
            fdata[0] = 1.0f / exposure*ldexpf((fdata[0] + 0.5f), exponent - (128 + 8));
            fdata[1] = 1.0f / exposure*ldexpf((fdata[1] + 0.5f), exponent - (128 + 8));
            fdata[2] = 1.0f / exposure*ldexpf((fdata[2] + 0.5f), exponent - (128 + 8));
            fdata[3] = 1.f;

            fdata += 4;
        }
    }

    if (metadata)
        memcpy(metadata, &mdata, sizeof(TexMetadata));

    return S_OK;
}


//-------------------------------------------------------------------------------------
// Load a HDR file from disk
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::LoadFromHDRFile(const wchar_t* szFile, TexMetadata* metadata, ScratchImage& image)
{
    if (!szFile)
        return E_INVALIDARG;

    image.Release();

#if (_WIN32_WINNT >= _WIN32_WINNT_WIN8)
    ScopedHandle hFile(safe_handle(CreateFile2(szFile, GENERIC_READ, FILE_SHARE_READ, OPEN_EXISTING, nullptr)));
#else
    ScopedHandle hFile(safe_handle(CreateFileW(szFile, GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING,
        FILE_FLAG_SEQUENTIAL_SCAN, nullptr)));
#endif
    if (!hFile)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // Get the file size
    FILE_STANDARD_INFO fileInfo;
    if (!GetFileInformationByHandleEx(hFile.get(), FileStandardInfo, &fileInfo, sizeof(fileInfo)))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // File is too big for 32-bit allocation, so reject read (4 GB should be plenty large enough for a valid HDR file)
    if (fileInfo.EndOfFile.HighPart > 0)
    {
        return HRESULT_FROM_WIN32(ERROR_FILE_TOO_LARGE);
    }

    // Need at least enough data to fill the header to be a valid HDR
    if (fileInfo.EndOfFile.LowPart < sizeof(g_Signature))
    {
        return E_FAIL;
    }

    // Read file
    std::unique_ptr<uint8_t[]> temp(new (std::nothrow) uint8_t[fileInfo.EndOfFile.LowPart]);
    if (!temp)
    {
        return E_OUTOFMEMORY;
    }

    DWORD bytesRead = 0;
    if (!ReadFile(hFile.get(), temp.get(), fileInfo.EndOfFile.LowPart, &bytesRead, nullptr))
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    if (bytesRead != fileInfo.EndOfFile.LowPart)
    {
        return E_FAIL;
    }

    return LoadFromHDRMemory(temp.get(), fileInfo.EndOfFile.LowPart, metadata, image);
}


//-------------------------------------------------------------------------------------
// Save a HDR file to memory
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::SaveToHDRMemory(const Image& image, Blob& blob)
{
    if (!image.pixels)
        return E_POINTER;

    if (image.width > 32767 || image.height > 32767)
    {
        // Images larger than this can't be RLE encoded. They are technically allowed as
        // uncompresssed, but we just don't support them.
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    int fpp;
    switch (image.format)
    {
    case DXGI_FORMAT_R32G32B32A32_FLOAT:
        fpp = 4;
        break;

    case DXGI_FORMAT_R32G32B32_FLOAT:
        fpp = 3;
        break;

    default:
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    blob.Release();

    char header[256] = {};
    sprintf_s(header, g_Header, image.height, image.width);

    auto headerLen = static_cast<DWORD>(strlen(header));

    size_t rowPitch = image.width * 4;
    size_t slicePitch = image.height * rowPitch;

    HRESULT hr = blob.Initialize(headerLen + slicePitch);
    if (FAILED(hr))
        return hr;

    // Copy header
    auto dPtr = reinterpret_cast<uint8_t*>(blob.GetBufferPointer());
    assert(dPtr != 0);
    memcpy_s(dPtr, blob.GetBufferSize(), header, headerLen);
    dPtr += headerLen;

#ifdef DISABLE_COMPRESS
    // Uncompressed write
    auto sPtr = reinterpret_cast<const uint8_t*>(image.pixels);
    for (size_t scan = 0; scan < image.height; ++scan)
    {
        FloatToRGBE(dPtr, reinterpret_cast<const float*>(sPtr), image.width, fpp);
        dPtr += rowPitch;
        sPtr += image.rowPitch;
    }
#else
    std::unique_ptr<uint8_t[]> temp(new (std::nothrow) uint8_t[rowPitch * 2]);
    if (!temp)
    {
        blob.Release();
        return E_OUTOFMEMORY;
    }

    auto rgbe = temp.get();
    auto enc = temp.get() + rowPitch;

    auto sPtr = reinterpret_cast<const uint8_t*>(image.pixels);
    for (size_t scan = 0; scan < image.height; ++scan)
    {
        FloatToRGBE(rgbe, reinterpret_cast<const float*>(sPtr), image.width, fpp);
        sPtr += image.rowPitch;

        size_t encSize = EncodeRLE(enc, rgbe, rowPitch, image.width);
        if (encSize > 0)
        {
            memcpy(dPtr, enc, encSize);
            dPtr += encSize;
        }
        else
        {
            memcpy(dPtr, rgbe, rowPitch);
            dPtr += rowPitch;
        }
    }
#endif

    hr = blob.Trim(dPtr - reinterpret_cast<uint8_t*>(blob.GetBufferPointer()));
    if (FAILED(hr))
    {
        blob.Release();
        return hr;
    }

    return S_OK;
}


//-------------------------------------------------------------------------------------
// Save a HDR file to disk
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::SaveToHDRFile(const Image& image, const wchar_t* szFile)
{
    if (!szFile)
        return E_INVALIDARG;

    if (!image.pixels)
        return E_POINTER;

    if (image.width > 32767 || image.height > 32767)
    {
        // Images larger than this can't be RLE encoded. They are technically allowed as
        // uncompresssed, but we just don't support them.
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    int fpp;
    switch (image.format)
    {
    case DXGI_FORMAT_R32G32B32A32_FLOAT:
        fpp = 4;
        break;

    case DXGI_FORMAT_R32G32B32_FLOAT:
        fpp = 3;
        break;

    default:
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    // Create file and write header
#if (_WIN32_WINNT >= _WIN32_WINNT_WIN8)
    ScopedHandle hFile(safe_handle(CreateFile2(szFile, GENERIC_WRITE, 0, CREATE_ALWAYS, nullptr)));
#else
    ScopedHandle hFile(safe_handle(CreateFileW(szFile, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, 0, nullptr)));
#endif
    if (!hFile)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    auto_delete_file delonfail(hFile.get());

    size_t rowPitch = image.width * 4;
    size_t slicePitch = image.height * rowPitch;

    if (slicePitch < 65535)
    {
        // For small images, it is better to create an in-memory file and write it out
        Blob blob;

        HRESULT hr = SaveToHDRMemory(image, blob);
        if (FAILED(hr))
            return hr;

        // Write blob
        const DWORD bytesToWrite = static_cast<DWORD>(blob.GetBufferSize());
        DWORD bytesWritten;
        if (!WriteFile(hFile.get(), blob.GetBufferPointer(), bytesToWrite, &bytesWritten, nullptr))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        if (bytesWritten != bytesToWrite)
        {
            return E_FAIL;
        }
    }
    else
    {
        // Otherwise, write the image one scanline at a time...
        std::unique_ptr<uint8_t[]> temp(new (std::nothrow) uint8_t[rowPitch * 2]);
        if (!temp)
            return E_OUTOFMEMORY;

        auto rgbe = temp.get();

        // Write header
        char header[256] = {};
        sprintf_s(header, g_Header, image.height, image.width);

        auto headerLen = static_cast<DWORD>(strlen(header));

        DWORD bytesWritten;
        if (!WriteFile(hFile.get(), header, headerLen, &bytesWritten, nullptr))
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }

        if (bytesWritten != headerLen)
            return E_FAIL;

#ifdef DISABLE_COMPRESS
        // Uncompressed write
        auto sPtr = reinterpret_cast<const uint8_t*>(image.pixels);
        for (size_t scan = 0; scan < image.height; ++scan)
        {
            FloatToRGBE(rgbe, reinterpret_cast<const float*>(sPtr), image.width, fpp);
            sPtr += image.rowPitch;

            if (!WriteFile(hFile.get(), rgbe, static_cast<DWORD>(rowPitch), &bytesWritten, nullptr))
            {
                return HRESULT_FROM_WIN32(GetLastError());
            }

            if (bytesWritten != rowPitch)
                return E_FAIL;
        }
#else
        auto enc = temp.get() + rowPitch;

        auto sPtr = reinterpret_cast<const uint8_t*>(image.pixels);
        for (size_t scan = 0; scan < image.height; ++scan)
        {
            FloatToRGBE(rgbe, reinterpret_cast<const float*>(sPtr), image.width, fpp);
            sPtr += image.rowPitch;

            size_t encSize = EncodeRLE(enc, rgbe, rowPitch, image.width);
            if (encSize > 0)
            {
                if (!WriteFile(hFile.get(), enc, static_cast<DWORD>(encSize), &bytesWritten, nullptr))
                {
                    return HRESULT_FROM_WIN32(GetLastError());
                }

                if (bytesWritten != encSize)
                    return E_FAIL;
            }
            else
            {
                if (!WriteFile(hFile.get(), rgbe, static_cast<DWORD>(rowPitch), &bytesWritten, nullptr))
                {
                    return HRESULT_FROM_WIN32(GetLastError());
                }

                if (bytesWritten != rowPitch)
                    return E_FAIL;
            }
        }
#endif
    }

    delonfail.clear();

    return S_OK;
}
