// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2013, Milosz Krajewski
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided 
// that the following conditions are met:
// 
// * Redistributions of source code must retain the above copyright notice, this list of conditions 
//   and the following disclaimer.
// 
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
//   and the following disclaimer in the documentation and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED 
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR 
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE 
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN 
// IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#pragma warning disable SA1027 // Tabs must not be used
#pragma warning disable SA1137 // Elements should have the same indentation
using System;
using System.IO;
using System.IO.Compression;
using Stride.Core.IO;

namespace Stride.Core.LZ4
{
	/// <summary>Block compression stream. Allows to use LZ4 for stream compression.</summary>
    public class LZ4Stream : NativeStream
	{
		#region ChunkFlags

		/// <summary>
		/// Flags of a chunk. Please note, this 
		/// </summary>
		[Flags]
		public enum ChunkFlags
		{
			/// <summary>None.</summary>
			None = 0x00,

			/// <summary>Set if chunk is compressed.</summary>
			Compressed = 0x01,

			/// <summary>Set if high compression has been selected (does not affect decoder, 
			/// but might be useful when rewriting)</summary>
			HighCompression = 0x02,

			/// <summary>3 bits for number of passes. Currently only 1 pass (value 0) 
			/// is supported.</summary>
			Passes = 0x04 | 0x08 | 0x10, // not used currently
		}

		#endregion

		#region fields

		/// <summary>The inner stream.</summary>
		private readonly Stream innerStream;

		/// <summary>The compression mode.</summary>
		private readonly CompressionMode compressionMode;

		/// <summary>The high compression flag (compression only).</summary>
		private readonly bool highCompression;

        /// <summary>
        /// Indicate whether the inner stream should be disposed or not.
        /// </summary>
	    private readonly bool disposeInnerStream;

	    /// <summary>The block size (compression only).</summary>
		private readonly int blockSize;

        /// <summary>The buffer.</summary>
        private byte[] dataBuffer;

        /// <summary>The buffer containing the compressed read data</summary>
        private byte[] compressedDataBuffer;

		/// <summary>The buffer length (can be different then _buffer.Length).</summary>
		private int bufferLength;

		/// <summary>The offset in a buffer.</summary>
		private int bufferOffset;

        /// <summary>
        /// The position in the not compressed stream.
        /// </summary>
	    private long position;

        /// <summary>
        /// The position in the inner stream.
        /// </summary>
	    private long innerStreamPosition;

        /// <summary>
        /// The size of the stream after having been uncompressed.
        /// </summary>
	    private readonly long length;

        /// <summary>
        /// The size of the compressed stream.
        /// </summary>
        private readonly long compressedSize;

		#endregion

		#region constructor

	    /// <summary>Initializes a new instance of the <see cref="LZ4Stream" /> class.</summary>
	    /// <param name="innerStream">The inner stream.</param>
	    /// <param name="compressionMode">The compression mode.</param>
	    /// <param name="uncompressedSize">The size of the stream uncompressed</param>
	    /// <param name="highCompression">if set to <c>true</c> [high compression].</param>
	    /// <param name="disposeInnerStream">if set to <c>true</c> <paramref name="innerStream"/> is disposed during called to <see cref="Dispose"/></param>
	    /// <param name="blockSize">Size of the block.</param>
	    public LZ4Stream(
			Stream innerStream,
			CompressionMode compressionMode,
            bool highCompression = false,
            long uncompressedSize = -1,
            long compressedSize = -1,
            bool disposeInnerStream = false,
			int blockSize = 1024 * 1024)
		{
			this.innerStream = innerStream;
			this.compressionMode = compressionMode;
			this.highCompression = highCompression;
		    this.blockSize = Math.Max(16, blockSize);
	        length = uncompressedSize;
	        this.compressedSize = compressedSize;
            this.disposeInnerStream = disposeInnerStream;
        }

		#endregion

		#region utilities

		/// <summary>Returns NotSupportedException.</summary>
		/// <param name="operationName">Name of the operation.</param>
		/// <returns>NotSupportedException</returns>
		private static NotSupportedException NotSupported(string operationName)
		{
			return new NotSupportedException(
				string.Format(
					"Operation '{0}' is not supported", operationName));
		}

