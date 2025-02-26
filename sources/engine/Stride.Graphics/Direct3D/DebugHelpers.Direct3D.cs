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
using Stride.Core.UnsafeExtensions;

namespace Stride.Graphics;

internal static unsafe class DebugHelpers
{
    // From d3dcommon.h in Windows SDK (WKPDID_D3DDebugObjectName)
    public static Guid* DebugObjectName
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

            Debug.Assert(data.Length == sizeof(Guid));

            return (Guid*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        }
    }


    /// <summary>
    ///   Associates a debug name to the private data of a DXGI or Direct3D object, useful to see a friendly name
    ///   in some graphics debuggers.
    /// </summary>
    /// <param name="comPtr">A COM pointer to the object to set the debug name for.</param>
    /// <param name="name">The name to associate with the object.</param>
    /// <exception cref="NotSupportedException">Thrown when the specified COM pointer type is not supported.</exception>
    public static void SetDebugName<T>(this ComPtr<T> comPtr, string name)
        where T : unmanaged, IComVtbl<T>
    {
        using var nameMemory = SilkMarshal.StringToMemory(name, NativeStringEncoding.LPWStr);

        switch (comPtr)
        {
            case IComVtbl<IDXGIObject>:
                var dxgiObject = comPtr.BitCast<ComPtr<T>, ComPtr<IDXGIObject>>();
                dxgiObject.SetPrivateData(DebugObjectName, (uint) nameMemory.Length, nameMemory.AsPtr<char>());
                break;

#if STRIDE_GRAPHICS_API_DIRECT3D11
            case IComVtbl<ID3D11DeviceChild>:
                var d3d11DeviceChild = comPtr.BitCast<ComPtr<T>, ComPtr<ID3D11DeviceChild>>();
                d3d11DeviceChild.SetPrivateData(DebugObjectName, (uint) nameMemory.Length, nameMemory.AsPtr<char>());
                break;
#elif STRIDE_GRAPHICS_API_DIRECT3D12
            case IComVtbl<ID3D12DeviceChild>:
                var d3d12DeviceChild = comPtr.BitCast<ComPtr<T>, ComPtr<ID3D12DeviceChild>>();
                d3d12DeviceChild.SetName(nameMemory.AsPtr<char>());
                break;
#endif
            default:
                throw new NotSupportedException("The specified COM pointer type is not supported.");
        }
    }
}

#endif
