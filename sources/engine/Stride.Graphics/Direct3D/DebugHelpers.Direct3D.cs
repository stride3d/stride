// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Stride.Core;

namespace Stride.Graphics;

internal static unsafe class DebugHelpers
{
    // From d3dcommon.h in Windows SDK
    public static Guid* WKPDID_D3DDebugObjectName
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x22, 0x8C, 0x9B, 0x42,
                0x88, 0x91,
                0x0C, 0x4B,
                0x87,
                0x42,
                0xAC,
                0xB0,
                0xBF,
                0x85,
                0xC2,
                0x00
            ];

            Debug.Assert(data.Length == Unsafe.SizeOf<Guid>());

            return (Guid*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        }
    }

    /// <summary>
    ///   Associates a debug name to the private data of a device child, useful to see a friendly name
    ///   in some graphics debuggers.
    /// </summary>
#if STRIDE_GRAPHICS_API_DIRECT3D11
    public static void SetDebugName(this ComPtr<ID3D11DeviceChild> deviceChild, string name, ICollectorHolder owningObject)
    {
        var nameMemory = SilkMarshal.StringToMemory(name, NativeStringEncoding.LPWStr);
        nameMemory.DisposeBy(owningObject);

        deviceChild.SetPrivateData(WKPDID_D3DDebugObjectName, (uint) nameMemory.Length, nameMemory.AsPtr<char>());
    }
#elif STRIDE_GRAPHICS_API_DIRECT3D12
    public static unsafe void SetDebugName(this ComPtr<ID3D12DeviceChild> deviceChild, string name)
    {
        using var nameMemory = SilkMarshal.StringToMemory(name, NativeStringEncoding.LPWStr);
        deviceChild->SetName(nameMemory.AsPtr<char>());
    }
#endif

    /// <summary>
    ///   Associates a debug name to the private data of a DXGI object, useful to see a friendly name
    ///   in some graphics debuggers.
    /// </summary>
    public static void SetDebugName(this ComPtr<IDXGIObject> dxgiObject, string name, ICollectorHolder owningObject)
    {
        var nameMemory = SilkMarshal.StringToMemory(name, NativeStringEncoding.LPWStr);
        nameMemory.DisposeBy(owningObject);

        dxgiObject.SetPrivateData(WKPDID_D3DDebugObjectName, (uint)nameMemory.Length, nameMemory.AsPtr<char>());
    }
}

#endif
