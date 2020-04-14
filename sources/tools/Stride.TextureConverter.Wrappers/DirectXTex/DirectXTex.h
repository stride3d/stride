//-------------------------------------------------------------------------------------
// DirectXTex.h
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

#include <stdint.h>

#include <algorithm>
#include <functional>
#include <vector>

#if !defined(__d3d11_h__) && !defined(__d3d11_x_h__) && !defined(__d3d12_h__) && !defined(__d3d12_x_h__)
#if defined(_XBOX_ONE) && defined(_TITLE)
#include <d3d11_x.h>
#define DCOMMON_H_INCLUDED
#else
#include <d3d11_1.h>
#endif
#endif

#include <directxmath.h>

#include <ocidl.h>

#define DIRECTX_TEX_VERSION 150

struct IWICImagingFactory;
struct IWICMetadataQueryReader;


namespace DirectX
{

    //---------------------------------------------------------------------------------
    // DXGI Format Utilities
    bool __cdecl IsValid( _In_ DXGI_FORMAT fmt );
    bool __cdecl IsCompressed( _In_ DXGI_FORMAT fmt );
    bool __cdecl IsPacked( _In_ DXGI_FORMAT fmt );
    bool __cdecl IsVideo( _In_ DXGI_FORMAT fmt );
    bool __cdecl IsPlanar( _In_ DXGI_FORMAT fmt );
    bool __cdecl IsPalettized( _In_ DXGI_FORMAT fmt );
    bool __cdecl IsDepthStencil(_In_ DXGI_FORMAT fmt );
    bool __cdecl IsSRGB( _In_ DXGI_FORMAT fmt );
    bool __cdecl IsTypeless( _In_ DXGI_FORMAT fmt, _In_ bool partialTypeless = true );

    bool __cdecl HasAlpha( _In_ DXGI_FORMAT fmt );

    size_t __cdecl BitsPerPixel( _In_ DXGI_FORMAT fmt );

    size_t __cdecl BitsPerColor( _In_ DXGI_FORMAT fmt );

    enum CP_FLAGS
    {
        CP_FLAGS_NONE               = 0x0,      // Normal operation
        CP_FLAGS_LEGACY_DWORD       = 0x1,      // Assume pitch is DWORD aligned instead of BYTE aligned
        CP_FLAGS_PARAGRAPH          = 0x2,      // Assume pitch is 16-byte aligned instead of BYTE aligned
        CP_FLAGS_YMM                = 0x4,      // Assume pitch is 32-byte aligned instead of BYTE aligned
        CP_FLAGS_ZMM                = 0x8,      // Assume pitch is 64-byte aligned instead of BYTE aligned
        CP_FLAGS_PAGE4K             = 0x200,    // Assume pitch is 4096-byte aligned instead of BYTE aligned
        CP_FLAGS_BAD_DXTN_TAILS     = 0x1000,   // BC formats with malformed mipchain blocks smaller than 4x4
        CP_FLAGS_24BPP              = 0x10000,  // Override with a legacy 24 bits-per-pixel format size
        CP_FLAGS_16BPP              = 0x20000,  // Override with a legacy 16 bits-per-pixel format size
        CP_FLAGS_8BPP               = 0x40000,  // Override with a legacy 8 bits-per-pixel format size
    };

    void __cdecl ComputePitch( _In_ DXGI_FORMAT fmt, _In_ size_t width, _In_ size_t height,
                               _Out_ size_t& rowPitch, _Out_ size_t& slicePitch, _In_ DWORD flags = CP_FLAGS_NONE );

    size_t __cdecl ComputeScanlines( _In_ DXGI_FORMAT fmt, _In_ size_t height );

    DXGI_FORMAT __cdecl MakeSRGB( _In_ DXGI_FORMAT fmt );
    DXGI_FORMAT __cdecl MakeTypeless( _In_ DXGI_FORMAT fmt );
    DXGI_FORMAT __cdecl MakeTypelessUNORM( _In_ DXGI_FORMAT fmt );
    DXGI_FORMAT __cdecl MakeTypelessFLOAT( _In_ DXGI_FORMAT fmt );

    //---------------------------------------------------------------------------------
    // Texture metadata
    enum TEX_DIMENSION
        // Subset here matches D3D10_RESOURCE_DIMENSION and D3D11_RESOURCE_DIMENSION
    {
        TEX_DIMENSION_TEXTURE1D    = 2,
        TEX_DIMENSION_TEXTURE2D    = 3,
        TEX_DIMENSION_TEXTURE3D    = 4,
    };

    enum TEX_MISC_FLAG
        // Subset here matches D3D10_RESOURCE_MISC_FLAG and D3D11_RESOURCE_MISC_FLAG
    {
        TEX_MISC_TEXTURECUBE = 0x4L,
    };

    enum TEX_MISC_FLAG2
    {
        TEX_MISC2_ALPHA_MODE_MASK = 0x7L,
    };

