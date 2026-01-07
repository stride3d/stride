// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core;

[Obsolete("Obtain Memory<T> using GC.Allocate*Array or a Stride-specific allocator mechanism.")]
public class UnmanagedArray<T> : IDisposable where T : struct
{
    private readonly bool isShared;

    [Obsolete("Obtain Memory<T> using GC.Allocate*Array or a Stride-specific allocator mechanism.")]
    public UnmanagedArray(int length)
    {
        Length = length;
        var finalSize = length * Unsafe.SizeOf<T>();
        Pointer = MemoryUtilities.Allocate(finalSize);
        isShared = false;
    }

    public void Dispose()
    {
        if (!isShared)
        {
            MemoryUtilities.Free(Pointer);
        }
    }

    public T this[int index]
    {
        get
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
#else
            if (index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
#endif // NET8_0_OR_GREATER

            T res;
            unsafe
            {
                var bptr = (byte*)Pointer;
                bptr += index * Unsafe.SizeOf<T>();
                // Pointer is aligned, we expect the struct to be aligned as well;
                // If the user decides to Pack=1, this scope is the least of their worries
                res = Unsafe.Read<T>(bptr);
            }

            return res;
        }
        set
        {
#if NET8_0_OR_GREATER
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
#else
            if (index >= Length) throw new ArgumentOutOfRangeException(nameof(index));
#endif // NET8_0_OR_GREATER

            unsafe
            {
                var bptr = (byte*)Pointer;
                bptr += index * Unsafe.SizeOf<T>();
                // Pointer is aligned, we expect the struct to be aligned as well;
                // If the user decides to Pack=1, this scope is the least of their worries
                Unsafe.Write(bptr, value);
            }
        }
    }

    public unsafe void Read(T[] destination, int offset = 0)
    {
        if (offset + destination.Length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        new Span<T>((void*)Pointer, destination.Length - offset).CopyTo(destination.AsSpan(offset));
    }

    public void Read(T[] destination, int pointerByteOffset, int arrayOffset, int arrayLen)
    {
        if (arrayOffset + arrayLen > Length)
        {
            throw new ArgumentOutOfRangeException();
        }

        unsafe
        {
            var ptr = (byte*)Pointer;
            ptr += pointerByteOffset;
            new Span<T>(ptr, Unsafe.SizeOf<T>() * arrayLen)
                .CopyTo(destination.AsSpan(arrayOffset, arrayLen));
        }
    }

    public unsafe void Write(T[] source, int offset = 0)
    {
        if (offset + source.Length > Length)
        {
            throw new ArgumentOutOfRangeException();
        }
        source.AsSpan(offset).CopyTo(new Span<T>((void*)Pointer, source.Length - offset));
    }

    public unsafe void Write(T[] source, int pointerByteOffset, int arrayOffset, int arrayLen)
    {
        if (arrayOffset + arrayLen > Length)
        {
            throw new ArgumentOutOfRangeException();
        }

        var ptr = (byte*)Pointer;
        ptr += pointerByteOffset;
        source.AsSpan(arrayOffset, arrayLen).CopyTo(new Span<T>(ptr, arrayLen));
    }

    public IntPtr Pointer { get; }

    public int Length { get; }
}
