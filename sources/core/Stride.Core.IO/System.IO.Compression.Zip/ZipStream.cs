// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZipStream.cs" company="Matthew Leibowitz">
//   Copyright (c) Matthew Leibowitz
//   This code is licensed under the Apache 2.0 License
//   http://www.apache.org/licenses/LICENSE-2.0.html
// </copyright>
// <summary>
//   The zip stream.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace System.IO.Compression.Zip
{
    /// <summary>
    /// The zip stream.
    /// </summary>
    public class ZipStream : Stream
    {
        #region Fields

        /// <summary>
        /// The inner stream.
        /// </summary>
        private readonly Stream innerStream;

        /// <summary>
        /// The zip file entry.
        /// </summary>
        private readonly ZipFileEntry zipFileEntry;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipStream"/> class.
        /// </summary>
        /// <param name="innerStream">
        /// The inner stream.
        /// </param>
        /// <param name="zipFileEntry">
        /// The zip file entry.
        /// </param>
        public ZipStream(Stream innerStream, ZipFileEntry zipFileEntry)
        {
            this.innerStream = innerStream;
            this.zipFileEntry = zipFileEntry;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets a value indicating whether CanRead.
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return this.innerStream.CanRead;
            }
        }

        /// <summary>
        /// Gets a value indicating whether CanSeek.
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return this.innerStream.CanSeek;
            }
        }

        /// <summary>
        /// Gets a value indicating whether CanWrite.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets Length.
        /// </summary>
        public override long Length
        {
            get
            {
                return this.zipFileEntry.FileSize;
            }
        }

        /// <summary>
        /// Gets or sets Position.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.innerStream.Position - this.zipFileEntry.FileOffset;
            }

            set
            {
                this.innerStream.Position = this.zipFileEntry.FileOffset + value;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The close.
        /// </summary>
        public override void Close()
        {
            base.Close();

            if (this.zipFileEntry.Method == Compression.Deflate)
            {
                this.innerStream.Dispose();
            }
        }

        /// <summary>
        /// The flush.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public override void Flush()
        {
            throw new InvalidOperationException("You cannot modify this stream.");
        }

        /// <summary>
        /// The read.
        /// </summary>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <param name="offset">
        /// The offset.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        /// <returns>
        /// The read.
        /// </returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// The seek.
        /// </summary>
        /// <param name="offset">
        /// The offset.
        /// </param>
        /// <param name="origin">
        /// The origin.
        /// </param>
        /// <returns>
        /// The seek.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// </exception>
        /// <exception cref="EndOfStreamException">
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long localOffset = -1;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    localOffset = this.zipFileEntry.FileOffset + offset;
                    break;
                case SeekOrigin.Current:
                    break;
                case SeekOrigin.End:
                    localOffset = this.zipFileEntry.FileOffset + this.zipFileEntry.FileSize + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }

            if (localOffset > this.zipFileEntry.FileSize || localOffset < 0)
            {
                throw new EndOfStreamException();
            }

            return this.innerStream.Seek(localOffset, origin) - this.zipFileEntry.FileOffset;
        }

        /// <summary>
        /// The set length.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public override void SetLength(long value)
        {
            throw new InvalidOperationException("You cannot modify this stream.");
        }

        /// <summary>
        /// The write.
        /// </summary>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <param name="offset">
        /// The offset.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("You cannot modify this stream.");
        }

        #endregion
    }
}