    enum TEX_ALPHA_MODE
        // Matches DDS_ALPHA_MODE, encoded in MISC_FLAGS2
    {
        TEX_ALPHA_MODE_UNKNOWN       = 0,
        TEX_ALPHA_MODE_STRAIGHT      = 1,
        TEX_ALPHA_MODE_PREMULTIPLIED = 2,
        TEX_ALPHA_MODE_OPAQUE        = 3,
        TEX_ALPHA_MODE_CUSTOM        = 4,
    };

    struct TexMetadata
    {
        size_t          width;
        size_t          height;     // Should be 1 for 1D textures
        size_t          depth;      // Should be 1 for 1D or 2D textures
        size_t          arraySize;  // For cubemap, this is a multiple of 6
        size_t          mipLevels;
        uint32_t        miscFlags;
        uint32_t        miscFlags2;
        DXGI_FORMAT     format;
        TEX_DIMENSION   dimension;

        size_t __cdecl ComputeIndex( _In_ size_t mip, _In_ size_t item, _In_ size_t slice ) const;
            // Returns size_t(-1) to indicate an out-of-range error

        bool __cdecl IsCubemap() const { return (miscFlags & TEX_MISC_TEXTURECUBE) != 0; }
            // Helper for miscFlags

        bool __cdecl IsPMAlpha() const { return ((miscFlags2 & TEX_MISC2_ALPHA_MODE_MASK) == TEX_ALPHA_MODE_PREMULTIPLIED) != 0; }
        void __cdecl SetAlphaMode( TEX_ALPHA_MODE mode ) { miscFlags2 = (miscFlags2 & ~TEX_MISC2_ALPHA_MODE_MASK) | static_cast<uint32_t>(mode); }
        TEX_ALPHA_MODE __cdecl GetAlphaMode() const { return static_cast<TEX_ALPHA_MODE>(miscFlags2 & TEX_MISC2_ALPHA_MODE_MASK); }
            // Helpers for miscFlags2

        bool __cdecl IsVolumemap() const { return (dimension == TEX_DIMENSION_TEXTURE3D); }
            // Helper for dimension
    };

    enum DDS_FLAGS
    {
        DDS_FLAGS_NONE                  = 0x0,

        DDS_FLAGS_LEGACY_DWORD          = 0x1,
            // Assume pitch is DWORD aligned instead of BYTE aligned (used by some legacy DDS files)

        DDS_FLAGS_NO_LEGACY_EXPANSION   = 0x2,
            // Do not implicitly convert legacy formats that result in larger pixel sizes (24 bpp, 3:3:2, A8L8, A4L4, P8, A8P8) 

        DDS_FLAGS_NO_R10B10G10A2_FIXUP  = 0x4,
            // Do not use work-around for long-standing D3DX DDS file format issue which reversed the 10:10:10:2 color order masks

        DDS_FLAGS_FORCE_RGB             = 0x8,
            // Convert DXGI 1.1 BGR formats to DXGI_FORMAT_R8G8B8A8_UNORM to avoid use of optional WDDM 1.1 formats

        DDS_FLAGS_NO_16BPP              = 0x10,
            // Conversions avoid use of 565, 5551, and 4444 formats and instead expand to 8888 to avoid use of optional WDDM 1.2 formats

        DDS_FLAGS_EXPAND_LUMINANCE      = 0x20,
            // When loading legacy luminance formats expand replicating the color channels rather than leaving them packed (L8, L16, A8L8)

        DDS_FLAGS_BAD_DXTN_TAILS        = 0x40,
            // Some older DXTn DDS files incorrectly handle mipchain tails for blocks smaller than 4x4

        DDS_FLAGS_FORCE_DX10_EXT        = 0x10000,
            // Always use the 'DX10' header extension for DDS writer (i.e. don't try to write DX9 compatible DDS files)

        DDS_FLAGS_FORCE_DX10_EXT_MISC2  = 0x20000,
            // DDS_FLAGS_FORCE_DX10_EXT including miscFlags2 information (result may not be compatible with D3DX10 or D3DX11)
    };

    enum WIC_FLAGS
    {
        WIC_FLAGS_NONE                  = 0x0,

        WIC_FLAGS_FORCE_RGB             = 0x1,
            // Loads DXGI 1.1 BGR formats as DXGI_FORMAT_R8G8B8A8_UNORM to avoid use of optional WDDM 1.1 formats

        WIC_FLAGS_NO_X2_BIAS            = 0x2,
            // Loads DXGI 1.1 X2 10:10:10:2 format as DXGI_FORMAT_R10G10B10A2_UNORM

        WIC_FLAGS_NO_16BPP              = 0x4,
            // Loads 565, 5551, and 4444 formats as 8888 to avoid use of optional WDDM 1.2 formats

        WIC_FLAGS_ALLOW_MONO            = 0x8,
            // Loads 1-bit monochrome (black & white) as R1_UNORM rather than 8-bit grayscale

        WIC_FLAGS_ALL_FRAMES            = 0x10,
            // Loads all images in a multi-frame file, converting/resizing to match the first frame as needed, defaults to 0th frame otherwise

