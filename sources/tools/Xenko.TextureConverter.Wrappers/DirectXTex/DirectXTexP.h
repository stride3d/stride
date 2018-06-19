//-------------------------------------------------------------------------------------
// DirectXTexp.h
//  
// DirectX Texture Library - Private header
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

#pragma once

#pragma warning(push)
#pragma warning(disable : 4005)
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#define NODRAWTEXT
#define NOGDI
#define NOBITMAP
#define NOMCX
#define NOSERVICE
#define NOHELP
#pragma warning(pop)

#ifndef _WIN32_WINNT_WIN10
#define _WIN32_WINNT_WIN10 0x0A00
#endif

#include <windows.h>

#if defined(_XBOX_ONE) && defined(_TITLE)
#include <d3d12_x.h>
#include <d3d11_x.h>
#elif (_WIN32_WINNT >= _WIN32_WINNT_WIN10)
#include <d3d12.h>
#include <d3d11_4.h>
#else
#include <d3d11_1.h>
#endif

#include <directxmath.h>
#include <directxpackedvector.h>
#include <assert.h>

#include <malloc.h>
#include <memory>

#include <vector>

#include <stdlib.h>
#include <search.h>

#include <ole2.h>

#include "directxtex.h"

#include <wincodec.h>

#include <wrl\client.h>

#include "scoped.h"

#define TEX_FILTER_MASK 0xF00000

#define XBOX_DXGI_FORMAT_R10G10B10_7E3_A2_FLOAT DXGI_FORMAT(116)
#define XBOX_DXGI_FORMAT_R10G10B10_6E4_A2_FLOAT DXGI_FORMAT(117)
#define XBOX_DXGI_FORMAT_D16_UNORM_S8_UINT DXGI_FORMAT(118)
#define XBOX_DXGI_FORMAT_R16_UNORM_X8_TYPELESS DXGI_FORMAT(119)
#define XBOX_DXGI_FORMAT_X16_TYPELESS_G8_UINT DXGI_FORMAT(120)

#define WIN10_DXGI_FORMAT_P208 DXGI_FORMAT(130)
#define WIN10_DXGI_FORMAT_V208 DXGI_FORMAT(131)
#define WIN10_DXGI_FORMAT_V408 DXGI_FORMAT(132)

#ifndef XBOX_DXGI_FORMAT_R10G10B10_SNORM_A2_UNORM
#define XBOX_DXGI_FORMAT_R10G10B10_SNORM_A2_UNORM DXGI_FORMAT(189)
#endif

#define XBOX_DXGI_FORMAT_R4G4_UNORM DXGI_FORMAT(190)


namespace DirectX
{
    //---------------------------------------------------------------------------------
    // WIC helper functions
    DXGI_FORMAT __cdecl _WICToDXGI( _In_ const GUID& guid );
    bool __cdecl _DXGIToWIC( _In_ DXGI_FORMAT format, _Out_ GUID& guid, _In_ bool ignoreRGBvsBGR = false );

    DWORD __cdecl _CheckWICColorSpace( _In_ const GUID& sourceGUID, _In_ const GUID& targetGUID );

    inline WICBitmapDitherType __cdecl _GetWICDither( _In_ DWORD flags )
    {
        static_assert( TEX_FILTER_DITHER == 0x10000, "TEX_FILTER_DITHER* flag values don't match mask" );

        static_assert( TEX_FILTER_DITHER == WIC_FLAGS_DITHER, "TEX_FILTER_DITHER* should match WIC_FLAGS_DITHER*" );
        static_assert( TEX_FILTER_DITHER_DIFFUSION == WIC_FLAGS_DITHER_DIFFUSION, "TEX_FILTER_DITHER* should match WIC_FLAGS_DITHER*" );

        switch( flags & 0xF0000 )
        {
        case TEX_FILTER_DITHER:
            return WICBitmapDitherTypeOrdered4x4;

        case TEX_FILTER_DITHER_DIFFUSION:
            return WICBitmapDitherTypeErrorDiffusion;

        default:
            return WICBitmapDitherTypeNone;
        }
    }

