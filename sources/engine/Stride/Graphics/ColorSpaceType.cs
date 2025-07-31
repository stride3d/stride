// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Specifies the type of color space a Graphics Presenter has to output.
/// </summary>
/// <remarks>
///   <para>
///     This enum is used within a Graphics Presenter when configuring a Swap Chain to check
///     the color space support of the Graphics Adapter and the Graphics Output, set a
///     specific color space as output of the Swap Chain. It is also referenced in D3D11 video methods
///   </para>
///   <para>
///     It is also used for defining the output color space of video decoding.
///   </para>
///   <para>
///     The constants in this enum are divided into parts that describe the following:
///
///     <h3>Color Space</h3>
///     <para>Defines the color space of the color channel data.</para>
///     <list type="table">
///       <item>
///         <term>Rgb</term>
///         <description>The Red / Green / Blue color space color channels.</description>
///       </item>
///       <item>
///         <term>YCbCr</term>
///         <description>
///           Three channel color model which splits luma (brightness) from chroma (color).
///           YUV technically refers to analog signals and YCbCr to digital, but they are used interchangeably.
///         </description>
///       </item>
///     </list>
///
///     <h3>Range</h3>
///     <para>
///       Indicates which integer range corresponds to the floating point [0..1] range of the data.
///       For video, integer YCbCr data with ranges of [16..235] or [8..247] are usually mapped to normalized YCbCr with ranges of [0..1] or [-0.5..0.5].
///     </para>
///     <list type="table">
///       <item>
///         <term>Full</term>
///         <description>
///           For PC desktop content and images.
///           Defines the ranges: For 8-bit: <c>0-255</c>, for 10-bit: <c>0-1023</c>, for 12-bit: <c>0-4095</c>.
///         </description>
///       </item>
///       <item>
///         <term>Studio</term>
///         <description>
///           Often used in video. Enables the calibration of white and black between displays.
///           Defines the ranges: For 8-bit: <c>16-235</c>, for 10-bit: <c>64-940</c>, for 12-bit: <c>256 - 3760</c>.
///         </description>
///       </item>
///     </list>
///
///     <h3>Gamma</h3>
///     <list type="table">
///       <item>
///         <term>G10</term>
///         <description><strong>Gamma 1.0</strong>. Linear light levels.</description>
///       </item>
///       <item>
///         <term>G22</term>
///         <description>
///           <strong>Gamma 2.2</strong>. Commonly used for sRGB and BT.709 (linear segment + 2.4).
///         </description>
///       </item>
///       <item>
///         <term>G24</term>
///         <description>
///           <strong>Gamma 2.4</strong>. Commonly used in cinema and professional video workflows.
///         </description>
///       </item>
///       <item>
///         <term>G2084</term>
///         <description>SMPTE ST.2084 (Perceptual Quantization).</description>
///       </item>
///     </list>
///
///     <h3>Siting</h3>
///     <para>
///       "Siting" indicates a horizontal or vertical shift of the chrominance channels relative to the luminance channel.
///       "Cositing" indicates values are sited between pixels in the vertical or horizontal direction (also known as being "sited interstitially").
///     </para>
///     <list type="table">
///       <item>
///         <term>None</term>
///         <description><strong>For images</strong>. The U and V planes are aligned vertically.</description>
///       </item>
///       <item>
///         <term>Left</term>
///         <description>
///           <strong>For video</strong>. Chroma samples are aligned horizontally with the luma samples, or with multiples of the luma samples.
///           The U and V planes are aligned vertically.
///         </description>
///       </item>
///       <item>
///         <term>TopLeft</term>
///         <description>
///           <strong>For video</strong>. The sampling point is the top left pixel (usually of a 2x2 pixel block). Chroma samples are
///           aligned horizontally with the luma samples, or with multiples of the luma samples. Chroma samples are also aligned vertically
///           with the luma samples, or with multiples of the luma samples.
///         </description>
///       </item>
///     </list>
///
///     <h3>Primaries</h3>
///     <list type="table">
///       <item>
///         <term>P601</term>
///         <description><strong>BT.601</strong>. Standard defining digital encoding of SDTV video.</description>
///       </item>
///       <item>
///         <term>P709</term>
///         <description>
///           <strong>BT.709</strong>. Standard defining digital encoding of HDTV video.
///         </description>
///       </item>
///       <item>
///         <term>P2020</term>
///         <description><strong>BT.2020</strong>. Standard defining ultra-high definition television (UHDTV).</description>
///       </item>
///     </list>
///
///     <h3>Transfer Matrix</h3>
///     <para>
///       In most cases, the transfer matrix can be determined from the primaries. For some cases it must be explicitly specified as described below:
///     </para>
///     <list type="table">
///       <item>
///         <term>X601</term>
///         <description><strong>BT.601</strong>. Standard defining digital encoding of SDTV video.</description>
///       </item>
///       <item>
///         <term>X709</term>
///         <description>
///           <strong>BT.709</strong>. Standard defining digital encoding of HDTV video.
///         </description>
///       </item>
///       <item>
///         <term>X2020</term>
///         <description><strong>BT.2020</strong>. Standard defining ultra-high definition television (UHDTV).</description>
///       </item>
///     </list>
///   </para>
/// </remarks>
public enum ColorSpaceType : uint
{
    // From DXGI_COLOR_SPACE_TYPE in dxgicommon.h