        WIC_FLAGS_IGNORE_SRGB           = 0x20,
            // Ignores sRGB metadata if present in the file

        WIC_FLAGS_DITHER                = 0x10000,
            // Use ordered 4x4 dithering for any required conversions

        WIC_FLAGS_DITHER_DIFFUSION      = 0x20000,
            // Use error-diffusion dithering for any required conversions

        WIC_FLAGS_FILTER_POINT          = 0x100000,
        WIC_FLAGS_FILTER_LINEAR         = 0x200000,
        WIC_FLAGS_FILTER_CUBIC          = 0x300000,
        WIC_FLAGS_FILTER_FANT           = 0x400000, // Combination of Linear and Box filter
            // Filtering mode to use for any required image resizing (only needed when loading arrays of differently sized images; defaults to Fant)
    };

    HRESULT __cdecl GetMetadataFromDDSMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size, _In_ DWORD flags,
                                              _Out_ TexMetadata& metadata );
    HRESULT __cdecl GetMetadataFromDDSFile( _In_z_ const wchar_t* szFile, _In_ DWORD flags,
                                            _Out_ TexMetadata& metadata );

    HRESULT __cdecl GetMetadataFromHDRMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size,
                                              _Out_ TexMetadata& metadata );
    HRESULT __cdecl GetMetadataFromHDRFile( _In_z_ const wchar_t* szFile,
                                            _Out_ TexMetadata& metadata );

    HRESULT __cdecl GetMetadataFromTGAMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size,
                                              _Out_ TexMetadata& metadata );
    HRESULT __cdecl GetMetadataFromTGAFile( _In_z_ const wchar_t* szFile,
                                            _Out_ TexMetadata& metadata );

    HRESULT __cdecl GetMetadataFromWICMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size, _In_ DWORD flags,
                                              _Out_ TexMetadata& metadata,
                                              _In_opt_ std::function<void __cdecl(IWICMetadataQueryReader*)> getMQR = nullptr);

    HRESULT __cdecl GetMetadataFromWICFile( _In_z_ const wchar_t* szFile, _In_ DWORD flags,
                                            _Out_ TexMetadata& metadata,
                                            _In_opt_ std::function<void __cdecl(IWICMetadataQueryReader*)> getMQR = nullptr);

    //---------------------------------------------------------------------------------
    // Bitmap image container
    struct Image
    {
        size_t      width;
        size_t      height;
        DXGI_FORMAT format;
        size_t      rowPitch;
        size_t      slicePitch;
        uint8_t*    pixels;
    };

    class ScratchImage
    {
    public:
        ScratchImage()
            : m_nimages(0), m_size(0), m_metadata{}, m_image(nullptr), m_memory(nullptr) {}
        ScratchImage(ScratchImage&& moveFrom)
            : m_nimages(0), m_size(0), m_metadata{}, m_image(nullptr), m_memory(nullptr) { *this = std::move(moveFrom); }
        ~ScratchImage() { Release(); }

        ScratchImage& __cdecl operator= (ScratchImage&& moveFrom);

        ScratchImage(const ScratchImage&) = delete;
        ScratchImage& operator=(const ScratchImage&) = delete;

        HRESULT __cdecl Initialize( _In_ const TexMetadata& mdata, _In_ DWORD flags = CP_FLAGS_NONE );

        HRESULT __cdecl Initialize1D( _In_ DXGI_FORMAT fmt, _In_ size_t length, _In_ size_t arraySize, _In_ size_t mipLevels, _In_ DWORD flags = CP_FLAGS_NONE );
        HRESULT __cdecl Initialize2D( _In_ DXGI_FORMAT fmt, _In_ size_t width, _In_ size_t height, _In_ size_t arraySize, _In_ size_t mipLevels, _In_ DWORD flags = CP_FLAGS_NONE );
        HRESULT __cdecl Initialize3D( _In_ DXGI_FORMAT fmt, _In_ size_t width, _In_ size_t height, _In_ size_t depth, _In_ size_t mipLevels, _In_ DWORD flags = CP_FLAGS_NONE );
        HRESULT __cdecl InitializeCube( _In_ DXGI_FORMAT fmt, _In_ size_t width, _In_ size_t height, _In_ size_t nCubes, _In_ size_t mipLevels, _In_ DWORD flags = CP_FLAGS_NONE );

        HRESULT __cdecl InitializeFromImage( _In_ const Image& srcImage, _In_ bool allow1D = false, _In_ DWORD flags = CP_FLAGS_NONE );
        HRESULT __cdecl InitializeArrayFromImages( _In_reads_(nImages) const Image* images, _In_ size_t nImages, _In_ bool allow1D = false, _In_ DWORD flags = CP_FLAGS_NONE ); 
        HRESULT __cdecl InitializeCubeFromImages( _In_reads_(nImages) const Image* images, _In_ size_t nImages, _In_ DWORD flags = CP_FLAGS_NONE );
        HRESULT __cdecl Initialize3DFromImages( _In_reads_(depth) const Image* images, _In_ size_t depth, _In_ DWORD flags = CP_FLAGS_NONE );

        void __cdecl Release();

        bool __cdecl OverrideFormat( _In_ DXGI_FORMAT f );

        const TexMetadata& __cdecl GetMetadata() const { return m_metadata; }
        const Image* __cdecl GetImage(_In_ size_t mip, _In_ size_t item, _In_ size_t slice) const;

        const Image* __cdecl GetImages() const { return m_image; }
        size_t __cdecl GetImageCount() const { return m_nimages; }

        uint8_t* __cdecl GetPixels() const { return m_memory; }
        size_t __cdecl GetPixelsSize() const { return m_size; }

        bool __cdecl IsAlphaAllOpaque() const;

    private:
        size_t      m_nimages;
        size_t      m_size;
        TexMetadata m_metadata;
        Image*      m_image;
        uint8_t*    m_memory;
    };

    //---------------------------------------------------------------------------------
    // Memory blob (allocated buffer pointer is always 16-byte aligned)
    class Blob
    {
    public:
        Blob() : m_buffer(nullptr), m_size(0) {}
        Blob(Blob&& moveFrom) : m_buffer(nullptr), m_size(0) { *this = std::move(moveFrom); }
        ~Blob() { Release(); }

        Blob& __cdecl operator= (Blob&& moveFrom);

        Blob(const Blob&) = delete;
        Blob& operator=(const Blob&) = delete;

        HRESULT __cdecl Initialize( _In_ size_t size );

        void __cdecl Release();

        void *__cdecl GetBufferPointer() const { return m_buffer; }
        size_t __cdecl GetBufferSize() const { return m_size; }

        HRESULT __cdecl Trim(size_t size);

    private:
        void*   m_buffer;
        size_t  m_size;
    };

    //---------------------------------------------------------------------------------
    // Image I/O

    // DDS operations
    HRESULT __cdecl LoadFromDDSMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size, _In_ DWORD flags,
                                       _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image );
    HRESULT __cdecl LoadFromDDSFile( _In_z_ const wchar_t* szFile, _In_ DWORD flags,
                                     _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image );

    HRESULT __cdecl SaveToDDSMemory( _In_ const Image& image, _In_ DWORD flags,
                                     _Out_ Blob& blob );
    HRESULT __cdecl SaveToDDSMemory( _In_reads_(nimages) const Image* images, _In_ size_t nimages, _In_ const TexMetadata& metadata, _In_ DWORD flags,
                                     _Out_ Blob& blob );

    HRESULT __cdecl SaveToDDSFile( _In_ const Image& image, _In_ DWORD flags, _In_z_ const wchar_t* szFile );
    HRESULT __cdecl SaveToDDSFile( _In_reads_(nimages) const Image* images, _In_ size_t nimages, _In_ const TexMetadata& metadata, _In_ DWORD flags, _In_z_ const wchar_t* szFile );

    // HDR operations
    HRESULT __cdecl LoadFromHDRMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size,
                                       _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image );
    HRESULT __cdecl LoadFromHDRFile( _In_z_ const wchar_t* szFile,
                                     _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image );

    HRESULT __cdecl SaveToHDRMemory( _In_ const Image& image, _Out_ Blob& blob );
    HRESULT __cdecl SaveToHDRFile( _In_ const Image& image, _In_z_ const wchar_t* szFile );

    // TGA operations
    HRESULT __cdecl LoadFromTGAMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size,
                                       _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image );
    HRESULT __cdecl LoadFromTGAFile( _In_z_ const wchar_t* szFile,
                                     _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image );

    HRESULT __cdecl SaveToTGAMemory( _In_ const Image& image, _Out_ Blob& blob );
    HRESULT __cdecl SaveToTGAFile( _In_ const Image& image, _In_z_ const wchar_t* szFile );

    // WIC operations
    HRESULT __cdecl LoadFromWICMemory( _In_reads_bytes_(size) const void* pSource, _In_ size_t size, _In_ DWORD flags,
                                       _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image,
                                       _In_opt_ std::function<void __cdecl(IWICMetadataQueryReader*)> getMQR = nullptr);
    HRESULT __cdecl LoadFromWICFile( _In_z_ const wchar_t* szFile, _In_ DWORD flags,
                                    _Out_opt_ TexMetadata* metadata, _Out_ ScratchImage& image,
                                    _In_opt_ std::function<void __cdecl(IWICMetadataQueryReader*)> getMQR = nullptr);

    HRESULT __cdecl SaveToWICMemory( _In_ const Image& image, _In_ DWORD flags, _In_ REFGUID guidContainerFormat,
                                     _Out_ Blob& blob, _In_opt_ const GUID* targetFormat = nullptr,
                                     _In_opt_ std::function<void __cdecl(IPropertyBag2*)> setCustomProps = nullptr );
    HRESULT __cdecl SaveToWICMemory( _In_count_(nimages) const Image* images, _In_ size_t nimages, _In_ DWORD flags, _In_ REFGUID guidContainerFormat,
                                     _Out_ Blob& blob, _In_opt_ const GUID* targetFormat = nullptr,
                                     _In_opt_ std::function<void __cdecl(IPropertyBag2*)> setCustomProps = nullptr );

    HRESULT __cdecl SaveToWICFile( _In_ const Image& image, _In_ DWORD flags, _In_ REFGUID guidContainerFormat,
                                   _In_z_ const wchar_t* szFile, _In_opt_ const GUID* targetFormat = nullptr,
                                   _In_opt_ std::function<void __cdecl(IPropertyBag2*)> setCustomProps = nullptr );
    HRESULT __cdecl SaveToWICFile( _In_count_(nimages) const Image* images, _In_ size_t nimages, _In_ DWORD flags, _In_ REFGUID guidContainerFormat,
                                   _In_z_ const wchar_t* szFile, _In_opt_ const GUID* targetFormat = nullptr,
                                   _In_opt_ std::function<void __cdecl(IPropertyBag2*)> setCustomProps = nullptr );

    //---------------------------------------------------------------------------------
    // Texture conversion, resizing, mipmap generation, and block compression

    enum TEX_FR_FLAGS
    {
        TEX_FR_ROTATE0          = 0x0,
        TEX_FR_ROTATE90         = 0x1,
        TEX_FR_ROTATE180        = 0x2,
        TEX_FR_ROTATE270        = 0x3,
        TEX_FR_FLIP_HORIZONTAL  = 0x08,
        TEX_FR_FLIP_VERTICAL    = 0x10,
    };

    HRESULT __cdecl FlipRotate( _In_ const Image& srcImage, _In_ DWORD flags, _Out_ ScratchImage& image );
    HRESULT __cdecl FlipRotate( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                _In_ DWORD flags, _Out_ ScratchImage& result );
        // Flip and/or rotate image

    enum TEX_FILTER_FLAGS
    {
        TEX_FILTER_DEFAULT          = 0,

        TEX_FILTER_WRAP_U           = 0x1,
        TEX_FILTER_WRAP_V           = 0x2,
        TEX_FILTER_WRAP_W           = 0x4,
        TEX_FILTER_WRAP             = ( TEX_FILTER_WRAP_U | TEX_FILTER_WRAP_V | TEX_FILTER_WRAP_W ),
        TEX_FILTER_MIRROR_U         = 0x10,
        TEX_FILTER_MIRROR_V         = 0x20,
        TEX_FILTER_MIRROR_W         = 0x40,
        TEX_FILTER_MIRROR          = ( TEX_FILTER_MIRROR_U | TEX_FILTER_MIRROR_V | TEX_FILTER_MIRROR_W ),
            // Wrap vs. Mirror vs. Clamp filtering options

        TEX_FILTER_SEPARATE_ALPHA   = 0x100,
            // Resize color and alpha channel independently

        TEX_FILTER_FLOAT_X2BIAS     = 0x200,
            // Enable *2 - 1 conversion cases for unorm<->float and positive-only float formats

        TEX_FILTER_RGB_COPY_RED     = 0x1000,
        TEX_FILTER_RGB_COPY_GREEN   = 0x2000,
        TEX_FILTER_RGB_COPY_BLUE    = 0x4000,
            // When converting RGB to R, defaults to using grayscale. These flags indicate copying a specific channel instead
            // When converting RGB to RG, defaults to copying RED | GREEN. These flags control which channels are selected instead.

        TEX_FILTER_DITHER           = 0x10000,
            // Use ordered 4x4 dithering for any required conversions
        TEX_FILTER_DITHER_DIFFUSION = 0x20000,
            // Use error-diffusion dithering for any required conversions

        TEX_FILTER_POINT            = 0x100000,
        TEX_FILTER_LINEAR           = 0x200000,
        TEX_FILTER_CUBIC            = 0x300000,
        TEX_FILTER_BOX              = 0x400000,
        TEX_FILTER_FANT             = 0x400000, // Equiv to Box filtering for mipmap generation
        TEX_FILTER_TRIANGLE         = 0x500000,
            // Filtering mode to use for any required image resizing

        TEX_FILTER_SRGB_IN          = 0x1000000,
        TEX_FILTER_SRGB_OUT         = 0x2000000,
        TEX_FILTER_SRGB             = ( TEX_FILTER_SRGB_IN | TEX_FILTER_SRGB_OUT ),
            // sRGB <-> RGB for use in conversion operations
            // if the input format type is IsSRGB(), then SRGB_IN is on by default
            // if the output format type is IsSRGB(), then SRGB_OUT is on by default

        TEX_FILTER_FORCE_NON_WIC    = 0x10000000,
            // Forces use of the non-WIC path when both are an option

        TEX_FILTER_FORCE_WIC        = 0x20000000,
            // Forces use of the WIC path even when logic would have picked a non-WIC path when both are an option
    };

    HRESULT __cdecl Resize( _In_ const Image& srcImage, _In_ size_t width, _In_ size_t height, _In_ DWORD filter,
                            _Out_ ScratchImage& image );
    HRESULT __cdecl Resize( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                            _In_ size_t width, _In_ size_t height, _In_ DWORD filter, _Out_ ScratchImage& result );
        // Resize the image to width x height. Defaults to Fant filtering.
        // Note for a complex resize, the result will always have mipLevels == 1

    const float TEX_THRESHOLD_DEFAULT = 0.5f;
        // Default value for alpha threshold used when converting to 1-bit alpha

    HRESULT __cdecl Convert( _In_ const Image& srcImage, _In_ DXGI_FORMAT format, _In_ DWORD filter, _In_ float threshold,
                            _Out_ ScratchImage& image );
    HRESULT __cdecl Convert( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                             _In_ DXGI_FORMAT format, _In_ DWORD filter, _In_ float threshold, _Out_ ScratchImage& result );
        // Convert the image to a new format
    
    HRESULT __cdecl ConvertToSinglePlane( _In_ const Image& srcImage, _Out_ ScratchImage& image );
    HRESULT __cdecl ConvertToSinglePlane( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                          _Out_ ScratchImage& image );
        // Converts the image from a planar format to an equivalent non-planar format

    HRESULT __cdecl GenerateMipMaps( _In_ const Image& baseImage, _In_ DWORD filter, _In_ size_t levels,
                                     _Inout_ ScratchImage& mipChain, _In_ bool allow1D = false );
    HRESULT __cdecl GenerateMipMaps( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                     _In_ DWORD filter, _In_ size_t levels, _Inout_ ScratchImage& mipChain );
        // levels of '0' indicates a full mipchain, otherwise is generates that number of total levels (including the source base image)
        // Defaults to Fant filtering which is equivalent to a box filter

    HRESULT __cdecl GenerateMipMaps3D( _In_reads_(depth) const Image* baseImages, _In_ size_t depth, _In_ DWORD filter, _In_ size_t levels,
                                       _Out_ ScratchImage& mipChain );
    HRESULT __cdecl GenerateMipMaps3D( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                       _In_ DWORD filter, _In_ size_t levels, _Out_ ScratchImage& mipChain );
        // levels of '0' indicates a full mipchain, otherwise is generates that number of total levels (including the source base image)
        // Defaults to Fant filtering which is equivalent to a box filter

    enum TEX_PMALPHA_FLAGS
    {
        TEX_PMALPHA_DEFAULT         = 0,

        TEX_PMALPHA_IGNORE_SRGB     = 0x1,
            // ignores sRGB colorspace conversions

        TEX_PMALPHA_REVERSE         = 0x2,
            // converts from premultiplied alpha back to straight alpha

        TEX_PMALPHA_SRGB_IN         = 0x1000000,
        TEX_PMALPHA_SRGB_OUT        = 0x2000000,
        TEX_PMALPHA_SRGB            = ( TEX_PMALPHA_SRGB_IN | TEX_PMALPHA_SRGB_OUT ),
            // if the input format type is IsSRGB(), then SRGB_IN is on by default
            // if the output format type is IsSRGB(), then SRGB_OUT is on by default
    };

    HRESULT __cdecl PremultiplyAlpha( _In_ const Image& srcImage, _In_ DWORD flags, _Out_ ScratchImage& image );
    HRESULT __cdecl PremultiplyAlpha( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                      _In_ DWORD flags, _Out_ ScratchImage& result );
        // Converts to/from a premultiplied alpha version of the texture

    enum TEX_COMPRESS_FLAGS
    {
        TEX_COMPRESS_DEFAULT            = 0,

        TEX_COMPRESS_RGB_DITHER         = 0x10000,
            // Enables dithering RGB colors for BC1-3 compression

        TEX_COMPRESS_A_DITHER           = 0x20000,
            // Enables dithering alpha for BC1-3 compression

        TEX_COMPRESS_DITHER             = 0x30000,
            // Enables both RGB and alpha dithering for BC1-3 compression

        TEX_COMPRESS_UNIFORM            = 0x40000,
            // Uniform color weighting for BC1-3 compression; by default uses perceptual weighting

        TEX_COMPRESS_BC7_USE_3SUBSETS   = 0x80000,
            // Enables exhaustive search for BC7 compress for mode 0 and 2; by default skips trying these modes

        TEX_COMPRESS_BC7_QUICK          = 0x100000,
            // Minimal modes (usually mode 6) for BC7 compression

        TEX_COMPRESS_SRGB_IN            = 0x1000000,
        TEX_COMPRESS_SRGB_OUT           = 0x2000000,
        TEX_COMPRESS_SRGB               = ( TEX_COMPRESS_SRGB_IN | TEX_COMPRESS_SRGB_OUT ),
            // if the input format type is IsSRGB(), then SRGB_IN is on by default
            // if the output format type is IsSRGB(), then SRGB_OUT is on by default

        TEX_COMPRESS_PARALLEL           = 0x10000000,
            // Compress is free to use multithreading to improve performance (by default it does not use multithreading)
    };

    HRESULT __cdecl Compress( _In_ const Image& srcImage, _In_ DXGI_FORMAT format, _In_ DWORD compress, _In_ float threshold,
                              _Out_ ScratchImage& cImage );
    HRESULT __cdecl Compress( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                              _In_ DXGI_FORMAT format, _In_ DWORD compress, _In_ float threshold, _Out_ ScratchImage& cImages );
        // Note that threshold is only used by BC1. TEX_THRESHOLD_DEFAULT is a typical value to use