    inline WICBitmapInterpolationMode __cdecl _GetWICInterp( _In_ DWORD flags )
    {
        static_assert( TEX_FILTER_POINT == 0x100000, "TEX_FILTER_ flag values don't match TEX_FILTER_MASK" );

        static_assert( TEX_FILTER_POINT == WIC_FLAGS_FILTER_POINT, "TEX_FILTER_* flags should match WIC_FLAGS_FILTER_*" );
        static_assert( TEX_FILTER_LINEAR == WIC_FLAGS_FILTER_LINEAR, "TEX_FILTER_* flags should match WIC_FLAGS_FILTER_*"  );
        static_assert( TEX_FILTER_CUBIC == WIC_FLAGS_FILTER_CUBIC, "TEX_FILTER_* flags should match WIC_FLAGS_FILTER_*"  );
        static_assert( TEX_FILTER_FANT == WIC_FLAGS_FILTER_FANT, "TEX_FILTER_* flags should match WIC_FLAGS_FILTER_*"  );

        switch( flags & TEX_FILTER_MASK )
        {
        case TEX_FILTER_POINT:
            return WICBitmapInterpolationModeNearestNeighbor;

        case TEX_FILTER_LINEAR:
            return WICBitmapInterpolationModeLinear;

        case TEX_FILTER_CUBIC:
            return WICBitmapInterpolationModeCubic;

        case TEX_FILTER_FANT:
        default:
            return WICBitmapInterpolationModeFant;
        }
    }

    //---------------------------------------------------------------------------------
    // Image helper functions
    void __cdecl _DetermineImageArray( _In_ const TexMetadata& metadata, _In_ DWORD cpFlags,
                                       _Out_ size_t& nImages, _Out_ size_t& pixelSize );

    _Success_(return != false)
    bool __cdecl _SetupImageArray( _In_reads_bytes_(pixelSize) uint8_t *pMemory, _In_ size_t pixelSize,
                                   _In_ const TexMetadata& metadata, _In_ DWORD cpFlags,
                                   _Out_writes_(nImages) Image* images, _In_ size_t nImages );

    //---------------------------------------------------------------------------------
    // Conversion helper functions

    enum TEXP_SCANLINE_FLAGS
    {
        TEXP_SCANLINE_NONE          = 0,
        TEXP_SCANLINE_SETALPHA      = 0x1,  // Set alpha channel to known opaque value
        TEXP_SCANLINE_LEGACY        = 0x2,  // Enables specific legacy format conversion cases
    };

    enum CONVERT_FLAGS
    {
        CONVF_FLOAT     = 0x1,
        CONVF_UNORM     = 0x2,
        CONVF_UINT      = 0x4,
        CONVF_SNORM     = 0x8, 
        CONVF_SINT      = 0x10,
        CONVF_DEPTH     = 0x20,
        CONVF_STENCIL   = 0x40,
        CONVF_SHAREDEXP = 0x80,
        CONVF_BGR       = 0x100,
        CONVF_XR        = 0x200,
        CONVF_PACKED    = 0x400,
        CONVF_BC        = 0x800,
        CONVF_YUV       = 0x1000,
        CONVF_POS_ONLY  = 0x2000,
        CONVF_R         = 0x10000,
        CONVF_G         = 0x20000,
        CONVF_B         = 0x40000,
        CONVF_A         = 0x80000,
        CONVF_RGB_MASK  = 0x70000,
        CONVF_RGBA_MASK = 0xF0000,
    };

    DWORD __cdecl _GetConvertFlags( _In_ DXGI_FORMAT format );

