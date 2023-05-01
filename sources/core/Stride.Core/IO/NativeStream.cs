// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Stride.Core.IO
{
    /// <summary>
    /// A <see cref="Stream"/> with additional methods for native read and write operations using <see cref="IntPtr"/>.
    /// </summary>
    [Obsolete]
    public abstract class NativeStream : Stream
    {
        protected const int NativeStreamBufferSize = 1024;

        // Helper buffer for classes needing it.
        // If null, it should be initialized with NativeStreamBufferSize constant.
        [Obsolete("Let the caller provide a buffer.")]
        protected byte[] nativeStreamBuffer;

        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public virtual unsafe ushort ReadUInt16()
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            var currentReadSize = Read(temporaryBuffer, 0, sizeof(ushort));
            if (currentReadSize != sizeof(ushort))
                throw new InvalidOperationException("Reached end of stream.");

            fixed (byte* temporaryBufferStart = temporaryBuffer)
            {
                return *((ushort*)temporaryBufferStart);
            }
        }

        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public virtual unsafe uint ReadUInt32()
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            var currentReadSize = Read(temporaryBuffer, 0, sizeof(uint));
            if (currentReadSize != sizeof(uint))
                throw new InvalidOperationException("Reached end of stream.");

            fixed (byte* temporaryBufferStart = temporaryBuffer)
            {
                return *((uint*)temporaryBufferStart);
            }
        }

        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public virtual unsafe ulong ReadUInt64()
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            var currentReadSize = Read(temporaryBuffer, 0, sizeof(ulong));
            if (currentReadSize != sizeof(ulong))
                throw new InvalidOperationException("Reached end of stream.");

            fixed (byte* temporaryBufferStart = temporaryBuffer)
            {
                return *((ulong*)temporaryBufferStart);
            }
        }

        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public virtual unsafe void Write(ushort i)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            fixed (byte* temporaryBufferStart = temporaryBuffer)
                *((ushort*)temporaryBufferStart) = i;

            Write(temporaryBuffer, 0, sizeof(ushort));
        }

        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public virtual unsafe void Write(uint i)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            fixed (byte* temporaryBufferStart = temporaryBuffer)
                *((uint*)temporaryBufferStart) = i;

            Write(temporaryBuffer, 0, sizeof(uint));
        }

        [Obsolete("Consider using System.Buffers.Binary.BinaryPrimitives or Unsafe.ReadUnaligned.")]
        public virtual unsafe void Write(ulong i)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            fixed (byte* temporaryBufferStart = temporaryBuffer)
                *((ulong*)temporaryBufferStart) = i;

            Write(temporaryBuffer, 0, sizeof(ulong));
        }
    }
}
