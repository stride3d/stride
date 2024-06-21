// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Stride.TextureConverter.PvrttWrapper;

public class Constant
{
    public const uint TOPMIPLEVEL = 0;
    public const int ALLMIPLEVELS = -1;
}

/// <summary>
/// Provides utilities methods to handle PVR Texture type.
/// </summary>
internal class PVRTextureUtilities
{
    #region Bindings
    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool pvrttTranscodeWithNoConversion(IntPtr texture, PixelType ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality, bool bDoDither);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_TranscodeTexture(IntPtr texture, ulong ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality, bool bDoDither);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_CopyTextureChannels(IntPtr sTexture, IntPtr sTextureSource, uint uiNumChannelCopies, out EChannelName eChannels, out EChannelName eChannelsSource);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_ResizeTexture(IntPtr sTexture, out uint u32NewWidth, out uint u32NewHeight, out uint u32NewDepth, EResizeMode eResizeMode);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_FlipTexture(IntPtr sTexture, EPVRTAxis eFlipDirection);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_GenerateNormalMap(IntPtr sTexture, float fScale, string sChannelOrder);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_PreMultiplyAlpha(IntPtr sTexture);

    #endregion

    /// <summary>
    /// Copies a specified channel from one texture to a specified channel in another texture.
    /// </summary>
    /// <param name="sTexture">The destination texture.</param>
    /// <param name="sTextureSource">The source texture.</param>
    /// <param name="uiNumChannelCopies">The UI num channel copies.</param>
    /// <param name="eChannels">The destination channel.</param>
    /// <param name="eChannelsSource">The source channel.</param>
    /// <returns></returns>
    public static bool CopyChannels(PVRTexture sTexture, PVRTexture sTextureSource, uint uiNumChannelCopies, out EChannelName eChannels, out EChannelName eChannelsSource)
    {
        return PVRTexLib_CopyTextureChannels(sTexture.texture, sTextureSource.texture, uiNumChannelCopies, out eChannels, out eChannelsSource);
    }

    public static bool Transcode(PVRTexture sTexture, PixelType ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality = ECompressorQuality.PVRTCNormal, bool bDoDither = false)
    {
        return pvrttTranscodeWithNoConversion(sTexture.texture, ptFormat, eChannelType, eColourspace, eQuality, bDoDither);
    }

    public static bool Transcode(PVRTexture sTexture, ulong ptFormat, EPVRTVariableType eChannelType, EPVRTColourSpace eColourspace, ECompressorQuality eQuality = ECompressorQuality.PVRTCNormal, bool bDoDither = false)
    {
        return PVRTexLib_TranscodeTexture(sTexture.texture, ptFormat, eChannelType, eColourspace, eQuality, bDoDither);
    }

    /// <summary>
    /// Resizes the specified texture.
    /// </summary>
    /// <param name="sTexture">The texture.</param>
    /// <param name="u32NewWidth">The new width.</param>
    /// <param name="u32NewHeight">The new height.</param>
    /// <param name="u32NewDepth">The new depth.</param>
    /// <param name="eResizeMode">The resize mode (Filter).</param>
    /// <returns></returns>
    public static bool Resize(PVRTexture sTexture, uint u32NewWidth, uint u32NewHeight, uint u32NewDepth, EResizeMode eResizeMode)
    {
        return PVRTexLib_ResizeTexture(sTexture.texture, out u32NewWidth, out u32NewHeight, out u32NewDepth, eResizeMode);
    }

    /// <summary>
    /// Flips the specified texture.
    /// </summary>
    /// <param name="sTexture">The texture.</param>
    /// <param name="eFlipDirection">The flip direction.</param>
    /// <returns></returns>
    public static bool Flip(PVRTexture sTexture, EPVRTAxis eFlipDirection)
    {
        return PVRTexLib_FlipTexture(sTexture.texture, eFlipDirection);
    }

    public static bool GenerateNormalMap(PVRTexture sTexture, float fScale, string sChannelOrder)
    {
        return PVRTexLib_GenerateNormalMap(sTexture.texture, fScale, sChannelOrder);
    }

    public static bool PreMultipliedAlpha(PVRTexture sTexture)
    {
        return PVRTexLib_PreMultiplyAlpha(sTexture.texture);
    }
}
