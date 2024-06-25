// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Stride.TextureConverter.PvrttWrapper;

/// <summary>
/// Binding class of PVR Texture class PVRTextureHeader.
/// </summary>
internal class PVRTextureHeader : IDisposable
{
    public IntPtr header { internal set; get; }

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern IntPtr PVRTexLib_CreateTextureHeader(PVRHeaderCreateParams parameters);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern uint PVRTexLib_GetTextureWidth(IntPtr header, uint uiMipLevel);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern uint PVRTexLib_GetTextureHeight(IntPtr header, uint uiMipLevel);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern uint PVRTexLib_GetTextureNumMipMapLevels(IntPtr header);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern uint PVRTexLib_GetTextureDataSize(IntPtr header, int iMipLevel, bool bAllSurfaces, bool bAllFaces);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern uint PVRTexLib_GetTextureDepth(IntPtr header, uint uiMipLevel);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern uint PVRTexLib_GetTextureNumArrayMembers(IntPtr header);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern uint PVRTexLib_GetTextureNumFaces(IntPtr header);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern ulong PVRTexLib_GetTexturePixelFormat(IntPtr header);        
    
    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern EPVRTVariableType PVRTexLib_GetTextureChannelType(IntPtr header);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern EPVRTColourSpace PVRTexLib_GetTextureColourSpace(IntPtr header);

    public PVRTextureHeader()
    {
        header = PVRTexLib_CreateTextureHeader(new());
    }

    public PVRTextureHeader(ulong pixelFormat, int height=1, int width=1, int depth=1, int numMipMaps=1, int numArrayMembers=1, int numFaces=1, EPVRTColourSpace eColourSpace=EPVRTColourSpace.Linear, EPVRTVariableType eChannelType=EPVRTVariableType.UnsignedByteNorm, bool bPreMultiplied=false)
    {
        header = PVRTexLib_CreateTextureHeader(new(){
            pixelFormat = pixelFormat,
            height = (uint)height,
            width = (uint)width,
            depth = (uint)depth,
            numMipMaps = (uint)numMipMaps,
            numArrayMembers = (uint)numArrayMembers,
            numFaces = (uint)numFaces,
            colourSpace = eColourSpace,
            channelType = eChannelType,
            preMultiplied = bPreMultiplied
        });
    }

    public PVRTextureHeader(IntPtr headerPtr)
    {
        header = headerPtr;
    }

    public uint GetWidth(uint uiMipLevel = Constant.TOPMIPLEVEL)
    {
        return PVRTexLib_GetTextureWidth(header, uiMipLevel);
    }

    public uint GetHeight(uint uiMipLevel = Constant.TOPMIPLEVEL)
    {
        return PVRTexLib_GetTextureHeight(header, uiMipLevel);
    }

    public uint GetNumMIPLevels()
    {
        return PVRTexLib_GetTextureNumMipMapLevels(header);
    }

    public uint GetDataSize(int iMipLevel = Constant.ALLMIPLEVELS, bool bAllSurfaces = true, bool bAllFaces = true)
    {
        return PVRTexLib_GetTextureDataSize(header, iMipLevel, bAllSurfaces, bAllFaces);
    }

    public uint GetDepth(uint uiMipLevel = Constant.TOPMIPLEVEL)
    {
        return PVRTexLib_GetTextureDepth(header, uiMipLevel);
    }
    
    public uint GetNumArrayMembers()
    {
        return PVRTexLib_GetTextureNumArrayMembers(header);
    }
    
    public uint GetNumFaces()
    {
        return PVRTexLib_GetTextureNumFaces(header);
    }

    public EPVRTVariableType GetChannelType()
    {
        return PVRTexLib_GetTextureChannelType(header);
    }

    public EPVRTColourSpace GetColourSpace()
    {
        return PVRTexLib_GetTextureColourSpace(header);
    }

    public Stride.Graphics.PixelFormat GetFormat()
    {
        EPVRTPixelFormat format = (EPVRTPixelFormat)PVRTexLib_GetTexturePixelFormat(header);

        switch (format)
        {
            case EPVRTPixelFormat.ETC1:
                return Stride.Graphics.PixelFormat.ETC1;
            case EPVRTPixelFormat.ETC2_RGB:
                return Stride.Graphics.PixelFormat.ETC2_RGB;
            case EPVRTPixelFormat.ETC2_RGBA:
                return Stride.Graphics.PixelFormat.ETC2_RGBA;
            case EPVRTPixelFormat.ETC2_RGB_A1:
                return Stride.Graphics.PixelFormat.ETC2_RGB_A1;
            case EPVRTPixelFormat.EAC_R11:
                return Stride.Graphics.PixelFormat.EAC_R11_Signed;
            case EPVRTPixelFormat.EAC_RG11:
                return Stride.Graphics.PixelFormat.EAC_RG11_Signed;
        }

        if (format == EPVRTPixelFormat.RGBG8888)
            return Stride.Graphics.PixelFormat.R8G8B8A8_UNorm;
        
        return Stride.Graphics.PixelFormat.None;
    }

    public int GetAlphaDepth()
    {
        ulong format = PVRTexLib_GetTexturePixelFormat(header);
        if (format <= 0xffffffff)
        {
            switch (format)
            {
                case (int)EPVRTPixelFormat.PVRTCI_2bpp_RGB:
                case (int)EPVRTPixelFormat.PVRTCI_4bpp_RGB:
                case (int)EPVRTPixelFormat.ETC1:
                case (int)EPVRTPixelFormat.ETC2_RGB:
                case (int)EPVRTPixelFormat.EAC_R11:
                case (int)EPVRTPixelFormat.EAC_RG11:
                    return 0;

                case (int)EPVRTPixelFormat.ETC2_RGB_A1:
                    return 1;

                case (int)EPVRTPixelFormat.ETC2_RGBA:
                case (int)EPVRTPixelFormat.PVRTCI_2bpp_RGBA:
                case (int)EPVRTPixelFormat.PVRTCI_4bpp_RGBA:
                    return 8;

                case (int)EPVRTPixelFormat.PVRTCII_2bpp:
                case (int)EPVRTPixelFormat.PVRTCII_4bpp:
                    return 8;  // or 0
            }
            return 0;
        }
        for (int i = 0 ; i < 4 ; i++)
        {
            if (((format & 255)|0x20) == 'a')
            {
                return (int)(format>>32) & 255;
            }
            format >>= 8;
        }
        return 0;
    }

    public void Dispose()
    {
    }
}