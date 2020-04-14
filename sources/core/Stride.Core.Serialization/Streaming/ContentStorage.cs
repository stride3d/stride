// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Streaming
{
    /// <summary>
    /// Streamable resources content storage containter.
    /// </summary>
    [DebuggerDisplay("Content Storage: {Url}; Loaded chunks: {LoadedChunksCount}/{ChunksCount}")]
    public class ContentStorage : DisposeBase
    {
        private ContentChunk[] chunks;
        private long locks;
        private readonly object storageLock = new object();

        /// <summary>
        /// The content streaming service which manages this storage container.
        /// </summary>
        public ContentStreamingService Service { get; }

        /// <summary>
        /// Gets the storage URL path.
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        /// Gets the time when container has been created (in UTC).
        /// </summary>
        public DateTime PackageTime { get; private set; }

        /// <summary>
        /// Gets the last access time.
        /// </summary>
        public DateTime LastAccessTime
        {
            get
            {
                var result = chunks[0].LastAccessTime;
                for (int i = 1; i < chunks.Length; i++)
                {
                    if (result < chunks[i].LastAccessTime)
                        result = chunks[i].LastAccessTime;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the amount of chunks located inside the storage container.
        /// </summary>
        public int ChunksCount => chunks.Length;

        /// <summary>
        /// Gets the amount of loaded chunks.
        /// </summary>
        public int LoadedChunksCount => chunks.Count(x => x.IsLoaded);

        internal ContentStorage(ContentStreamingService service)
        {
            Service = service;
        }

        internal void Init(ref ContentStorageHeader header)
        {
            Url = header.DataUrl;
            chunks = new ContentChunk[header.ChunksCount];
            for (int i = 0; i < chunks.Length; i++)
            {
                var e = header.Chunks[i];
                chunks[i] = new ContentChunk(this, e.Location, e.Size);
            }
            PackageTime = header.PackageTime;

            // Validate hash code
            if (GetHashCode() != header.HashCode)
                throw new ContentStreamingException("Invalid hash code.", this);
        }

        /// <summary>
        /// Gets the chunk.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>Chunk</returns>
        public ContentChunk GetChunk(int index)
        {
            var chunk = chunks[index];
            chunk.RegisterUsage();
            return chunk;
        }

        internal void ReleaseUnusedChunks()
        {
            if (Interlocked.Read(ref locks) != 0)
                return;

            lock (storageLock)
            {
                var now = DateTime.UtcNow;
                foreach (var chunk in chunks)
                {
                    if (chunk.IsLoaded && now - chunk.LastAccessTime >= Service.UnusedDataChunksLifetime)
                        chunk.Unload();
                }
            }
        }

        internal void ReleaseChunks()
        {
            if (Interlocked.Read(ref locks) != 0)
                return;

            lock (storageLock)
                chunks.ForEach(x => x.Unload());
        }

        /// <summary>
        /// Locks the chunks.
        /// </summary>
        public void LockChunks()
        {
            Interlocked.Increment(ref locks);
            Monitor.Enter(storageLock);
        }

        /// <summary>
        /// Unlocks the chunks.
        /// </summary>
        public void UnlockChunks()
        {
            Monitor.Exit(storageLock);
            Interlocked.Decrement(ref locks);
        }

        /// <summary>
        /// Creates the new storage container at the specified location and generates header for that.
        /// </summary>
        /// <param name="contentManager">The content manager.</param>
        /// <param name="dataUrl">The file url.</param>
        /// <param name="chunksData">The chunks data.</param>
        /// <param name="header">The header data.</param>
        public static void Create(ContentManager contentManager, string dataUrl, List<byte[]> chunksData, out ContentStorageHeader header)
        {
            if (chunksData == null || chunksData.Count == 0 || chunksData.Any(x => x == null || x.Length == 0))
                throw new ArgumentException(nameof(chunksData));

            var packageTime = DateTime.UtcNow;

            // Sort chunks (smaller ones go first)
            int chunksCount = chunksData.Count;
            List<int> chunksOrder = new List<int>(chunksCount);
            for (int i = 0; i < chunksCount; i++)
                chunksOrder.Add(i);
            chunksOrder.Sort((a, b) => chunksData[a].Length - chunksData[b].Length);

            // Calculate header hash code (used to provide simple data verification during loading)
            // Note: this must match ContentStorage.GetHashCode()
            int hashCode = (int)packageTime.Ticks;
            hashCode = (hashCode * 397) ^ chunksCount;
            for (int i = 0; i < chunksCount; i++)
                hashCode = (hashCode * 397) ^ chunksData[i].Length;

            // Create header
            header = new ContentStorageHeader
            {
                DataUrl = dataUrl,
                PackageTime = packageTime,
                HashCode = hashCode,
                Chunks = new ContentStorageHeader.ChunkEntry[chunksCount],
            };

            // Calculate chunks locations in the file
            int offset = 0;
            for (int i = 0; i < chunksCount; i++)
            {
                int chunkIndex = chunksOrder[i];
                int size = chunksData[chunkIndex].Length;
                header.Chunks[chunkIndex].Location = offset;
                header.Chunks[chunkIndex].Size = size;
                offset += size;
            }

            // Create file with a raw data
            using (var outputStream = contentManager.FileProvider.OpenStream(dataUrl, VirtualFileMode.Create, VirtualFileAccess.Write, VirtualFileShare.Read, StreamFlags.Seekable))
            using (var stream = new BinaryWriter(outputStream))
            {
                // Write data (one after another)
                for (int i = 0; i < chunksCount; i++)
                    stream.Write(chunksData[chunksOrder[i]]);

                // Validate calculated offset
                if (offset != outputStream.Position)
                    throw new ContentStreamingException("Invalid storage offset.");
            }
        }

        /// <inheritdoc/>
        public sealed override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)PackageTime.Ticks;
                hashCode = (hashCode * 397) ^ chunks.Length;
                for (int i = 0; i < chunks.Length; i++)
                    hashCode = (hashCode * 397) ^ chunks[i].Size;
                return hashCode;
            }
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            Service.UnregisterStorage(this);
            ReleaseChunks();

            base.Destroy();
        }
    }
}
