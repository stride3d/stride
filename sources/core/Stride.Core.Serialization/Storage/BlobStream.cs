// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.IO;

namespace Xenko.Core.Storage
{
    /// <summary>
    /// A read-only <see cref="NativeMemoryStream"/> that will properly keep references on its underlying <see cref="Blob"/>.
    /// </summary>
    internal class BlobStream : NativeMemoryStream
    {
        private Blob blob;

        public BlobStream(Blob blob) : base(blob.Content, blob.Size)
        {
            this.blob = blob;

            // Keep a reference on the blob while its data is used.
            this.blob.AddReference();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Release reference on the blob
            blob.Release();
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(IntPtr buffer, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }
    }
}
