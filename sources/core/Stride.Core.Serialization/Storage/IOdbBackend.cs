// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Storage
{
    /// <summary>
    /// Base class for custom object database backends (ODB).
    /// </summary>
    public interface IOdbBackend : IDisposable
    {
        /// <summary>
        /// Gets the asset index map.
        /// </summary>
        /// <value>
        /// The asset index map.
        /// </value>
        IContentIndexMap ContentIndexMap { get; }

        /// <summary>
        /// Opens a <see cref="NativeStream" /> of the object with the specified <see cref="ObjectId" />.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId" />.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The process share mode.</param>
        /// <returns>
        /// A <see cref="NativeStream" /> opened from the specified <see cref="ObjectId" />.
        /// </returns>
        Stream OpenStream(ObjectId objectId, VirtualFileMode mode = VirtualFileMode.Open, VirtualFileAccess access = VirtualFileAccess.Read, VirtualFileShare share = VirtualFileShare.Read);

        /// <summary>
        /// Requests that this backend read an object's length (but not its contents).
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/>.</param>
        /// <returns>The object size.</returns>
        int GetSize(ObjectId objectId);

        /// <summary>
        /// Writes an object to the backing store.
        /// The backend may need to compute the object ID and return it to the caller.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> if already computed, or <see cref="ObjectId.Empty"/> if not determined yet.</param>
        /// <param name="dataStream">The data stream.</param>
        /// <param name="length">The data length.</param>
        /// <param name="forceWrite">Set to true to force writing the datastream even if a content is already stored with the same id. Default is false.</param>
        /// <returns>The generated <see cref="ObjectId"/>.</returns>
        ObjectId Write(ObjectId objectId, Stream dataStream, int length, bool forceWrite = false);

        /// <summary>
        /// Creates a stream that will be saved to database when closed and/or disposed.
        /// </summary>
        /// <returns>a stream writer that should be passed to <see cref="SaveStream"/> in order to be stored in the database</returns>
        OdbStreamWriter CreateStream();

        /// <summary>
        /// Determines weither the object with the specified <see cref="ObjectId"/> exists.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/> to check existence for.</param>
        /// <returns><c>true</c> if an object with the specified <see cref="ObjectId"/> exists; otherwise, <c>false</c>.</returns>
        bool Exists(ObjectId objectId);

        /// <summary>
        /// Enumerates the object stored in this backend.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ObjectId> EnumerateObjects();

        /// <summary>
        /// Deletes the specified <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        void Delete(ObjectId objectId);

        /// <summary>
        /// Returns the file path corresponding to the given id (in the VFS domain), if appliable.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/>.</param>
        /// <returns>The file path.</returns>
        string GetFilePath(ObjectId objectId);
    }
}