		/// <summary>Returns EndOfStreamException.</summary>
		/// <returns>EndOfStreamException</returns>
		private static EndOfStreamException EndOfStream()
		{
			return new EndOfStreamException("Unexpected end of stream");
		}

		/// <summary>Tries to read variable length int.</summary>
		/// <param name="result">The result.</param>
		/// <returns><c>true</c> if integer has been read, <c>false</c> if end of stream has been
		/// encountered. If end of stream has been encoutered in the middle of value 
		/// <see cref="EndOfStreamException"/> is thrown.</returns>
		private bool TryReadVarInt(out ulong result)
		{
			var buffer = new byte[1];
			var count = 0;
			result = 0;

			while (true)
			{
				if ((compressedSize != -1 && innerStreamPosition >= compressedSize) || innerStream.Read(buffer, 0, 1) == 0)
				{
					if (count == 0) return false;
					throw EndOfStream();
				}
			    innerStreamPosition += 1;
				var b = buffer[0];
				result = result + ((ulong)(b & 0x7F) << count);
				count += 7;
				if ((b & 0x80) == 0 || count >= 64) break;
			}

			return true;
		}

		/// <summary>Reads the variable length int. Work with assumption that value is in the stream
		/// and throws exception if it isn't. If you want to check if value is in the stream
		/// use <see cref="TryReadVarInt"/> instead.</summary>
		/// <returns>The value.</returns>
		private ulong ReadVarInt()
		{
			ulong result;
			if (!TryReadVarInt(out result)) throw EndOfStream();
			return result;
		}

		/// <summary>Reads the block of bytes. 
		/// Contrary to <see cref="Stream.Read"/> does not read partial data if possible. 
		/// If there is no data (yet) it waits.</summary>
		/// <param name="buffer">The buffer.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The length.</param>
		/// <returns>Number of bytes read.</returns>
		private int ReadBlock(byte[] buffer, int offset, int count)
		{
			var total = 0;

			while (count > 0)
			{
				var read = innerStream.Read(buffer, offset, count);
				if (read == 0) break;
                innerStreamPosition += read;
				offset += read;
				count -= read;
				total += read;
			}

			return total;
		}

		/// <summary>Writes the variable length integer.</summary>
		/// <param name="value">The value.</param>
		private void WriteVarInt(ulong value)
		{
			var buffer = new byte[1];
			while (true)
			{
				var b = (byte)(value & 0x7F);
				value >>= 7;
				buffer[0] = (byte)(b | (value == 0 ? 0 : 0x80));
                innerStream.Write(buffer, 0, 1);
                innerStreamPosition += 1;
				if (value == 0) break;
			}
		}

		/// <summary>Flushes current chunk.</summary>
		private void FlushCurrentChunk()
		{
			if (bufferOffset <= 0) return;

			var compressed = new byte[bufferOffset];
			var compressedLength = highCompression
				? LZ4Codec.EncodeHC(dataBuffer, 0, bufferOffset, compressed, 0, bufferOffset)
				: LZ4Codec.Encode(dataBuffer, 0, bufferOffset, compressed, 0, bufferOffset);

			if (compressedLength <= 0 || compressedLength >= bufferOffset)
			{
				// uncompressible block
				compressed = dataBuffer;
				compressedLength = bufferOffset;
			}

			var isCompressed = compressedLength < bufferOffset;

			var flags = ChunkFlags.None;

			if (isCompressed) flags |= ChunkFlags.Compressed;
			if (highCompression) flags |= ChunkFlags.HighCompression;

			WriteVarInt((ulong)flags);
			WriteVarInt((ulong)bufferOffset);
			if (isCompressed) WriteVarInt((ulong)compressedLength);

            innerStream.Write(compressed, 0, compressedLength);
            innerStreamPosition += compressedLength;

			bufferOffset = 0;
		}

