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

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics;

internal static unsafe class DebugHelpers
{
    /// <summary>
    ///   A flag indicating whether to log debug names for Direct3D objects whenever they are set.
    /// </summary>
    public const bool LogDebugNames = true;


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
        var comPtrVtbl = *comPtr.Handle;

        switch (comPtrVtbl)
        {
            case IComVtbl<IDXGIObject>:
            {
                var nameSpan = name.GetAsciiSpan();
                var nameSpanLength = (uint) nameSpan.Length;

                var dxgiObject = CastComPtr<T, IDXGIObject>(comPtr);
                dxgiObject.SetPrivateData(DebugObjectName, nameSpanLength, nameSpan);
                break;
            }
#if STRIDE_GRAPHICS_API_DIRECT3D11
            case IComVtbl<ID3D11DeviceChild>:
            {
                var nameSpan = name.GetAsciiSpan();
                var nameSpanLength = (uint) nameSpan.Length;

                var d3d11DeviceChild = CastComPtr<T, ID3D11DeviceChild>(comPtr);
                d3d11DeviceChild.SetPrivateData(DebugObjectName, nameSpanLength, nameSpan);
                break;
            }
#elif STRIDE_GRAPHICS_API_DIRECT3D12
            case IComVtbl<ID3D12DeviceChild>:
            {
                var d3d12DeviceChild = CastComPtr<T, ID3D12DeviceChild>(comPtr);
                d3d12DeviceChild.SetName(name);
                break;
            }
#endif
            default:
                throw new NotSupportedException("The specified COM pointer type is not supported.");
        }

        if (LogDebugNames)
        {
            // Log the debug name for the object
            var typeName = typeof(T).Name;
            var ptr = (nint) comPtr.Handle;
            Debug.WriteLine($"Changed or set the debug name for {typeName} at 0x{ptr:X8} to '{name}'.");
        }
    }
}

#endif