    /// <summary>
    ///   A custom color definition is used.
    /// </summary>
    Custom = 0xFFFFFFFF,  // TODO: Expose a way to define custom color spaces (red, green, blue, white point, etc.)

    #region Color Space: RGB

    // Range: Full -----------------------------------------------------------------------

    /// <summary>
    ///   <para>
    ///     This is the standard definition for sRGB. Note that this is often implemented with a linear segment,
    ///     but in that case, the exponent is corrected to stay aligned with a gamma 2.2 curve.
    ///   </para>
    ///   <para>
    ///     This is usually used with 8-bit and 10-bit color channels. Use with 8-bit-per-channel Back-Buffers formats,
    ///     such as <see cref="PixelFormat.B8G8R8A8_UNorm"/>.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Full_G22_None_P709 = 0,

    /// <summary>
    ///   <para>
    ///     This is the linear RGB color space with BT.709 primaries. Linear gamma means no gamma correction is applied,
    ///     making it suitable for HDR content and mathematical operations on color values.
    ///   </para>
    ///   <para>
    ///     It is the standard definition for scRGB, and is usually used with 16-bit integer,
    ///     16-bit floating point, and 32-bit floating point channels.
    ///   </para>
    ///   <para>
    ///     Commonly used in HDR workflows and when precise color calculations are required.
    ///     Use with floating-point formats like <see cref="PixelFormat.R16G16B16A16_Float"/>.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>1.0</strong> (Linear)</item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Full_G10_None_P709 = 1,

    /// <summary>
    ///   <para>
    ///     This is RGB with Perceptual Quantizer (PQ) transfer function and BT.2020 primaries. PQ (ST-2084)
    ///     is designed for HDR content and can represent brightness levels up to 10,000 nits.
    ///   </para>
    ///   <para>
    ///     Used for HDR10 content and high dynamic range applications. Requires HDR-capable displays
    ///     and processing. Common with formats like <see cref="PixelFormat.R10G10B10A2_UNorm"/>.
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>ST-2084 (PQ)</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Full_G2084_None_P2020 = 12,

    /// <summary>
    ///   <para>
    ///     This is RGB with wide color gamut BT.2020 primaries and traditional gamma 2.2.
    ///     Provides wider color gamut than BT.709 while maintaining compatibility with SDR workflows.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used for wide color gamut content that doesn't require HDR transfer functions.
    ///     Suitable for displays capable of BT.2020 color reproduction.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Full_G22_None_P2020 = 17,


    // Range: Studio -----------------------------------------------------------------------

    /// <summary>
    ///   <para>
    ///     This is RGB with studio/limited range, commonly used in broadcast and professional video production.
    ///     The limited range reserves headroom and footroom for video processing.
    ///   </para>
    ///   <para>
    ///     It is the standard definition for ITU-R Recommendation BT.709. Note that due to the inclusion of a linear segment,
    ///     the transfer curve looks similar to a pure exponential gamma of 1.9.
    ///     This is usually used with 8-bit and 10-bit color channels.
    ///   </para>
    ///   <para>
    ///     Typically used in broadcast workflows and professional video applications where studio range is required.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Studio</strong> (16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Studio_G22_None_P709 = 2,