#if defined(__d3d11_h__) || defined(__d3d11_x_h__)
    HRESULT __cdecl Compress( _In_ ID3D11Device* pDevice, _In_ const Image& srcImage, _In_ DXGI_FORMAT format, _In_ DWORD compress,
                              _In_ float alphaWeight, _Out_ ScratchImage& image );
    HRESULT __cdecl Compress( _In_ ID3D11Device* pDevice, _In_ const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                              _In_ DXGI_FORMAT format, _In_ DWORD compress, _In_ float alphaWeight, _Out_ ScratchImage& cImages );
        // DirectCompute-based compression (alphaWeight is only used by BC7. 1.0 is the typical value to use)
#endif

    HRESULT __cdecl Decompress( _In_ const Image& cImage, _In_ DXGI_FORMAT format, _Out_ ScratchImage& image );
    HRESULT __cdecl Decompress( _In_reads_(nimages) const Image* cImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                _In_ DXGI_FORMAT format, _Out_ ScratchImage& images );

    //---------------------------------------------------------------------------------
    // Normal map operations

    enum CNMAP_FLAGS
    {
        CNMAP_DEFAULT           = 0,

        CNMAP_CHANNEL_RED       = 0x1,
        CNMAP_CHANNEL_GREEN     = 0x2,
        CNMAP_CHANNEL_BLUE      = 0x3,
        CNMAP_CHANNEL_ALPHA     = 0x4,
        CNMAP_CHANNEL_LUMINANCE = 0x5,
            // Channel selection when evaluting color value for height
            // Luminance is a combination of red, green, and blue

        CNMAP_MIRROR_U          = 0x1000,
        CNMAP_MIRROR_V          = 0x2000,
        CNMAP_MIRROR            = 0x3000,
            // Use mirror semantics for scanline references (defaults to wrap)

        CNMAP_INVERT_SIGN       = 0x4000,
            // Inverts normal sign

        CNMAP_COMPUTE_OCCLUSION = 0x8000,
            // Computes a crude occlusion term stored in the alpha channel
    };

    HRESULT __cdecl ComputeNormalMap( _In_ const Image& srcImage, _In_ DWORD flags, _In_ float amplitude,
                                      _In_ DXGI_FORMAT format, _Out_ ScratchImage& normalMap );
    HRESULT __cdecl ComputeNormalMap( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                      _In_ DWORD flags, _In_ float amplitude, _In_ DXGI_FORMAT format, _Out_ ScratchImage& normalMaps );

    //---------------------------------------------------------------------------------
    // Misc image operations

    struct Rect
    {
        size_t x;
        size_t y;
        size_t w;
        size_t h;

        Rect() = default;
        Rect( size_t _x, size_t _y, size_t _w, size_t _h ) : x(_x), y(_y), w(_w), h(_h) {}
    };

    HRESULT __cdecl CopyRectangle( _In_ const Image& srcImage, _In_ const Rect& srcRect, _In_ const Image& dstImage,
                                   _In_ DWORD filter, _In_ size_t xOffset, _In_ size_t yOffset );

    enum CMSE_FLAGS
    {
        CMSE_DEFAULT                = 0,

        CMSE_IMAGE1_SRGB            = 0x1,
        CMSE_IMAGE2_SRGB            = 0x2,
            // Indicates that image needs gamma correction before comparision

        CMSE_IGNORE_RED             = 0x10,
        CMSE_IGNORE_GREEN           = 0x20,
        CMSE_IGNORE_BLUE            = 0x40,
        CMSE_IGNORE_ALPHA           = 0x80,
            // Ignore the channel when computing MSE

        CMSE_IMAGE1_X2_BIAS         = 0x100,
        CMSE_IMAGE2_X2_BIAS         = 0x200,
            // Indicates that image should be scaled and biased before comparison (i.e. UNORM -> SNORM)
    };

    HRESULT __cdecl ComputeMSE( _In_ const Image& image1, _In_ const Image& image2, _Out_ float& mse, _Out_writes_opt_(4) float* mseV, _In_ DWORD flags = 0 );

    HRESULT __cdecl EvaluateImage( _In_ const Image& image,
                                   _In_ std::function<void __cdecl(_In_reads_(width) const XMVECTOR* pixels, size_t width, size_t y)> pixelFunc );
    HRESULT __cdecl EvaluateImage( _In_reads_(nimages) const Image* images, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                   _In_ std::function<void __cdecl(_In_reads_(width) const XMVECTOR* pixels, size_t width, size_t y)> pixelFunc );

    HRESULT __cdecl TransformImage( _In_ const Image& image,
                                    _In_ std::function<void __cdecl(_Out_writes_(width) XMVECTOR* outPixels,
                                        _In_reads_(width) const XMVECTOR* inPixels, size_t width, size_t y)> pixelFunc,
                                    ScratchImage& result );
    HRESULT __cdecl TransformImage( _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                        _In_ std::function<void __cdecl(_Out_writes_(width) XMVECTOR* outPixels,
                                    _In_reads_(width) const XMVECTOR* inPixels, size_t width, size_t y)> pixelFunc,
                                    ScratchImage& result );

    //---------------------------------------------------------------------------------
    // WIC utility code

    enum WICCodecs
    {
        WIC_CODEC_BMP = 1,     // Windows Bitmap (.bmp)
        WIC_CODEC_JPEG,             // Joint Photographic Experts Group (.jpg, .jpeg)
        WIC_CODEC_PNG,              // Portable Network Graphics (.png)
        WIC_CODEC_TIFF,             // Tagged Image File Format  (.tif, .tiff)
        WIC_CODEC_GIF,              // Graphics Interchange Format  (.gif)
        WIC_CODEC_WMP,              // Windows Media Photo / HD Photo / JPEG XR (.hdp, .jxr, .wdp)
        WIC_CODEC_ICO,              // Windows Icon (.ico)
    };

    REFGUID __cdecl GetWICCodec(_In_ WICCodecs codec);

    IWICImagingFactory* __cdecl GetWICFactory( bool& iswic2 );
    void __cdecl SetWICFactory( _In_opt_ IWICImagingFactory* pWIC);

    //---------------------------------------------------------------------------------
    // Direct3D 11 functions