		/// <summary>Reads the next chunk from stream.</summary>
		/// <returns><c>true</c> if next has been read, or <c>false</c> if it is legitimate end of file.
		/// Throws <see cref="EndOfStreamException"/> if end of stream was unexpected.</returns>
		private bool AcquireNextChunk()
		{
			do
			{
				ulong varint;
				if (!TryReadVarInt(out varint)) return false;
				var flags = (ChunkFlags)varint;
				var isCompressed = (flags & ChunkFlags.Compressed) != 0;

				var originalLength = (int)ReadVarInt();
				var compressedLength = isCompressed ? (int)ReadVarInt() : originalLength;
				if (compressedLength > originalLength) throw EndOfStream(); // corrupted

                if (compressedDataBuffer == null || compressedDataBuffer.Length < compressedLength)
                    compressedDataBuffer = new byte[compressedLength];
                var chunk = ReadBlock(compressedDataBuffer, 0, compressedLength);

				if (chunk != compressedLength) throw EndOfStream(); // currupted

				if (!isCompressed)
				{
                    // swap the buffers
				    var oldDataBuffer = dataBuffer;
                    dataBuffer = compressedDataBuffer; // no compression on this chunk
				    compressedDataBuffer = oldDataBuffer; // ensure that compressedDataBuffer and dataBuffer are different

					bufferLength = compressedLength;
				}
				else
				{
					if (dataBuffer == null || dataBuffer.Length < originalLength)
						dataBuffer = new byte[originalLength];
					var passes = (int)flags >> 2;
					if (passes != 0)
						throw new NotSupportedException("Chunks with multiple passes are not supported.");
					LZ4Codec.Decode(compressedDataBuffer, 0, compressedLength, dataBuffer, 0, originalLength, true);
					bufferLength = originalLength;
				}

				bufferOffset = 0;
			} while (bufferLength == 0); // skip empty block (shouldn't happen but...)

			return true;
		}

		#endregion

		#region overrides

		/// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports reading.</summary>
		/// <returns>true if the stream supports reading; otherwise, false.</returns>
		public override bool CanRead
		{
			get { return compressionMode == CompressionMode.Decompress; }
		}

		/// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports seeking.</summary>
		/// <returns>true if the stream supports seeking; otherwise, false.</returns>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>When overridden in a derived class, gets a value indicating whether the current stream supports writing.</summary>
		/// <returns>true if the stream supports writing; otherwise, false.</returns>
		public override bool CanWrite
		{
			get { return compressionMode == CompressionMode.Compress; }
		}

		/// <summary>When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.</summary>
		public override void Flush()
		{
			if (bufferOffset > 0 && CanWrite) FlushCurrentChunk();
		}

		/// <summary>When overridden in a derived class, gets the length in bytes of the stream.</summary>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		public override long Length
		{
			get { return length; }
		}

		/// <summary>The position in the uncompressed stream.</summary>
		/// <returns>The current position within the stream.</returns>
		public override long Position
		{
			get { return position; }
			set { throw NotSupported("SetPosition"); }
		}

		/// <summary>Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.</summary>
		/// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
		public override int ReadByte()
		{
			if (!CanRead) throw NotSupported("Read");

			if (bufferOffset >= bufferLength && !AcquireNextChunk())
				return -1; // that's just end of stream
            
            position += 1;

			return dataBuffer[bufferOffset++];
		}

		/// <summary>When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		public override unsafe int Read(byte[] buffer, int offset, int count)
		{
			if (!CanRead) throw NotSupported("Read");

			var total = 0;

			while (count > 0)
			{
				var chunk = Math.Min(count, bufferLength - bufferOffset);
				if (chunk > 0)
                {
                    fixed (byte* pSrc = dataBuffer)
                    fixed (byte* pDst = buffer)
                    {
                        Utilities.CopyMemory((IntPtr)pDst + offset, (IntPtr)pSrc + bufferOffset, chunk);
                    }
					bufferOffset += chunk;
					offset += chunk;
					count -= chunk;
					total += chunk;
				}
				else
				{
					if (!AcquireNextChunk()) break;
				}
            }
            position += total;

			return total;
		}