    void __cdecl _CopyScanline( _When_(pDestination == pSource, _Inout_updates_bytes_(outSize))
                                _When_(pDestination != pSource, _Out_writes_bytes_(outSize))
                                    void* pDestination, _In_ size_t outSize,
                                _In_reads_bytes_(inSize) const void* pSource, _In_ size_t inSize,
                                _In_ DXGI_FORMAT format, _In_ DWORD flags );

    void __cdecl _SwizzleScanline( _When_(pDestination == pSource, _In_)
                                   _When_(pDestination != pSource, _Out_writes_bytes_(outSize))
                                        void* pDestination, _In_ size_t outSize,
                                   _In_reads_bytes_(inSize) const void* pSource, _In_ size_t inSize,
                                   _In_ DXGI_FORMAT format, _In_ DWORD flags );
 
    _Success_(return != false)
    bool __cdecl _ExpandScanline( _Out_writes_bytes_(outSize) void* pDestination, _In_ size_t outSize,
                                  _In_ DXGI_FORMAT outFormat, 
                                  _In_reads_bytes_(inSize) const void* pSource, _In_ size_t inSize,
                                  _In_ DXGI_FORMAT inFormat, _In_ DWORD flags );

    _Success_(return != false)
    bool __cdecl _LoadScanline( _Out_writes_(count) XMVECTOR* pDestination, _In_ size_t count,
                                _In_reads_bytes_(size) const void* pSource, _In_ size_t size, _In_ DXGI_FORMAT format );

    _Success_(return != false)
    bool __cdecl _LoadScanlineLinear( _Out_writes_(count) XMVECTOR* pDestination, _In_ size_t count,
                                      _In_reads_bytes_(size) const void* pSource, _In_ size_t size, _In_ DXGI_FORMAT format, _In_ DWORD flags  );

    _Success_(return != false)
    bool __cdecl _StoreScanline( _Out_writes_bytes_(size) void* pDestination, _In_ size_t size, _In_ DXGI_FORMAT format,
                                 _In_reads_(count) const XMVECTOR* pSource, _In_ size_t count, _In_ float threshold = 0 );

    _Success_(return != false)
    bool __cdecl _StoreScanlineLinear( _Out_writes_bytes_(size) void* pDestination, _In_ size_t size, _In_ DXGI_FORMAT format,
                                       _Inout_updates_all_(count) XMVECTOR* pSource, _In_ size_t count, _In_ DWORD flags, _In_ float threshold = 0 );

    _Success_(return != false)
    bool __cdecl _StoreScanlineDither( _Out_writes_bytes_(size) void* pDestination, _In_ size_t size, _In_ DXGI_FORMAT format,
                                       _Inout_updates_all_(count) XMVECTOR* pSource, _In_ size_t count, _In_ float threshold, size_t y, size_t z,
                                       _Inout_updates_all_opt_(count+2) XMVECTOR* pDiffusionErrors );

    HRESULT __cdecl _ConvertToR32G32B32A32( _In_ const Image& srcImage, _Inout_ ScratchImage& image );

    HRESULT __cdecl _ConvertFromR32G32B32A32( _In_ const Image& srcImage, _In_ const Image& destImage );
    HRESULT __cdecl _ConvertFromR32G32B32A32( _In_ const Image& srcImage, _In_ DXGI_FORMAT format, _Inout_ ScratchImage& image );
    HRESULT __cdecl _ConvertFromR32G32B32A32( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                              _In_ DXGI_FORMAT format, _Out_ ScratchImage& result );

    void __cdecl _ConvertScanline( _Inout_updates_all_(count) XMVECTOR* pBuffer, _In_ size_t count,
                                   _In_ DXGI_FORMAT outFormat, _In_ DXGI_FORMAT inFormat, _In_ DWORD flags );

    //---------------------------------------------------------------------------------
    // DDS helper functions
    HRESULT __cdecl _EncodeDDSHeader( _In_ const TexMetadata& metadata, DWORD flags,
                                      _Out_writes_bytes_to_opt_(maxsize, required) void* pDestination, _In_ size_t maxsize, _Out_ size_t& required );

}; // namespace
