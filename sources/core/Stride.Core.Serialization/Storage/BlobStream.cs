// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Stride.Core.IO;

namespace Stride.Core.Storage
{
    /// <summary>
    /// A read-only <see cref="NativeMemoryStream"/> that will properly keep references on its underlying <see cref="Blob"/>.
    /// </summary>
    internal class BlobStream : UnmanagedMemoryStream
    {
        private readonly Blob blob;

        public unsafe BlobStream(Blob blob)
            : base((byte*)blob.Content, blob.Size, capacity: blob.Size, access: FileAccess.Read)
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
        public override void WriteByte(byte value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();


        /// <inheritdoc/>
        public override bool CanWrite => false;
    }
}