	    /// <inheritdoc/>
	    public unsafe override int Read(IntPtr buffer, int count)
	    {
            if (!CanRead) throw NotSupported("Read");

            var total = 0;

            while (count > 0)
            {
                var chunk = Math.Min(count, bufferLength - bufferOffset);
                if (chunk > 0)
                {
                    fixed (byte* pSrc = dataBuffer)
                    {
                        Utilities.CopyMemory((IntPtr)buffer + total, (IntPtr)pSrc + bufferOffset, chunk);
                    }
                    bufferOffset += chunk;
                    count -= chunk;
                    total += chunk;
                }
                else
                {
                    if (!AcquireNextChunk()) break;
                }
            }
            position += total;

            return total;
        }

	    /// <summary>When overridden in a derived class, sets the position within the current stream.</summary>
		/// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
		/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
		/// <returns>The new position within the current stream.</returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
		    long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = Position + offset;
                    break;
                case SeekOrigin.End:
                    throw NotSupported("Seek");
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }

            if (newPosition == 0)
            {
                innerStream.Seek(-innerStreamPosition, SeekOrigin.Current);
                Reset();
            }
            else if (newPosition == Position)
            {
                // nothing to do
            }
            else
            {
                throw NotSupported("Seek");
            }

		    return Position;
		}

		/// <summary>When overridden in a derived class, sets the length of the current stream.</summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		public override void SetLength(long value)
		{
			throw NotSupported("SetLength");
		}

		/// <summary>Writes a byte to the current position in the stream and advances the position within the stream by one byte.</summary>
		/// <param name="value">The byte to write to the stream.</param>
		public override void WriteByte(byte value)
		{
			if (!CanWrite) throw NotSupported("Write");

		    position += 1;

			if (dataBuffer == null)
			{
				dataBuffer = new byte[blockSize];
				bufferLength = blockSize;
				bufferOffset = 0;
			}

			if (bufferOffset >= bufferLength)
			{
				FlushCurrentChunk();
			}

			dataBuffer[bufferOffset++] = value;
		}

		/// <summary>When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.</summary>
		/// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public override unsafe void Write(byte[] buffer, int offset, int count)
		{
			if (!CanWrite) throw NotSupported("Write");

		    position += count;

			if (dataBuffer == null)
			{
				dataBuffer = new byte[blockSize];
				bufferLength = blockSize;
				bufferOffset = 0;
			}

			while (count > 0)
			{
				var chunk = Math.Min(count, bufferLength - bufferOffset);
				if (chunk > 0)
                {
                    fixed (byte* pSrc = buffer)
                    fixed (byte* pDst = dataBuffer)
                    {
                        Utilities.CopyMemory((IntPtr)pDst + bufferOffset, (IntPtr)pSrc + offset, chunk);
                    }
					offset += chunk;
					count -= chunk;
					bufferOffset += chunk;
				}
				else
				{
					FlushCurrentChunk();
				}
			}
		}

	    /// <inheritdoc/>
        public override unsafe void Write(IntPtr buffer, int count)
        {
            if (!CanWrite) throw NotSupported("Write");

            position += count;
	        int offset = 0;

            if (dataBuffer == null)
            {
                dataBuffer = new byte[blockSize];
                bufferLength = blockSize;
                bufferOffset = 0;
            }

            while (count > 0)
            {
                var chunk = Math.Min(count, bufferLength - bufferOffset);
                if (chunk > 0)
                {
                    fixed (byte* pDst = dataBuffer)
                    {
                        Utilities.CopyMemory((IntPtr)pDst + bufferOffset, (IntPtr)buffer + offset, chunk);
                    }
                    offset += chunk;
                    count -= chunk;
                    bufferOffset += chunk;
                }
                else
                {
                    FlushCurrentChunk();
                }
            }
        }

	    /// <summary>
        /// Reset the stream to its initial position and state
        /// </summary>
        public void Reset()
        {
            position = 0;
            innerStreamPosition = 0;
            dataBuffer = null;
            bufferLength = 0;
            bufferOffset = 0;
        }

		/// <summary>Releases the unmanaged resources used by the <see cref="T:System.IO.Stream" /> and optionally releases the managed resources.</summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			Flush();
            if (disposeInnerStream)
			    innerStream.Dispose();
			base.Dispose(disposing);
		}

		#endregion
	}
}
