// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;

namespace Xenko.Core
{
    public class UnmanagedArray<T> : IDisposable where T : struct
    {
        private readonly int sizeOfT;
        private readonly bool isShared;

        public UnmanagedArray(int length)
        {
            Length = length;
            sizeOfT = Utilities.SizeOf<T>();
            var finalSize = length * sizeOfT;
            Pointer = Utilities.AllocateMemory(finalSize);
            isShared = false;
        }

        public UnmanagedArray(int length, IntPtr unmanagedDataPtr)
        {
            Length = length;
            sizeOfT = Utilities.SizeOf<T>();
            Pointer = unmanagedDataPtr;
            isShared = true;
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
                    Interop.Read<T>(bptr, ref res);
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
                    Interop.Write<T>(bptr, ref value);
                }
            }
        }

        public void Read([NotNull] T[] destination, int offset = 0)
        {
            if (offset + destination.Length > Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            unsafe
            {
                Interop.Read((void*)Pointer, destination, offset, destination.Length);
            }        
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
                Interop.Read(ptr, destination, arrayOffset, arrayLen);
            }
        }

        public void Write([NotNull] T[] source, int offset = 0)
        {
            if (offset + source.Length > Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            unsafe
            {
                Interop.Write((void*)Pointer, source, offset, source.Length);
            }
        }

        public void Write(T[] source, int pointerByteOffset, int arrayOffset, int arrayLen)
        {
            if (arrayOffset + arrayLen > Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            unsafe
            {
                var ptr = (byte*)Pointer;
                ptr += pointerByteOffset;
                Interop.Write(ptr, source, arrayOffset, arrayLen);
            }
        }

        public IntPtr Pointer { get; }

        public int Length { get; }
    }
}
