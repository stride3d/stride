// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Stride.Core.IO;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// A multithreaded wrapper over a Stream, used by the VirtualFileSystem.
    /// It also allows restricted access to subparts of the Stream (useful for serialization and data streaming).
    /// </summary>
    public class VirtualFileStream : NativeStream
    {
        public Stream InternalStream { get; internal protected set; }
        protected VirtualFileStream virtualFileStream;
        protected readonly long startPosition;
        protected readonly long endPosition;
        private readonly bool disposeInternalStream;

        public long StartPosition
        {
            get { return startPosition; }
        }

        public long EndPosition
        {
            get { return endPosition; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualFileStream" /> class.
        /// </summary>
        /// <param name="internalStream">The internal stream.</param>
        /// <param name="startPosition">The start position.</param>
        /// <param name="endPosition">The end position.</param>
        /// <param name="disposeInternalStream">if set to <c>true</c> this instance has ownership of the internal stream and will dispose it].</param>
        /// <exception cref="System.ArgumentException">startPosition and endPosition doesn't fit inside current bounds</exception>
        /// <exception cref="System.NotSupportedException">Attempted to create a VirtualFileStream from a Stream which doesn't support seeking.</exception>
        public VirtualFileStream(Stream internalStream, long startPosition = 0, long endPosition = -1, bool disposeInternalStream = true, bool seekToBeginning = true)
        {
            this.disposeInternalStream = disposeInternalStream;

            if (internalStream is VirtualFileStream)
            {
                virtualFileStream = (VirtualFileStream)internalStream;
                internalStream = virtualFileStream.InternalStream;
                startPosition += virtualFileStream.startPosition;
                if (endPosition == -1)
                    endPosition = virtualFileStream.endPosition;
                else
                    endPosition += virtualFileStream.startPosition;
                if (startPosition < virtualFileStream.startPosition || ((endPosition < startPosition || endPosition > virtualFileStream.endPosition) && virtualFileStream.endPosition != -1))
                    throw new ArgumentException("startPosition and endPosition doesn't fit inside current bounds");

                if (!virtualFileStream.disposeInternalStream)
                    this.disposeInternalStream = false;
            }

            InternalStream = internalStream;
            this.startPosition = startPosition;
            this.endPosition = endPosition;

            if (seekToBeginning)
                InternalStream.Seek(startPosition, SeekOrigin.Begin);
        }

        protected override void Dispose(bool disposing)
        {
            if (virtualFileStream != null)
                virtualFileStream.Dispose();
            virtualFileStream = null;

            if (disposeInternalStream && InternalStream != null)
            {
                InternalStream.Dispose();   
            }

            InternalStream = null;
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        public override bool CanRead
        {
            get { return InternalStream.CanRead; }
        }

        /// <inheritdoc/>
        public override bool CanSeek
        {
            get { return InternalStream.CanSeek; }
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get { return InternalStream.CanWrite; }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            InternalStream.Flush();
        }

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                if (endPosition == -1) // Use underlying stream if not a substream
                    return InternalStream.Length - startPosition;
                return endPosition - startPosition;
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get { return InternalStream.Position - startPosition; }
            set { InternalStream.Position = startPosition + value; }
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (endPosition != -1)
            {
                var maxCount = (int)(endPosition - InternalStream.Position);
                if (count > maxCount)
                    count = maxCount;
            }

            var bytesProcessed = InternalStream.Read(buffer, offset, count);
            return bytesProcessed;
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            if (endPosition != -1 && InternalStream.Position >= endPosition)
            {
                return -1;
            }

            return InternalStream.ReadByte();
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    InternalStream.Seek(startPosition + offset, SeekOrigin.Begin);
                    break;
                case SeekOrigin.Current:
                    InternalStream.Seek(offset, SeekOrigin.Current);
                    break;
                case SeekOrigin.End:
                    // Maybe we don't know the actual file size (full file)
                    if (endPosition == -1)
                    {
                        InternalStream.Seek(offset, SeekOrigin.End);
                    }
                    else
                    {
                        InternalStream.Seek(endPosition - startPosition + offset, SeekOrigin.Begin);
                    }
                    break;
            }

            var newPosition = InternalStream.Position;
            if (newPosition < startPosition || (endPosition != -1 && newPosition > endPosition))
            {
                InternalStream.Position = startPosition;
                throw new InvalidOperationException("Seek position is out of bound.");
            }

            return newPosition;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            if (endPosition != -1)
            {
                throw new InvalidOperationException("Can't resize VirtualFileStream if endPosition is not -1.");
            }

            InternalStream.SetLength(value);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (endPosition != -1 && count > endPosition - InternalStream.Position)
            {
                throw new NotSupportedException("Can't write beyond end of stream.");
            }

            InternalStream.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            if (endPosition != -1 && InternalStream.Position >= endPosition)
            {
                throw new NotSupportedException("Can't write beyond end of stream.");
            }

            InternalStream.WriteByte(value);
        }
    }
}
