// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// The colorspace used for textures, materials, lighting...
    /// </summary>
    [DataContract("ColorSpace")]
    public enum ColorSpace
    {
        /// <summary>
        /// Use a linear colorspace.
        /// </summary>
        Linear,

        /// <summary>
        /// Use a gamma colorspace.
        /// </summary>
        Gamma,
    }

    /// <summary>
    /// Specifies color space types.
    /// </summary>
    /// <remarks>
    /// This enum is used within DXGI in the CheckColorSpaceSupport, SetColorSpace1 and
    /// CheckOverlayColorSpaceSupport methods. It is also referenced in D3D11 video methods
    /// such as ID3D11VideoContext1::VideoProcessorSetOutputColorSpace1, and D2D methods
    /// such as ID2D1DeviceContext2::CreateImageSourceFromDxgi. The following color parameters
    /// are defined:
    /// </remarks>
    public enum PresenterColorSpace
    {
        /// <summary>
        /// ColorspaceRGB Range0-255 Gamma2.2 SitingImage PrimariesBT.709. Use with backbuffer of 8 bit colors, such as PixelFormat.B8G8R8A8_UNorm.
        /// This is the standard definition for sRGB. Note that this is often implemented
        /// with a linear segment, but in that case, the exponent is corrected to stay aligned
        /// with a gamma 2.2 curve. This is usually used with 8-bit and 10-bit color channels.
        /// </summary>
        RgbFullG22NoneP709 = 0,

        /// <summary>
        /// ColorspaceRGB Range0-255 Gamma1.0 SitingImage PrimariesBT.709. Use with backbuffer of 16 bit colors, PixelFormat.R16G16B16A16_Float.
        /// This is the standard definition for scRGB, and is usually used with 16-bit integer,
        /// 16-bit floating point, and 32-bit floating point channels.
        /// </summary>
        RgbFullG10NoneP709 = 1,

        /// <summary>
        /// ColorspaceRGB Range16-235 Gamma2.2 SitingImage PrimariesBT.709.
        /// This is the standard definition for ITU-R Recommendation BT.709. Note that
        /// due to the inclusion of a linear segment, the transfer curve looks similar to
        /// a pure exponential gamma of 1.9. This is usually used with 8-bit and 10-bit color
        /// channels.
        /// </summary>
        RgbStudioG22NoneP709 = 2,

        /// <summary>
        /// ColorspaceRGB Range16-235 Gamma2.2 SitingImage PrimariesBT.2020.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        RgbStudioG22NoneP2020 = 3,

        /// <summary>
        /// Reserved.
        /// </summary>
        Reserved = 4,

        /// <summary>
        /// ColorspaceYCbCr Range0-255 Gamma2.2 SitingImage PrimariesBT.709 TransferBT.601.
        /// This definition is commonly used for JPG, and is usually used with 8, 10, 12,
        /// or 16-bit color channels.
        /// </summary>
        YcbcrFullG22NoneP709X601 = 5,

        /// <summary>
        /// ColorspaceYCbCr Range16-235 Gamma2.2 SitingVideo PrimariesBT.601.
        /// This definition is commonly used for MPEG2, and is usually used with 8, 10, 12,
        /// or 16-bit color channels.
        /// </summary>
        YcbcrStudioG22LeftP601 = 6,

        /// <summary>
        /// ColorspaceYCbCr Range0-255 Gamma2.2 SitingVideo PrimariesBT.601.
        /// This is sometimes used for H.264 camera capture, and is usually used with 8, 10,
        /// 12, or 16-bit color channels.
        /// </summary>
        YcbcrFullG22LeftP601 = 7,

        /// <summary>
        /// ColorspaceYCbCr Range16-235 Gamma2.2 SitingVideo PrimariesBT.709.
        /// This definition is commonly used for H.264 and HEVC, and is usually used with
        /// 8, 10, 12, or 16-bit color channels.
        /// </summary>
        YcbcrStudioG22LeftP709 = 8,

        /// <summary>
        /// ColorspaceYCbCr Range0-255 Gamma2.2 SitingVideo PrimariesBT.709.
        /// This is sometimes used for H.264 camera capture, and is usually used with 8, 10,
        /// 12, or 16-bit color channels.
        /// </summary>
        YcbcrFullG22LeftP709 = 9,

        /// <summary>
        /// ColorspaceYCbCr Range16-235 Gamma2.2 SitingVideo PrimariesBT.2020.
        /// This definition may be used by HEVC, and is usually used with 10, 12, or 16-bit
        /// color channels.
        /// </summary>
        YcbcrStudioG22LeftP2020 = 10,

        /// <summary>
        /// ColorspaceYCbCr Range0-255 Gamma2.2 SitingVideo PrimariesBT.2020.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        YcbcrFullG22LeftP2020 = 11,

        /// <summary>
        /// ColorspaceRGB Range0-255 Gamma2084 SitingImage PrimariesBT.2020. Use with backbuffer of 10 bit colors, PixelFormat.R10G10B10A2_UNorm.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        RgbFullG2084NoneP2020 = 12,

        /// <summary>
        /// ColorspaceYCbCr Range16-235 Gamma2084 SitingVideo PrimariesBT.2020.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        YcbcrStudioG2084LeftP2020 = 13,

        /// <summary>
        /// ColorspaceRGB Range16-235 Gamma2084 SitingImage PrimariesBT.2020.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        RgbStudioG2084NoneP2020 = 14,

        /// <summary>
        /// ColorspaceYCbCr Range16-235 Gamma2.2 SitingVideo PrimariesBT.2020.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        YcbcrStudioG22TopleftP2020 = 15,

        /// <summary>
        /// ColorspaceYCbCr Range16-235 Gamma2084 SitingVideo PrimariesBT.2020.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        YcbcrStudioG2084TopleftP2020 = 16,

        /// <summary>
        /// ColorspaceRGB Range0-255 Gamma2.2 SitingImage PrimariesBT.2020.
        /// This is usually used with 10, 12, or 16-bit color channels.
        /// </summary>
        RgbFullG22NoneP2020 = 17,

        /// <summary>
        /// A custom color definition is used.
        /// </summary>
        YcbcrStudioGhlgTopleftP2020 = 18,

        /// <summary>
        /// No documentation.
        /// </summary>
        YcbcrFullGhlgTopleftP2020 = 19,

        /// <summary>
        /// No documentation.
        /// </summary>
        RgbStudioG24NoneP709 = 20,

        /// <summary>
        /// No documentation.
        /// </summary>
        RgbStudioG24NoneP2020 = 21,

        /// <summary>
        /// No documentation.
        /// </summary>
        YcbcrStudioG24LeftP709 = 22,

        /// <summary>
        /// No documentation.
        /// </summary>
        YcbcrStudioG24LeftP2020 = 23,

        /// <summary>
        /// No documentation.
        /// </summary>
        YcbcrStudioG24TopleftP2020 = 24,

        /// <summary>
        /// A custom color definition is used.
        /// </summary>
        Custom = -1
    }

}
