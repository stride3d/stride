// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Stride.TextureConverter.PvrttWrapper;

/// <summary>
/// Binding class of PVR Texture class PVRTexture.
/// </summary>
internal class PVRTexture : IDisposable
{
    internal IntPtr texture;

    #region Constants
        const uint TOPMIPLEVEL = 0;
        const int ALLMIPLEVELS = -1;
    #endregion

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern IntPtr PVRTexLib_CreateTexture(IntPtr header, IntPtr data);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern IntPtr PVRTexLib_CreateTextureFromData(IntPtr pTexture);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern IntPtr PVRTexLib_CreateTextureFromFile(string filePath);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern void PVRTexLib_DestroyTexture(IntPtr texture);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_SaveTextureToFile(IntPtr texture, string filePath);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern IntPtr PVRTexLib_GetTextureHeader(IntPtr texture);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern IntPtr PVRTexLib_GetTextureDataPtr(IntPtr texture, uint uiMIPLevel, uint uiArrayMember, uint uiFaceNumber);

    [DllImport("PVRTexLib", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    private static extern bool PVRTexLib_GenerateMIPMaps(IntPtr texture, EResizeMode eFilterMode, int uiMIPMapsToDo);


    public PVRTexture()
    {
        texture = PVRTexLib_CreateTexture(0,0);
    }

    public PVRTexture(IntPtr pTexture)
    {
        texture = PVRTexLib_CreateTextureFromData(pTexture);
    }

    public PVRTexture(string filePath)
    {
        texture = PVRTexLib_CreateTextureFromFile(filePath);
    }

    public PVRTexture(PVRTextureHeader headerIn, IntPtr data)
    {
        texture = PVRTexLib_CreateTexture(headerIn.header, data);
    }

    public bool Save(string filePath)
    {
        return PVRTexLib_SaveTextureToFile(texture, filePath);
    }

    public PVRTextureHeader GetHeader()
    {
        return new PVRTextureHeader(PVRTexLib_GetTextureHeader(texture));
    }

    public IntPtr GetDataPtr(uint uiMIPLevel = 0, uint uiArrayMember = 0, uint uiFaceNumber = 0)
    {
        return PVRTexLib_GetTextureDataPtr(texture, uiMIPLevel, uiArrayMember, uiFaceNumber);
    }

    public bool GenerateMIPMaps(EResizeMode eFilterMode, int uiMIPMapsToDo = ALLMIPLEVELS)
    {
        return PVRTexLib_GenerateMIPMaps(texture, eFilterMode, uiMIPMapsToDo);
    }
    
    public void Dispose()
    {
        PVRTexLib_DestroyTexture(texture);
    }
}
