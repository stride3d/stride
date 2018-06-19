// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Xenko.TextureConverter.AtitcWrapper
{
    #region Enum
    internal enum Format
    {
        ATI_TC_FORMAT_Unknown,                    ///< An undefined texture format.
        ATI_TC_FORMAT_ARGB_8888,                  ///< An ARGB format with 8-bit fixed channels.
        ATI_TC_FORMAT_RGB_888,                    ///< A RGB format with 8-bit fixed channels.
        ATI_TC_FORMAT_RG_8,                       ///< A two component format with 8-bit fixed channels.
        ATI_TC_FORMAT_R_8,                        ///< A single component format with 8-bit fixed channels.
        ATI_TC_FORMAT_ARGB_2101010,               ///< An ARGB format with 10-bit fixed channels for color & a 2-bit fixed channel for alpha.
        ATI_TC_FORMAT_ARGB_16,                    ///< A ARGB format with 16-bit fixed channels.
        ATI_TC_FORMAT_RG_16,                      ///< A two component format with 16-bit fixed channels.
        ATI_TC_FORMAT_R_16,                       ///< A single component format with 16-bit fixed channels.
        ATI_TC_FORMAT_ARGB_16F,                   ///< An ARGB format with 16-bit floating-point channels.
        ATI_TC_FORMAT_RG_16F,                     ///< A two component format with 16-bit floating-point channels.
        ATI_TC_FORMAT_R_16F,                      ///< A single component with 16-bit floating-point channels.
        ATI_TC_FORMAT_ARGB_32F,                   ///< An ARGB format with 32-bit floating-point channels.
        ATI_TC_FORMAT_RG_32F,                     ///< A two component format with 32-bit floating-point channels.
        ATI_TC_FORMAT_R_32F,                      ///< A single component with 32-bit floating-point channels.
        ATI_TC_FORMAT_DXT1,                       ///< An opaque (or 1-bit alpha) DXTC compressed texture format. Four bits per pixel.
        ATI_TC_FORMAT_DXT3,                       ///< A DXTC compressed texture format with explicit alpha. Eight bits per pixel.
        ATI_TC_FORMAT_DXT5,                       ///< A DXTC compressed texture format with interpolated alpha. Eight bits per pixel.
        ATI_TC_FORMAT_DXT5_xGBR,                  ///< A DXT5 with the red component swizzled into the alpha channel. Eight bits per pixel.
        ATI_TC_FORMAT_DXT5_RxBG,                  ///< A swizzled DXT5 format with the green component swizzled into the alpha channel. Eight bits per pixel.
        ATI_TC_FORMAT_DXT5_RBxG,                  ///< A swizzled DXT5 format with the green component swizzled into the alpha channel & the blue component swizzled into the green channel. Eight bits per pixel.
        ATI_TC_FORMAT_DXT5_xRBG,                  ///< A swizzled DXT5 format with the green component swizzled into the alpha channel & the red component swizzled into the green channel. Eight bits per pixel.
        ATI_TC_FORMAT_DXT5_RGxB,                  ///< A swizzled DXT5 format with the blue component swizzled into the alpha channel. Eight bits per pixel.
        ATI_TC_FORMAT_DXT5_xGxR,                  ///< A two-component swizzled DXT5 format with the red component swizzled into the alpha channel & the green component in the green channel. Eight bits per pixel.
        ATI_TC_FORMAT_ATI1N,                      ///< A single component compression format using the same technique as DXT5 alpha. Four bits per pixel.
        ATI_TC_FORMAT_ATI2N,                      ///< A two component compression format using the same technique as DXT5 alpha. Designed for compression object space normal maps. Eight bits per pixel.
        ATI_TC_FORMAT_ATI2N_XY,                   ///< A two component compression format using the same technique as DXT5 alpha. The same as ATI2N but with the channels swizzled. Eight bits per pixel.
        ATI_TC_FORMAT_ATI2N_DXT5,                 ///< An ATI2N like format using DXT5. Intended for use on GPUs that do not natively support ATI2N. Eight bits per pixel.
        ATI_TC_FORMAT_BC1,                        ///< A four component opaque (or 1-bit alpha) compressed texture format for Microsoft DirectX10. Identical to DXT1.  Four bits per pixel.
        ATI_TC_FORMAT_BC2,                        ///< A four component compressed texture format with explicit alpha for Microsoft DirectX10. Identical to DXT3. Eight bits per pixel.
        ATI_TC_FORMAT_BC3,                        ///< A four component compressed texture format with interpolated alpha for Microsoft DirectX10. Identical to DXT5. Eight bits per pixel.
        ATI_TC_FORMAT_BC4,                        ///< A single component compressed texture format for Microsoft DirectX10. Identical to ATI1N. Four bits per pixel.
        ATI_TC_FORMAT_BC5,                        ///< A two component compressed texture format for Microsoft DirectX10. Identical to ATI2N. Eight bits per pixel.
        ATI_TC_FORMAT_ATC_RGB,                    ///< ATI_TC - a compressed RGB format for cellphones & hand-held devices.
        ATI_TC_FORMAT_ATC_RGBA_Explicit,          ///< ATI_TC - a compressed ARGB format with explicit alpha for cellphones & hand-held devices.
        ATI_TC_FORMAT_ATC_RGBA_Interpolated,      ///< ATI_TC - a compressed ARGB format with interpolated alpha for cellphones & hand-held devices.
        ATI_TC_FORMAT_ETC_RGB,                    ///< ETC (aka Ericsson Texture Compression) - a compressed RGB format for cellphones.
        ATI_TC_FORMAT_MAX = ATI_TC_FORMAT_ETC_RGB
    } ;

    internal enum Result
    {
        ATI_TC_OK = 0,                            ///< Ok.
        ATI_TC_ABORTED,                           ///< The conversion was aborted.
        ATI_TC_ERR_INVALID_SOURCE_TEXTURE,        ///< The source texture is invalid.
        ATI_TC_ERR_INVALID_DEST_TEXTURE,          ///< The destination texture is invalid.
        ATI_TC_ERR_UNSUPPORTED_SOURCE_FORMAT,     ///< The source format is not a supported format.
        ATI_TC_ERR_UNSUPPORTED_DEST_FORMAT,       ///< The destination format is not a supported format.
        ATI_TC_ERR_SIZE_MISMATCH,                 ///< The source and destination texture sizes do not match.
        ATI_TC_ERR_UNABLE_TO_INIT_CODEC,          ///< ATI_Compress was unable to initialize the codec needed for conversion.
        ATI_TC_ERR_GENERIC                        ///< An unknown error occurred.
    } ;

    internal enum Speed
    {
        ATI_TC_Speed_Normal,                      ///< Highest quality mode
        ATI_TC_Speed_Fast,                        ///< Slightly lower quality but much faster compression mode - DXTn & ATInN only
        ATI_TC_Speed_SuperFast,                   ///< Slightly lower quality but much, much faster compression mode - DXTn & ATInN only
    } ;
    #endregion


    /// <summary>
    /// C# Equivalent of ATC structure CompressOptions
    /// </summary>
    internal struct CompressOptions
    {
        public int dwSize;					      ///< The size of this structure.
        public bool bUseChannelWeighting;      ///< Use channel weightings. With swizzled formats the weighting applies to the data within the specified channel not the channel itself.
        public double fWeightingRed;			      ///< The weighting of the Red or X Channel.
        public double fWeightingGreen;		      ///< The weighting of the Green or Y Channel.
        public double fWeightingBlue;		      ///< The weighting of the Blue or Z Channel.
        public bool bUseAdaptiveWeighting;     ///< Adapt weighting on a per-block basis.
        public bool bDXT1UseAlpha;              ///< Encode single-bit alpha data. Only valid when compressing to DXT1 & BC1.
        public byte nAlphaThreshold;            ///< The alpha threshold to use when compressing to DXT1 & BC1 with bDXT1UseAlpha. Texels with an alpha value less than the threshold are treated as transparent.
        public bool bDisableMultiThreading;    ///< Disable multi-threading of the compression. This will slow the compression but can be useful if you're managing threads in your application.
        public Speed nCompressionSpeed;         ///< The trade-off between compression speed & quality.

        public unsafe CompressOptions(bool bUseChannelWeighting, double fWeightingRed, double fWeightingGreen, double fWeightingBlue, bool bUseAdaptiveWeighting, bool bDXT1UseAlpha, byte nAlphaThreshold, bool bDisableMultiThreading, Speed nCompressionSpeed)
        {
            this.dwSize = sizeof(CompressOptions);
            this.bUseChannelWeighting = bUseChannelWeighting;
            this.fWeightingRed = fWeightingRed;
            this.fWeightingGreen = fWeightingGreen;
            this.fWeightingBlue = fWeightingBlue;
            this.bUseAdaptiveWeighting = bUseAdaptiveWeighting;
            this.bDXT1UseAlpha = bDXT1UseAlpha;
            this.nAlphaThreshold = nAlphaThreshold;
            this.bDisableMultiThreading = bDisableMultiThreading;
            this.nCompressionSpeed = nCompressionSpeed;
        }
    }

    /// <summary>
    /// C# Equivalent of ATC structure Texture
    /// </summary>
    internal struct Texture
    {
        public int dwSize;                      ///< Size of this structure.
        public int dwWidth;                     ///< Width of the texture.
        public int dwHeight;                    ///< Height of the texture.
        public int dwPitch;                     ///< Distance to start of next line - necessary only for uncompressed textures.
        public Format format;                      ///< Format of the texture.
        public int dwDataSize;                  ///< Size of the allocated texture data.
        public IntPtr pData;                      ///< Pointer to the texture data

        public unsafe Texture(int dwWidth, int dwHeight, int dwPitch, Format format, int dwDataSize, IntPtr pData)
        {
            this.dwSize = sizeof(Texture);
            this.dwWidth = dwWidth;
            this.dwHeight = dwHeight;
            this.dwPitch = dwPitch;
            this.format = format;
            this.dwDataSize = dwDataSize;
            this.pData = pData;
        }

        public override String ToString()
        {
            return "width:" + dwWidth + "\nheight:" + dwHeight + "\nformat:" + format + "\nrowPitch:" + dwPitch + "\nSize:" + dwDataSize + "\npixels:" + pData;
        }
    }

    /// <summary>
    /// Binding of the ATC utility methods
    /// </summary>
    internal class Utilities
    {

        #region Bindings
        [DllImport("AtitcWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static int atitcCalculateBufferSize(out Texture pTexture);

        [DllImport("AtitcWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static void atitcDeleteData(out Texture pTexture);

        [DllImport("AtitcWrapper", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private extern static Result atitcConvertTexture(out Texture pSourceTexture, out Texture pDestTexture, out CompressOptions pOptions);
        #endregion

        public static int CalculateBufferSize(out Texture pTexture)
        {
            return atitcCalculateBufferSize(out pTexture);
        }

        public static Result ConvertTexture(out Texture pSourceTexture, out Texture pDestTexture, out CompressOptions pOptions)
        {
            return atitcConvertTexture(out pSourceTexture, out pDestTexture, out pOptions);
        }

        public static void DeleteData(out Texture pTexture)
        {
            atitcDeleteData(out pTexture);
        }
    }

}