#if defined(__d3d11_h__) || defined(__d3d11_x_h__)
    bool __cdecl IsSupportedTexture( _In_ ID3D11Device* pDevice, _In_ const TexMetadata& metadata );

    HRESULT __cdecl CreateTexture( _In_ ID3D11Device* pDevice, _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                   _Outptr_ ID3D11Resource** ppResource );

    HRESULT __cdecl CreateShaderResourceView( _In_ ID3D11Device* pDevice, _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                              _Outptr_ ID3D11ShaderResourceView** ppSRV );

    HRESULT __cdecl CreateTextureEx( _In_ ID3D11Device* pDevice, _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                     _In_ D3D11_USAGE usage, _In_ unsigned int bindFlags, _In_ unsigned int cpuAccessFlags, _In_ unsigned int miscFlags, _In_ bool forceSRGB,
                                     _Outptr_ ID3D11Resource** ppResource );

    HRESULT __cdecl CreateShaderResourceViewEx( _In_ ID3D11Device* pDevice, _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                                _In_ D3D11_USAGE usage, _In_ unsigned int bindFlags, _In_ unsigned int cpuAccessFlags, _In_ unsigned int miscFlags, _In_ bool forceSRGB,
                                                _Outptr_ ID3D11ShaderResourceView** ppSRV );

    HRESULT __cdecl CaptureTexture( _In_ ID3D11Device* pDevice, _In_ ID3D11DeviceContext* pContext, _In_ ID3D11Resource* pSource, _Out_ ScratchImage& result );
