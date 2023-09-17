// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;

namespace Stride.Core
{
    [Obsolete("Obtain Memory<T> using GC.Allocate*Array or a Stride-specific allocator mechanism.")]
    public class UnmanagedArray<T> : IDisposable where T : struct
    {
        private readonly int sizeOfT;
        private readonly bool isShared;

        [Obsolete("Obtain Memory<T> using GC.Allocate*Array or a Stride-specific allocator mechanism.")]
        public UnmanagedArray(int length)
        {
            Length = length;
            sizeOfT = Unsafe.SizeOf<T>();
            var finalSize = length * sizeOfT;
            Pointer = Utilities.AllocateMemory(finalSize);
            isShared = false;
        }

        public void Dispose()
        {
            if (!isShared)
            {
                Utilities.FreeMemory(Pointer);
            }
        }

        public T this[int index]
        {
            get
            {
                if (index >= Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var res = new T();

                unsafe
                {
                    var bptr = (byte*)Pointer;
                    bptr += index * sizeOfT;
                    res = Unsafe.ReadUnaligned<T>(bptr);
                }

                return res;
            }
            set
            {
                if (index >= Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                unsafe
                {
                    var bptr = (byte*)Pointer;
                    bptr += index * sizeOfT;
                    Unsafe.WriteUnaligned(bptr, value);
                }
            }
        }

        public unsafe void Read([NotNull] T[] destination, int offset = 0)
        {
            if (offset + destination.Length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            // Interop.Read((void*)Pointer, destination, offset, destination.Length);
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
                // Interop.Read(ptr, destination, arrayOffset, arrayLen);
                new Span<T>(ptr, sizeOfT * arrayLen)
                    .CopyTo(destination.AsSpan(arrayOffset, arrayLen));
            }
        }

        public unsafe void Write([NotNull] T[] source, int offset = 0)
        {
            if (offset + source.Length > Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            // Interop.Write((void*)Pointer, source, offset, source.Length);
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
            // Interop.Write(ptr, source, arrayOffset, arrayLen);
            source.AsSpan(arrayOffset, arrayLen).CopyTo(new Span<T>(ptr, arrayLen));
        }

        public IntPtr Pointer { get; }

        public int Length { get; }
    }
}
