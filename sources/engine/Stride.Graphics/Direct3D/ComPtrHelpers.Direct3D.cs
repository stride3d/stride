// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
#if STRIDE_GRAPHICS_API_DIRECT3D11
using Silk.NET.Direct3D11;
#elif STRIDE_GRAPHICS_API_DIRECT3D12
using Silk.NET.Direct3D12;
#endif
using Silk.NET.DXGI;
using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Defines helper methods to aid in the usage of Direct3D COM pointers with <see cref="ComPtr{T}"/>.
/// </summary>
internal static unsafe class ComPtrHelpers
{
    /// <summary>
    ///   Returns a <see langword="null"/> COM pointer.
    /// </summary>
    /// <typeparam name="T">The type of the COM pointer.</typeparam>
    /// <returns>
    ///   A <see langword="null"/> COM pointer.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComPtr<T> NullComPtr<T>()
        where T : unmanaged, IComVtbl<T>
    {
        return default;
    }

    /// <summary>
    ///   Checks if the underlying COM pointer is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The type of the COM pointer.</typeparam>
    /// <param name="comPtr">The COM pointer to check.</param>
    /// <returns>
    ///   <see langword="true"/> if the COM pointer is <see langword="null"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNull<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IComVtbl<T>
    {
        return comPtr.Handle == null;
    }

    /// <summary>
    ///   Checks if the underlying COM pointer is a valid non-<see langword="null"/> pointer.
    /// </summary>
    /// <typeparam name="T">The type of the COM pointer.</typeparam>
    /// <param name="comPtr">The COM pointer to check.</param>
    /// <returns>
    ///   <see langword="true"/> if the COM pointer is not <see langword="null"/>; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotNull<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IComVtbl<T>
    {
        return comPtr.Handle != null;
    }

    /// <summary>
    ///   Returns a new <see cref="ComPtr{T}"/> instance wrapping the specified native COM pointer without
    ///   altering its reference count.
    /// </summary>
    /// <typeparam name="T">The type of the COM pointer.</typeparam>
    /// <param name="nativePtr">The native COM pointer to wrap.</param>
    /// <returns>A <see cref="ComPtr{T}"/> instance wrapping <paramref name="nativePtr"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComPtr<T> ToComPtr<T>(T* nativePtr)
        where T : unmanaged, IComVtbl<T>
    {
        return new ComPtr<T> { Handle = nativePtr };
    }

    /// <summary>
    ///   Returns a new <see cref="ComPtr{T}"/> instance wrapping the specified native COM pointer without
    ///   altering its reference count.
    /// </summary>
    /// <typeparam name="TParent">The type of the resulting COM pointer.</typeparam>
    /// <typeparam name="TChild">The type of the COM pointer to an interface that inherits from that of <typeparamref name="TParent"/>.</typeparam>
    /// <param name="nativePtr">The native COM pointer to wrap.</param>
    /// <returns>A <see cref="ComPtr{T}"/> to a <typeparamref name="TParent"/> instance wrapping <paramref name="nativePtr"/>.</returns>
    public static ComPtr<TParent> ToComPtr<TParent, TChild>(TChild* nativePtr)
        where TParent : unmanaged, IComVtbl<TParent>
        where TChild : unmanaged, IComVtbl<TChild>, IComVtbl<TParent>
    {
        var parentPtr = (TParent*) nativePtr;
        return new ComPtr<TParent> { Handle = parentPtr };
    }

    /// <summary>
    ///   Releases a COM pointer and sets it to <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The type of the COM pointer.</typeparam>
    /// <param name="nativePtr">The COM pointer to release and forget.</param>
    public static void SafeRelease<T>(ref T* nativePtr)
        where T : unmanaged, IComVtbl<IUnknown>
    {
        if (nativePtr != null)
        {
            var iUnknown = (IUnknown*) nativePtr;
            iUnknown->Release();
            nativePtr = null;
        }
    }

    /// <summary>
    ///   Releases a COM pointer and sets it to <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The type of the COM pointer.</typeparam>
    /// <param name="comPtr">The COM pointer to release and forget.</param>
    public static void SafeRelease<T>(ref ComPtr<T> comPtr)
        where T : unmanaged, IComVtbl<IUnknown>, IComVtbl<T>
    {
        if (comPtr.Handle != null)
        {
            var iUnknown = (IUnknown*) comPtr.Detach();
            iUnknown->Release();
        }
    }

    /// <summary>
    ///   Returns a <see cref="ComPtr{T}"/> reinterpreting a COM pointer to a Direct3D 11 device child as a
    ///   COM pointer to a <see cref="ID3D11DeviceChild"/>.
    /// </summary>
    /// <param name="comPtr">The COM pointer of a Direct3D 11 device child.</param>
    /// <returns>The COM pointer reinterpreted as a <see cref="ID3D11DeviceChild"/>.</returns>
    public static ComPtr<IUnknown> AsIUnknown<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IComVtbl<IUnknown>, IComVtbl<T>
    {
        return new ComPtr<IUnknown> { Handle = (IUnknown*) comPtr.Handle };
    }

#if STRIDE_GRAPHICS_API_DIRECT3D11
    ///// <summary>
    /////   Returns a <see cref="ComPtr{T}"/> reinterpreting a COM pointer to a <see cref="ID3D11DeviceContext"/> as a
    /////   COM pointer to a <see cref="ID3D11DeviceChild"/>.
    ///// </summary>
    ///// <param name="deviceContext">The COM pointer of a <see cref="ID3D11DeviceContext"/>.</param>
    ///// <returns>The COM pointer reinterpreted as a <see cref="ID3D11DeviceChild"/>.</returns>
    //public static ComPtr<ID3D11DeviceChild> AsDeviceChild(this ComPtr<ID3D11DeviceContext> deviceContext)
    //{
    //    return new ComPtr<ID3D11DeviceChild> { Handle = (ID3D11DeviceChild*) deviceContext.Handle };
    //}

    /// <summary>
    ///   Returns a <see cref="ComPtr{T}"/> reinterpreting a COM pointer to a Direct3D 11 device child as a
    ///   COM pointer to a <see cref="ID3D11DeviceChild"/>.
    /// </summary>
    /// <param name="comPtr">The COM pointer of a Direct3D 11 device child.</param>
    /// <returns>The COM pointer reinterpreted as a <see cref="ID3D11DeviceChild"/>.</returns>
    public static ComPtr<ID3D11DeviceChild> AsDeviceChild<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IComVtbl<ID3D11DeviceChild>, IComVtbl<T>
    {
        return new ComPtr<ID3D11DeviceChild> { Handle = (ID3D11DeviceChild*) comPtr.Handle };
    }

    /// <summary>
    ///   Reinterprets a <see cref="ComPtr{T}"/> to an interface of type <typeparamref name="TFrom"/> as a
    ///   COM pointer to an interface of type <typeparamref name="TTo"/> which it inherits from.
    /// </summary>
    /// <param name="comPtr">The COM pointer to reinterpret.</param>
    /// <returns>The COM pointer reinterpreted as a <see cref="ComPtr{T}"/> of type <typeparamref name="TTo"/>.</returns>
    public static ComPtr<TTo> AsComPtr<TFrom, TTo>(this ComPtr<TFrom> comPtr)
        where TFrom : unmanaged, IComVtbl<TFrom>, IComVtbl<TTo>
        where TTo : unmanaged, IComVtbl<TTo>
    {
        return new ComPtr<TTo> { Handle = (TTo*) comPtr.Handle };
    }
#endif

#if STRIDE_GRAPHICS_API_DIRECT3D12

#endif
}

#endif