#endif

    //---------------------------------------------------------------------------------
    // Direct3D 12 functions
#if defined(__d3d12_h__) || defined(__d3d12_x_h__)
    bool __cdecl IsSupportedTexture( _In_ ID3D12Device* pDevice, _In_ const TexMetadata& metadata );

    HRESULT __cdecl CreateTexture( _In_ ID3D12Device* pDevice, _In_ const TexMetadata& metadata,
                                   _Outptr_ ID3D12Resource** ppResource );

    HRESULT __cdecl CreateTextureEx( _In_ ID3D12Device* pDevice, _In_ const TexMetadata& metadata,
                                     _In_ D3D12_RESOURCE_FLAGS resFlags, _In_ bool forceSRGB,
                                     _Outptr_ ID3D12Resource** ppResource );

    HRESULT __cdecl PrepareUpload( _In_ ID3D12Device* pDevice,
                                   _In_reads_(nimages) const Image* srcImages, _In_ size_t nimages, _In_ const TexMetadata& metadata,
                                   std::vector<D3D12_SUBRESOURCE_DATA>& subresources );

    HRESULT __cdecl CaptureTexture( _In_ ID3D12CommandQueue* pCommandQueue, _In_ ID3D12Resource* pSource, _In_ bool isCubeMap,
                                    _Out_ ScratchImage& result,
                                    _In_ D3D12_RESOURCE_STATES beforeState = D3D12_RESOURCE_STATE_RENDER_TARGET,
                                    _In_ D3D12_RESOURCE_STATES afterState = D3D12_RESOURCE_STATE_RENDER_TARGET );
#endif

#include "DirectXTex.inl"

}; // namespace
