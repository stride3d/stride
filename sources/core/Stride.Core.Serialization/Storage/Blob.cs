// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Core.Storage;

/// <summary>
/// Stores immutable binary content.
/// </summary>
public class Blob : ReferenceBase
{
    protected Blob(ObjectDatabase objectDatabase, ObjectId objectId)
    {
        this.ObjectDatabase = objectDatabase;
        this.ObjectId = objectId;
    }

    internal unsafe Blob(ObjectDatabase objectDatabase, ObjectId objectId, IntPtr content, int size)
        : this(objectDatabase, objectId)
    {
        Debug.Assert(size >= 0);
        this.Size = size;
        this.Content = Marshal.AllocHGlobal(size);
        MemoryUtilities.CopyWithAlignmentFallback((void*)this.Content, (void*)content, (uint)size);
    }

    internal unsafe Blob(ObjectDatabase objectDatabase, ObjectId objectId, Stream stream)
        : this(objectDatabase, objectId)
    {
        Size = (int)stream.Length;
        Content = Marshal.AllocHGlobal(Size);
        stream.Read(new Span<byte>((void*)Content, Size));
    }

    /// <summary>
    /// Gets the size.
    /// </summary>
    /// <value>
    /// The size.
    /// </value>
    public int Size { get; }

    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <value>
    /// The content.
    /// </value>
    public IntPtr Content { get; }

    /// <summary>
    /// Gets the <see cref="ObjectId"/>.
    /// </summary>
    /// <value>
    /// The <see cref="ObjectId"/>.
    /// </value>
    public ObjectId ObjectId { get; }

    internal ObjectDatabase ObjectDatabase { get; }

    /// <summary>
    /// Gets a <see cref="Stream"/> over the <see cref="Content"/>.
    /// </summary>
    /// It will keeps a reference to the <see cref="Blob"/> until disposed.
    /// <returns>A <see cref="Stream"/> over the <see cref="Content"/>.</returns>
    public Stream GetContentStream() => new BlobStream(this);

    /// <inheritdoc/>
    protected override void Destroy()
    {
        ObjectDatabase.DestroyBlob(this);
        Marshal.FreeHGlobal(this.Content);
    }
}
