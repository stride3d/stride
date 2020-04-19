//-------------------------------------------------------------------------------------
// DirectXTex.inl
//  
// DirectX Texture Library
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

//=====================================================================================
// DXGI Format Utilities
//=====================================================================================

_Use_decl_annotations_
inline bool __cdecl IsValid( DXGI_FORMAT fmt )
{
    return ( static_cast<size_t>(fmt) >= 1 && static_cast<size_t>(fmt) <= 190 );
}

_Use_decl_annotations_
inline bool __cdecl IsCompressed(DXGI_FORMAT fmt)
{
    switch ( fmt )
    {
    case DXGI_FORMAT_BC1_TYPELESS:
    case DXGI_FORMAT_BC1_UNORM:
    case DXGI_FORMAT_BC1_UNORM_SRGB:
    case DXGI_FORMAT_BC2_TYPELESS:
    case DXGI_FORMAT_BC2_UNORM:
    case DXGI_FORMAT_BC2_UNORM_SRGB:
    case DXGI_FORMAT_BC3_TYPELESS:
    case DXGI_FORMAT_BC3_UNORM:
    case DXGI_FORMAT_BC3_UNORM_SRGB:
    case DXGI_FORMAT_BC4_TYPELESS:
    case DXGI_FORMAT_BC4_UNORM:
    case DXGI_FORMAT_BC4_SNORM:
    case DXGI_FORMAT_BC5_TYPELESS:
    case DXGI_FORMAT_BC5_UNORM:
    case DXGI_FORMAT_BC5_SNORM:
    case DXGI_FORMAT_BC6H_TYPELESS:
    case DXGI_FORMAT_BC6H_UF16:
    case DXGI_FORMAT_BC6H_SF16:
    case DXGI_FORMAT_BC7_TYPELESS:
    case DXGI_FORMAT_BC7_UNORM:
    case DXGI_FORMAT_BC7_UNORM_SRGB:
        return true;

    default:
        return false;
    }
}

_Use_decl_annotations_
inline bool __cdecl IsPalettized(DXGI_FORMAT fmt)
{
    switch( fmt )
    {
    case DXGI_FORMAT_AI44:
    case DXGI_FORMAT_IA44:
    case DXGI_FORMAT_P8:
    case DXGI_FORMAT_A8P8:
        return true;

    default:
        return false;
    }
}

_Use_decl_annotations_
inline bool __cdecl IsSRGB(DXGI_FORMAT fmt)
{
    switch( fmt )
    {
    case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
    case DXGI_FORMAT_BC1_UNORM_SRGB:
    case DXGI_FORMAT_BC2_UNORM_SRGB:
    case DXGI_FORMAT_BC3_UNORM_SRGB:
    case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
    case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
    case DXGI_FORMAT_BC7_UNORM_SRGB:
        return true;

    default:
        return false;
    }
}


//=====================================================================================
// Image I/O
//=====================================================================================
_Use_decl_annotations_
inline HRESULT __cdecl SaveToDDSMemory(const Image& image, DWORD flags, Blob& blob)
{
    TexMetadata mdata = {};
    mdata.width = image.width;
    mdata.height = image.height;
    mdata.depth = 1;
    mdata.arraySize = 1;
    mdata.mipLevels = 1;
    mdata.format = image.format;
    mdata.dimension = TEX_DIMENSION_TEXTURE2D;

    return SaveToDDSMemory( &image, 1, mdata, flags, blob );
}

_Use_decl_annotations_
inline HRESULT __cdecl SaveToDDSFile(const Image& image, DWORD flags, const wchar_t* szFile)
{
    TexMetadata mdata = {};
    mdata.width = image.width;
    mdata.height = image.height;
    mdata.depth = 1;
    mdata.arraySize = 1;
    mdata.mipLevels = 1;
    mdata.format = image.format;
    mdata.dimension = TEX_DIMENSION_TEXTURE2D;

    return SaveToDDSFile( &image, 1, mdata, flags, szFile );
}
