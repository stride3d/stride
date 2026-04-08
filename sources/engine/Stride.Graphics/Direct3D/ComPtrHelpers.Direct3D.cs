// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_DIRECT3D12

using System.Runtime.CompilerServices;

using Silk.NET.Core.Native;
#if STRIDE_GRAPHICS_API_DIRECT3D11
using Silk.NET.Direct3D11;
#elif STRIDE_GRAPHICS_API_DIRECT3D12
using Silk.NET.Direct3D12;
#endif

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
    ///  Indicates whether two COM pointers are equal (i.e. point to the same COM object).
    /// </summary>
    /// <typeparam name="T">The type of the COM pointer.</typeparam>
    /// <param name="left">The first COM pointer to compare.</param>
    /// <param name="right">The second COM pointer to compare.</param>
    /// <returns>
    ///   <see langword="true"/> if the two COM pointer are equal; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsComPtr<T>(this ComPtr<T> left, ComPtr<T> right)
        where T : unmanaged, IComVtbl<T>
    {
        return left.Handle == right.Handle;
    }

    /// <summary>
    ///   Casts a COM pointer from one interface type to another.
    /// </summary>
    /// <typeparam name="TFrom">The source interface type.</typeparam>
    /// <typeparam name="TTo">The target interface type.</typeparam>
    /// <param name="comPtr">The COM pointer to be cast.</param>
    /// <returns>A new <see cref="ComPtr{TTo}"/> representing the casted COM pointer.</returns>
    /// <remarks>
    ///   This method performs a direct cast of the underlying pointer. It is the caller's responsibility
    ///   to ensure that the cast is valid.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComPtr<TTo> CastComPtr<TFrom, TTo>(ComPtr<TFrom> comPtr)
        where TFrom : unmanaged, IComVtbl<TFrom>
        where TTo : unmanaged, IComVtbl<TTo>
    {
        return new ComPtr<TTo> { Handle = (TTo*) comPtr.Handle };
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

    /// <summary>
    ///   Reinterprets a <see cref="ComPtr{T}"/> to an interface of type <typeparamref name="TFrom"/> as a
    ///   COM pointer to an interface of type <typeparamref name="TTo"/> which it inherits from
    ///   (i.e., from a more <strong>specific type</strong> to a more <strong>generic type</strong>).
    /// </summary>
    /// <param name="comPtr">The COM pointer to reinterpret.</param>
    /// <returns>The COM pointer reinterpreted as a <see cref="ComPtr{T}"/> of type <typeparamref name="TTo"/>.</returns>
    public static ComPtr<TTo> AsComPtr<TFrom, TTo>(this ComPtr<TFrom> comPtr)
        where TFrom : unmanaged, IComVtbl<TFrom>, IComVtbl<TTo>
        where TTo : unmanaged, IComVtbl<TTo>
    {
        return new ComPtr<TTo> { Handle = (TTo*) comPtr.Handle };
    }

    /// <summary>
    ///   Reinterprets a <see cref="ComPtr{T}"/> to an interface of type <typeparamref name="TFrom"/> as a
    ///   COM pointer to an interface of type <typeparamref name="TTo"/> which inherits from it
    ///   (i.e., from a more <strong>generic type</strong> to a more <strong>specific type</strong>).
    /// </summary>
    /// <param name="comPtr">The COM pointer to reinterpret.</param>
    /// <returns>The COM pointer reinterpreted as a <see cref="ComPtr{T}"/> of type <typeparamref name="TTo"/>.</returns>
    /// <remarks>
    ///   ⚠️ Warning: This method is unsafe because it allows casting to a more specific interface type.
    ///   Only use this method if you are certain that the underlying COM object actually implements
    ///   the <typeparamref name="TTo"/> interface; otherwise, using the resulting COM pointer may lead to
    ///   undefined behavior, memory corruption, or application crashes.
    /// </remarks>
    public static ComPtr<TTo> AsComPtrUnsafe<TFrom, TTo>(this ComPtr<TFrom> comPtr)
        where TTo : unmanaged, IComVtbl<TTo>, IComVtbl<TFrom>
        where TFrom : unmanaged, IComVtbl<TFrom>
    {
        return new ComPtr<TTo> { Handle = (TTo*) comPtr.Handle };
    }

#if STRIDE_GRAPHICS_API_DIRECT3D11
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
#endif

#if STRIDE_GRAPHICS_API_DIRECT3D12
    /// <summary>
    ///   Returns a <see cref="ComPtr{T}"/> reinterpreting a COM pointer to a Direct3D 12 device child as a
    ///   COM pointer to a <see cref="ID3D12DeviceChild"/>.
    /// </summary>
    /// <param name="comPtr">The COM pointer of a Direct3D 12 device child.</param>
    /// <returns>The COM pointer reinterpreted as a <see cref="ID3D12DeviceChild"/>.</returns>
    public static ComPtr<ID3D12DeviceChild> AsDeviceChild<T>(this ComPtr<T> comPtr)
        where T : unmanaged, IComVtbl<ID3D12DeviceChild>, IComVtbl<T>
    {
        return new ComPtr<ID3D12DeviceChild> { Handle = (ID3D12DeviceChild*) comPtr.Handle };
    }
#endif
}

#endif
