// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

namespace Xenko.Core.IO
{
    /// <summary>
    /// A <see cref="MemoryStream"/> over a native memory region.
    /// </summary>
    public unsafe class NativeMemoryStream : NativeStream
    {
        private readonly byte* dataStart;
        private readonly byte* dataEnd;
        private byte* dataCurrent;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMemoryStream"/> class.
        /// </summary>
        /// <param name="data">The native data pointer.</param>
        /// <param name="length">The data length.</param>
        public NativeMemoryStream(IntPtr data, long length)
            : this((byte*)data, length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeMemoryStream"/> class.
        /// </summary>
        /// <param name="data">The native data pointer.</param>
        /// <param name="length">The data length.</param>
        public NativeMemoryStream(byte* data, long length)
        {
            this.dataStart = data;
            this.dataCurrent = data;
            this.dataEnd = data + length;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    dataCurrent = dataStart + offset;
                    break;
                case SeekOrigin.Current:
                    dataCurrent += offset;
                    break;
                case SeekOrigin.End:
                    dataCurrent = dataEnd + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
            return dataCurrent - dataStart;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("Length can't be changed.");
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                count = bytesLeft;
            Utilities.Read((IntPtr)dataCurrent, buffer, offset, count);
            dataCurrent += count;
            return count;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                throw new InvalidOperationException("Buffer too small");

            Utilities.Write((IntPtr)dataCurrent, buffer, offset, count);
            dataCurrent += count;
        }

        /// <inheritdoc/>
        public override int Read(IntPtr buffer, int count)
        {
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                count = bytesLeft;

            Utilities.CopyMemory(buffer, (IntPtr)dataCurrent, count);
            dataCurrent += count;
            return count;
        }

        /// <inheritdoc/>
        public override void Write(IntPtr buffer, int count)
        {
            var bytesLeft = (int)(dataEnd - dataCurrent);
            if (count > bytesLeft)
                throw new InvalidOperationException("Buffer too small");

            Utilities.CopyMemory((IntPtr)dataCurrent, buffer, count);
            dataCurrent += count;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            if (dataCurrent == dataEnd)
                return -1;

            return *dataCurrent++;
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            if (dataCurrent == dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *dataCurrent++ = value;
        }

        /// <inheritdoc/>
        public override ushort ReadUInt16()
        {
            if (dataCurrent + sizeof(ushort) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            var result = *((ushort*)dataCurrent);
            dataCurrent += sizeof(ushort);
            return result;
        }

        public override uint ReadUInt32()
        {
            if (dataCurrent + sizeof(uint) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            var result = *((uint*)dataCurrent);
            dataCurrent += sizeof(uint);
            return result;
        }

        /// <inheritdoc/>
        public override ulong ReadUInt64()
        {
            if (dataCurrent + sizeof(ulong) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            var result = *((ulong*)dataCurrent);
            dataCurrent += sizeof(ulong);
            return result;
        }

        /// <inheritdoc/>
        public override void Write(ushort i)
        {
            if (dataCurrent + sizeof(ushort) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *((ushort*)dataCurrent) = i;
            dataCurrent += sizeof(ushort);
        }

        /// <inheritdoc/>
        public override void Write(uint i)
        {
            if (dataCurrent + sizeof(uint) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *((uint*)dataCurrent) = i;
            dataCurrent += sizeof(uint);
        }

        /// <inheritdoc/>
        public override void Write(ulong i)
        {
            if (dataCurrent + sizeof(ulong) > dataEnd)
                throw new InvalidOperationException("Buffer too small");

            *((ulong*)dataCurrent) = i;
            dataCurrent += sizeof(ulong);
        }

        /// <inheritdoc/>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override long Length
        {
            get { return dataEnd - dataStart; }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get { return dataCurrent - dataStart; }
            set { dataCurrent = dataStart + value; }
        }
    }
}
