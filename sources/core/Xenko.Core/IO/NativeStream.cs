// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

namespace Xenko.Core.IO
{
    /// <summary>
    /// A <see cref="Stream"/> with additional methods for native read and write operations using <see cref="IntPtr"/>.
    /// </summary>
    public abstract class NativeStream : Stream
    {
        protected const int NativeStreamBufferSize = 1024;

        // Helper buffer for classes needing it.
        // If null, it should be initialized with NativeStreamBufferSize constant.
        protected byte[] nativeStreamBuffer;

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

        public virtual unsafe void Write(ushort i)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            fixed (byte* temporaryBufferStart = temporaryBuffer)
                *((ushort*)temporaryBufferStart) = i;

            Write(temporaryBuffer, 0, sizeof(ushort));
        }
        
        public virtual unsafe void Write(uint i)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            fixed (byte* temporaryBufferStart = temporaryBuffer)
                *((uint*)temporaryBufferStart) = i;

            Write(temporaryBuffer, 0, sizeof(uint));
        }

        public virtual unsafe void Write(ulong i)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            fixed (byte* temporaryBufferStart = temporaryBuffer)
                *((ulong*)temporaryBufferStart) = i;

            Write(temporaryBuffer, 0, sizeof(ulong));
        }

        /// <summary>
        /// Reads a block of bytes from the stream and writes the data in a given buffer.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified buffer with the values between 0 and (count - 1) replaced by the bytes read from the current source. </param>
        /// <param name="count">The maximum number of bytes to read. </param>
        /// <exception cref="ArgumentNullException">array is null. </exception>
        /// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
        public virtual int Read(IntPtr buffer, int count)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            var readSize = 0;

            for (var offset = 0; offset < count; offset += NativeStreamBufferSize, buffer += NativeStreamBufferSize)
            {
                // Compute missing bytes in this block
                var blockSize = count - offset;
                if (blockSize > NativeStreamBufferSize)
                    blockSize = NativeStreamBufferSize;

                var currentReadSize = Read(temporaryBuffer, 0, blockSize);
                readSize += currentReadSize;
                Utilities.Write(buffer, temporaryBuffer, 0, currentReadSize);

                // Reached end of stream?
                if (currentReadSize < blockSize)
                    break;
            }

            return readSize;
        }

        /// <summary>
        /// Writes a block of bytes to this stream using data from a buffer.
        /// </summary>
        /// <param name="buffer">The buffer containing data to write to the stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream. </param>
        public virtual unsafe void Write(IntPtr buffer, int count)
        {
            var temporaryBuffer = nativeStreamBuffer;
            if (temporaryBuffer == null)
                temporaryBuffer = nativeStreamBuffer = new byte[NativeStreamBufferSize];

            for (var offset = 0; offset < count; offset += NativeStreamBufferSize, buffer += NativeStreamBufferSize)
            {
                // Compute missing bytes in this block
                var blockSize = count - offset;
                if (blockSize > NativeStreamBufferSize)
                    blockSize = NativeStreamBufferSize;

                fixed (byte* temporaryBufferStart = temporaryBuffer)
                    Utilities.CopyMemory((IntPtr)temporaryBufferStart, buffer, blockSize);

                Write(temporaryBuffer, 0, blockSize);
            }
        }
    }
}
