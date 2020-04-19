// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Storage
{
    public abstract class OdbStreamWriter : Stream
    {
        protected readonly Stream stream;
        private readonly long initialPosition;

        public Action<OdbStreamWriter> Disposed;

        protected OdbStreamWriter(Stream stream, string temporaryName)
        {
            this.stream = stream;
            initialPosition = stream.Position;
            TemporaryName = temporaryName;
        }

        public string TemporaryName;

        public abstract ObjectId CurrentHash { get; }

        public override bool CanRead { get { return false; } }

        public override bool CanSeek { get { return true; } }

        public override bool CanWrite { get { return stream.CanWrite; } }

        public override void Flush()
        {
            stream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            // Force hash computation before stream is closed.
            var hash = CurrentHash;
            stream.Dispose();

            Disposed?.Invoke(this);
        }

        public override long Length
        {
            get
            {
                return stream.Length - initialPosition;
            }
        }

        public override long Position
        {
            get
            {
                return stream.Position - initialPosition;
            }
            set
            {
                stream.Position = initialPosition + value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }
    }
}
