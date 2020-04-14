// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.IO;

namespace Xenko.Core.Storage
{
    /// <summary>
    /// Gives access to the object database.
    /// </summary>
    public class ObjectDatabase : IDisposable
    {
        // Loaded Hash => Blob mapping
        private static readonly Dictionary<ObjectId, Blob> LoadedBlobs = new Dictionary<ObjectId, Blob>();

        // When reading, first try backendRead2, then backendRead1.
        // When writing, try backendWrite.
        private readonly IOdbBackend backendRead1;
        private readonly IOdbBackend backendRead2;
        private readonly IOdbBackend backendWrite;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectDatabase" /> class.
        /// </summary>
        /// <param name="vfsMainUrl">The VFS main URL.</param>
        /// <param name="indexName">Name of the index file.</param>
        /// <param name="vfsAdditionalUrl">The VFS additional URL. It will be used only if vfsMainUrl is read-only.</param>
        public ObjectDatabase(string vfsMainUrl, string indexName, string vfsAdditionalUrl = null, bool loadDefaultBundle = true)
        {
            if (vfsMainUrl == null) throw new ArgumentNullException(nameof(vfsMainUrl));

            // Create the merged asset index map
            ContentIndexMap = new ObjectDatabaseContentIndexMap();

            // Try to open file backends
            bool isReadOnly = Platform.Type != PlatformType.Windows;
            var backend = new FileOdbBackend(vfsMainUrl, indexName, isReadOnly);

            ContentIndexMap.Merge(backend.ContentIndexMap);
            if (backend.IsReadOnly)
            {
                backendRead1 = backend;
                if (vfsAdditionalUrl != null)
                {
                    backendWrite = backendRead2 = new FileOdbBackend(vfsAdditionalUrl, indexName, false);
                    ContentIndexMap.Merge(backendWrite.ContentIndexMap);
                }
            }
            else
            {
                backendWrite = backendRead1 = backend;
            }

            ContentIndexMap.WriteableContentIndexMap = backendWrite.ContentIndexMap;

            BundleBackend = new BundleOdbBackend(vfsMainUrl);

            // Try to open "default" pack file synchronously
            if (loadDefaultBundle)
            {
                try
                {
                    BundleBackend.LoadBundle("default", ContentIndexMap).GetAwaiter().GetResult();
                }
                catch (FileNotFoundException)
                {
                }
            }
        }

        public ObjectDatabaseContentIndexMap ContentIndexMap { get; }

