// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.IO;

namespace Stride.Core.Storage
{
    /// <summary>
    /// Stores immutable binary content.
    /// </summary>
    public class Blob : ReferenceBase
    {
        private readonly ObjectDatabase objectDatabase;
        private readonly IntPtr content;
        private readonly int size;
        private readonly ObjectId objectId;

        protected Blob(ObjectDatabase objectDatabase, ObjectId objectId)
        {
            this.objectDatabase = objectDatabase;
            this.objectId = objectId;
        }

        internal unsafe Blob(ObjectDatabase objectDatabase, ObjectId objectId, IntPtr content, int size)
            : this(objectDatabase, objectId)
        {
            Debug.Assert(size >= 0);
            this.size = size;
            this.content = Marshal.AllocHGlobal(size);
            Unsafe.CopyBlockUnaligned((void*)this.content, (void*)content, (uint)size);
        }

        internal unsafe Blob(ObjectDatabase objectDatabase, ObjectId objectId, Stream stream)
            : this(objectDatabase, objectId)
        {
            size = (int)stream.Length;
            content = Marshal.AllocHGlobal(size);
            stream.Read(new Span<byte>((void*)content, size));
        }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public int Size
        {
            get { return size; }
        }

        /// <summary>
        /// Gets the content.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public IntPtr Content
        {
            get { return content; }
        }

        /// <summary>
        /// Gets the <see cref="ObjectId"/>.
        /// </summary>
        /// <value>
        /// The <see cref="ObjectId"/>.
        /// </value>
        public ObjectId ObjectId
        {
            get { return objectId; }
        }

        internal ObjectDatabase ObjectDatabase
        {
            get { return objectDatabase; }
        }

        /// <summary>
        /// Gets a <see cref="Stream"/> over the <see cref="Content"/>.
        /// </summary>
        /// It will keeps a reference to the <see cref="Blob"/> until disposed.
        /// <returns>A <see cref="Stream"/> over the <see cref="Content"/>.</returns>
        public Stream GetContentStream() => new BlobStream(this);

        /// <inheritdoc/>
        protected override void Destroy()
        {
            objectDatabase.DestroyBlob(this);
            Marshal.FreeHGlobal(this.content);
        }
    }
}