    /// <summary>
    ///   <para>
    ///     This is RGB with studio range and wide color gamut BT.2020 primaries. BT.2020 provides a much wider
    ///     color gamut than BT.709, supporting more vivid and saturated colors.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in Ultra HD (4K) and HDR content production. Requires displays capable of wide color gamut reproduction.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Studio</strong> (16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Studio_G22_None_P2020 = 3,

    /// <summary>
    ///   <para>
    ///     This is RGB with Perceptual Quantizer (PQ) transfer function, studio range, and BT.2020 primaries.
    ///     Studio range provides headroom for HDR processing while maintaining compatibility with broadcast standards.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in professional HDR workflows where studio range is required for broadcast compatibility.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Studio</strong> (16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit)</item>
    ///     <item>Gamma: <strong>ST-2084 (PQ)</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Studio_G2084_None_P2020 = 14,

    /// <summary>
    ///   <para>
    ///     This is RGB with gamma 2.4 transfer function, studio range, and BT.709 primaries. Gamma 2.4
    ///     is used in some professional and cinema workflows for more precise color reproduction.
    ///   </para>
    ///   <para>
    ///     It is usually used with 8, 10, or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in professional video production and cinema workflows where gamma 2.4 is specified.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Studio</strong> (16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.4</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Studio_G24_None_P709 = 20,

    /// <summary>
    ///   <para>
    ///     This is RGB with gamma 2.4 transfer function, studio range, and wide color gamut BT.2020 primaries.
    ///     Combines precise gamma 2.4 with wide color gamut for professional workflows.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in professional wide color gamut workflows where gamma 2.4 is specified.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>RGB</strong></item>
    ///     <item>Range: <strong>Studio</strong> (16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.4</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    Rgb_Studio_G24_None_P2020 = 21,

    #endregion

    #region Color Space: YCbCr, YUV

    // Range: Full -----------------------------------------------------------------------

    /// <summary>
    ///   <para>
    ///     This is YCbCr with full range using BT.709 primaries but BT.601 matrix coefficients.
    ///     This combination is sometimes used for compatibility reasons in certain legacy workflows.
    ///   </para>
    ///   <para>
    ///     It is usually used with 8, 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Less common configuration, typically found in specific legacy or compatibility scenarios,
    ///     for example, it is commonly used in JPEG images and some video codecs.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Image</strong></item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///     <item>Transfer Matrix: <strong>BT.601</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Full_G22_None_P709_X601 = 5,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with full range and BT.601 primaries. Full range utilizes the complete bit depth
    ///     available, providing slightly better precision than studio range.
    ///   </para>
    ///   <para>
    ///     It is usually used with 8, 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Sometimes used in JPEG images and certain video codecs that support full range YCbCr, such
    ///     as H.264 camera capture.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.601</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Full_G22_Left_P601 = 7,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with full range and BT.709 primaries. Full range provides better utilization
    ///     of the available bit depth compared to studio range.
    ///   </para>
    ///   <para>
    ///     It is usually used with 8, 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in some video codecs and applications that prefer full range for better precision.
    ///     Sometimes used for H.264 camera capture.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Full_G22_Left_P709 = 9,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with full range and wide color gamut BT.2020 primaries. Full range provides
    ///     better bit depth utilization while BT.2020 enables wide color gamut reproduction.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in wide color gamut applications where full range precision is desired.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Full_G22_Left_P2020 = 11,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with Hybrid Log-Gamma (HLG) transfer function, full range, and BT.2020 primaries.
    ///     Full range provides better bit depth utilization while maintaining HLG's backward compatibility.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in HDR applications where full range precision is desired with HLG transfer function.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>Range: <strong>Full</strong> (0-255 for 8-bit, 0-1023 for 10-bit, 0-4095 for 12-bit)</item>
    ///     <item>Gamma: <strong>HLG</strong> (Hybrid Log-Gamma)</item>
    ///     <item>Siting: <strong>Video</strong> (Top-Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Full_Ghlg_Topleft_P2020 = 19,


    // Range: Studio -----------------------------------------------------------------------

    /// <summary>
    ///   <para>
    ///     This is the standard definition television (SDTV) color space with BT.601 primaries and matrix.
    ///     Left siting means chroma samples are aligned with the left edge of luma samples.
    ///   </para>
    ///   <para>
    ///     It is usually used with 8, 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used for standard definition video content, DVDs, and legacy broadcast systems.
    ///     Commonly used for MPEG2. It is ususally used with formats like <c>NV12</c>.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.601</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G22_Left_P601 = 6,