        public BundleOdbBackend BundleBackend { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="ObjectDatabase"/> class using default database path, index name, and local database path, and loading default bundle.
        /// </summary>
        /// <returns>A new instance of the <see cref="ObjectDatabase"/> class.</returns>
        public static ObjectDatabase CreateDefaultDatabase()
        {
            return new ObjectDatabase(VirtualFileSystem.ApplicationDatabasePath, VirtualFileSystem.ApplicationDatabaseIndexName, VirtualFileSystem.LocalDatabasePath);
        }

        public void Dispose()
        {
            backendRead1.Dispose();
            if (backendRead2 != null && !ReferenceEquals(backendRead2, backendRead1))
            {
                backendRead2.Dispose();
            }
            if (backendWrite != null && !ReferenceEquals(backendRead2, backendWrite) && !ReferenceEquals(backendRead2, backendRead1))
            {
                backendWrite.Dispose();
            }
        }

        public void CreateBundle(ObjectId[] objectIds, string bundleName, BundleOdbBackend bundleBackend, ISet<ObjectId> disableCompressionIds, Dictionary<string, ObjectId> indexMap, IList<string> dependencies, bool useIncrementalBundle)
        {
            if (bundleBackend == null)
                throw new InvalidOperationException("Can't pack files.");

            if (objectIds.Length == 0)
                return;

            var packUrl = bundleBackend.BundleDirectory + bundleName + BundleOdbBackend.BundleExtension; // we don't want the pack to be compressed in the APK on android

            // Create pack
            BundleOdbBackend.CreateBundle(packUrl, backendRead1, objectIds, disableCompressionIds, indexMap, dependencies, useIncrementalBundle);
        }

        public bool TryGetObjectLocation(ObjectId objectId, out string filePath, out long start, out long end)
        {
            if (BundleBackend != null && BundleBackend.TryGetObjectLocation(objectId, out filePath, out start, out end))
                return true;

            foreach (var backend in new[] { backendRead1, backendRead2 })
            {
                if (backend != null && backend.Exists(objectId))
                {
                    filePath = backend.GetFilePath(objectId);
                    start = 0;
                    end = backend.GetSize(objectId);
                    return true;
                }
            }

            filePath = null;
            start = 0;
            end = 0;
            return false;
        }

        /// <summary>
        /// Loads the specified bundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle.</param>
        /// <returns>Task that will complete when bundle is loaded.</returns>
        public Task LoadBundle(string bundleName)
        {
            return BundleBackend.LoadBundle(bundleName, ContentIndexMap);
        }

        /// <summary>
        /// Loads the specified bundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle.</param>
        public void UnloadBundle(string bundleName)
        {
            BundleBackend.UnloadBundle(bundleName, ContentIndexMap);
        }

        public IEnumerable<ObjectId> EnumerateObjects()
        {
            var result = backendRead1.EnumerateObjects();

            if (BundleBackend != null)
                result = result.Union(BundleBackend.EnumerateObjects());

            if (backendRead2 != null)
                result = result.Union(backendRead2.EnumerateObjects());

            return result;
        }

        public IEnumerable<ObjectId> EnumerateLooseObjects()
        {
            return backendRead1.EnumerateObjects();
        }

        public void Delete(ObjectId objectId)
        {
            if (backendWrite == null)
                throw new InvalidOperationException("Read-only object database.");

            backendWrite.Delete(objectId);
        }

        public bool Exists(ObjectId objectId)
        {
            return (BundleBackend != null && BundleBackend.Exists(objectId)) || backendRead1.Exists(objectId) || (backendRead2 != null && backendRead2.Exists(objectId));
        }

        public int GetSize(ObjectId objectId)
        {
            if (BundleBackend != null && BundleBackend.Exists(objectId))
                return BundleBackend.GetSize(objectId);

            if (backendRead1.Exists(objectId))
                return backendRead1.GetSize(objectId);

            if (backendRead2 == null)
                throw new FileNotFoundException();

            return backendRead2.GetSize(objectId);
        }

        public string GetFilePath(ObjectId objectId)
        {
            if (BundleBackend != null && BundleBackend.Exists(objectId))
                throw new InvalidOperationException();

            if (backendRead1.Exists(objectId))
                return backendRead1.GetFilePath(objectId);

            if (backendRead2 != null && backendRead2.Exists(objectId))
                return backendRead2.GetFilePath(objectId);

            return backendWrite.GetFilePath(objectId);
        }

        /// <summary>
        /// Writes the specified data using the active <see cref="IOdbBackend"/>.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="size">The size.</param>
        /// <param name="forceWrite">Set to true to force writing the datastream even if a content is already stored with the same id. Default is false.</param>
        /// <returns>The <see cref="ObjectId"/> of the given data.</returns>
        public ObjectId Write(IntPtr data, int size, bool forceWrite = false)
        {
            if (backendWrite == null)
                throw new InvalidOperationException("Read-only object database.");

            return backendWrite.Write(ObjectId.Empty, new NativeMemoryStream(data, size), size, forceWrite);
        }

        /// <summary>
        /// Writes the specified data using the active <see cref="IOdbBackend"/>.
        /// </summary>
        /// <param name="stream">The data stream.</param>
        /// <returns>The <see cref="ObjectId"/> of the given data.</returns>
        public ObjectId Write(Stream stream)
        {
            if (backendWrite == null)
                throw new InvalidOperationException("Read-only object database.");

            return backendWrite.Write(ObjectId.Empty, stream, (int)stream.Length);
        }

        /// <summary>
        /// Writes the specified data using the active <see cref="IOdbBackend"/> and a precomputer <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="stream">The data stream.</param>
        /// <param name="objectId">The precomputed objectId.</param>
        /// <param name="forceWrite">Set to true to force writing the datastream even if a content is already stored with the same id. Default is false.</param>
        /// <returns>The <see cref="ObjectId"/> of the given data, which is the same that the passed one.</returns>
        public ObjectId Write(Stream stream, ObjectId objectId, bool forceWrite = false)
        {
            if (backendWrite == null)
                throw new InvalidOperationException("Read-only object database.");

            return backendWrite.Write(objectId, stream, (int)stream.Length, forceWrite);
        }

        /// <summary>
        /// Opens a stream for the specified <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="objectId">The object identifier.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The share.</param>
        /// <returns>A Stream.</returns>
        /// <exception cref="System.InvalidOperationException">Read-only object database.</exception>
        public Stream OpenStream(ObjectId objectId, VirtualFileMode mode = VirtualFileMode.Open, VirtualFileAccess access = VirtualFileAccess.Read, VirtualFileShare share = VirtualFileShare.Read)
        {
            if (access == VirtualFileAccess.Read)
            {
                return OpenStreamForRead(objectId, mode, access, share);
            }

            if (backendWrite == null)
                throw new InvalidOperationException("Read-only object database.");

            if (backendRead1 == backendWrite)
            {
                return backendWrite.OpenStream(objectId, mode, access, share);
            }
            else
            {
                using (var streamRead = OpenStreamForRead(objectId, VirtualFileMode.Open, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite))
                {
                    var stream = backendWrite.OpenStream(objectId, mode, access, share);

                    streamRead?.CopyTo(stream);
                    stream.Position = 0;
                    return stream;
                }
            }
        }

        /// <summary>
        /// Returns a data stream of the data specified <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="objectId">The <see cref="ObjectId"/>.</param>
        /// <param name="checkCache">if set to <c>true</c> [check cache for existing blobs].</param>
        /// <returns>A <see cref="NativeStream"/> of the requested data.</returns>
        public Stream Read(ObjectId objectId, bool checkCache = false)
        {
            if (checkCache)
            {
                lock (LoadedBlobs)
                {
                    // Check if there is already an in-memory blob that we can use.
                    Blob blob;
                    if (LoadedBlobs.TryGetValue(objectId, out blob))
                    {
                        return new BlobStream(blob);
                    }
                }
            }

            return OpenStream(objectId);
        }

        /// <summary>
        /// Creates a stream that can then be saved directly in the database using <see cref="SaveStream"/>.
        /// </summary>
        /// <returns>a stream writer that should be passed to <see cref="SaveStream"/> in order to be stored in the database</returns>
        public OdbStreamWriter CreateStream()
        {
            return backendWrite.CreateStream();
        }

        /// <summary>
        /// Creates a in-memory binary blob as a <see cref="Blob"/> that will also be stored using the active <see cref="IOdbBackend"/>.
        /// Even if <see cref="Blob"/> is new (not in the ODB), memory will be copied.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="size">The size.</param>
        /// <returns>The <see cref="Blob"/> containing given data, with its reference count incremented.</returns>
        public Blob CreateBlob(IntPtr data, int size)
        {
            // Generate hash
            ObjectId objectId;
            var nativeMemoryStream = new NativeMemoryStream(data, size);

            using (var digestStream = new DigestStream(Stream.Null))
            {
                nativeMemoryStream.CopyTo(digestStream);
                objectId = digestStream.CurrentHash;
            }

            lock (LoadedBlobs)
            {
                var blob = Lookup(objectId);
                
                // Blob doesn't exist yet, so let's create it and save it to ODB.
                if (blob == null)
                {
                    // Let's go back to beginning of stream after previous hash
                    nativeMemoryStream.Position = 0;

                    // Create blob
                    blob = new Blob(this, objectId, data, size);
                    blob.AddReference();

                    // Write to disk
                    backendWrite.Write(objectId, nativeMemoryStream, size, false);

                    // Add blob to cache
                    LoadedBlobs.Add(objectId, blob);
                }

                return blob;
            }
        }

        /// <summary>
        /// Lookups the <see cref="Blob"/> with the specified <see cref="ObjectId"/>.
        /// Any object returned will have its reference count incremented.
        /// </summary>
        /// <param name="objectId">The object id.</param>
        /// <returns>The <see cref="Blob"/> matching this <see cref="ObjectId"/> with an incremented reference count if it exists; [null] otherwise.</returns>
        public Blob Lookup(ObjectId objectId)
        {
            Blob blob;
            lock (LoadedBlobs)
            {
                if (!LoadedBlobs.TryGetValue(objectId, out blob))
                {
                    if (!Exists(objectId))
                        return null;

                    // Load blob if not cached
                    var stream = OpenStream(objectId).ToNativeStream();

                    // Create blob and add to cache
                    blob = new Blob(this, objectId, stream);
                    LoadedBlobs.Add(objectId, blob);

                    // Dispose the previously opened stream.
                    stream.Dispose();
                }

                // Lookup adds a reference
                blob.AddReference();
            }

            return blob;
        }

        internal void DestroyBlob(Blob blob)
        {
            // Remove blob from cache when destroyed
            lock (LoadedBlobs)
            {
                if (!LoadedBlobs.Remove(blob.ObjectId))
                    throw new InvalidOperationException("Destroying a blob not created through ObjectDatabase.CreateBlob.");
            }
        }

        private Stream OpenStreamForRead(ObjectId objectId, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share)
        {
            if (BundleBackend != null && BundleBackend.Exists(objectId))
                return BundleBackend.OpenStream(objectId, mode, access, share);

            if (backendRead1.Exists(objectId))
                return backendRead1.OpenStream(objectId, mode, access, share);

            if (backendRead2 != null && backendRead2.Exists(objectId))
                return backendRead2.OpenStream(objectId, mode, access, share);

            throw new FileNotFoundException();
        }
    }
}