    /// <summary>
    ///   <para>
    ///     This is the standard high definition television (HDTV) color space with BT.709 primaries.
    ///     This is the most common color space for HD video content and Blu-ray discs.
    ///   </para>
    ///   <para>
    ///     It is usually used with 8, 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Widely used for HD video content, streaming, and broadcast television.
    ///     Compatible with most HD video formats and codecs, like H.264 and HEVC.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G22_Left_P709 = 8,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with wide color gamut BT.2020 primaries and studio range. BT.2020 supports
    ///     a much wider color gamut than BT.709, enabling more vivid and saturated colors.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used for Ultra HD (4K) content and wide color gamut video production.
    ///     Requires compatible displays and processing pipelines, like those used in HEVC (H.265) video encoding.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G22_Left_P2020 = 10,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with Perceptual Quantizer (PQ) transfer function, studio range, and BT.2020 primaries.
    ///     This is the standard color space for HDR10 video content.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     The primary color space for HDR10 video streams, Ultra HD Blu-ray, and HDR streaming content.
    ///     Widely supported by HDR displays and media players.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>ST-2084 (PQ)</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G2084_Left_P2020 = 13,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with BT.2020 primaries and top-left chroma siting. Top-left siting means
    ///     chroma samples are aligned with the top-left corner of the corresponding luma samples.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in specific video processing pipelines where top-left chroma siting is required.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>2.2</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Top-Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G22_Topleft_P2020 = 15,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with Perceptual Quantizer (PQ) transfer function, BT.2020 primaries, and top-left chroma siting.
    ///     Combines HDR capabilities with specific chroma siting requirements.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10, 12, or 16-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in HDR video processing where top-left chroma siting is specifically required.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>ST-2084 (PQ)</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Top-Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G2084_Topleft_P2020 = 16,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with Hybrid Log-Gamma (HLG) transfer function and BT.2020 primaries. HLG is designed
    ///     for HDR broadcasting and provides backward compatibility with SDR displays.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used for HDR broadcasting and live TV where backward compatibility with SDR is important.
    ///     Common in broadcast television and streaming services.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>HLG</strong> (Hybrid Log-Gamma)</item>
    ///     <item>Siting: <strong>Video</strong> (Top-Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_Ghlg_Topleft_P2020 = 18,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with gamma 2.4 transfer function, studio range, and BT.709 primaries.
    ///     Gamma 2.4 provides more precise color reproduction in professional video workflows.
    ///   </para>
    ///   <para>
    ///     It is usually used with 8, 10, or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in professional video production where gamma 2.4 is specified for HD content.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>2.4</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.709</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G24_Left_P709 = 22,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with gamma 2.4 transfer function, studio range, and wide color gamut BT.2020 primaries.
    ///     Combines precise gamma 2.4 with wide color gamut for professional workflows.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in professional wide color gamut video production where gamma 2.4 is specified.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>2.4</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G24_Left_P2020 = 23,

    /// <summary>
    ///   <para>
    ///     This is YCbCr with gamma 2.4 transfer function, studio range, BT.2020 primaries, and top-left chroma siting.
    ///     Combines precise gamma 2.4 with wide color gamut and specific chroma siting requirements.
    ///   </para>
    ///   <para>
    ///     It is usually used with 10 or 12-bit color channels.
    ///   </para>
    ///   <para>
    ///     Used in professional wide color gamut video production where gamma 2.4 and top-left chroma siting are specified.
    ///   </para>
    ///   <list type="bullet">
    ///     <item>Color Space: <strong>YCbCr</strong></item>
    ///     <item>
    ///       Range: <strong>Studio</strong> (luma: 16-235 for 8-bit, 64-940 for 10-bit, 256-3760 for 12-bit;
    ///                                       chroma (Cb/Cr): 16-240 for 8-bit, 64-960 for 10-bit, 256-3840 for 12-bit).
    ///     </item>
    ///     <item>Gamma: <strong>2.4</strong></item>
    ///     <item>Siting: <strong>Video</strong> (Top-Left)</item>
    ///     <item>Primaries: <strong>BT.2020</strong></item>
    ///   </list>
    /// </summary>
    YCbCr_Studio_G24_Topleft_P2020 = 24

    #endregion
}
