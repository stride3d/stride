// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Xenko.Core.Serialization;

namespace Xenko.Core.Streaming
{
    /// <summary>
    /// Header with description of streamable resource data storage.
    /// </summary>
    public struct ContentStorageHeader
    {
        /// <summary>
        /// Describies single data chunk storage information.
        /// </summary>
        public struct ChunkEntry
        {
            /// <summary>
            /// The location (adress in file).
            /// </summary>
            public int Location;

            /// <summary>
            /// The size in bytes.
            /// </summary>
            public int Size;
        }

        /// <summary>
        /// True if data is followed by initial low resolution image.
        /// </summary>
        public bool InitialImage;

        /// <summary>
        /// The data container url.
        /// </summary>
        public string DataUrl;

        /// <summary>
        /// Time when package has been created (in UTC).
        /// </summary>
        public DateTime PackageTime;

        /// <summary>
        /// The hash code for the package header. Used to ensure data consistency.
        /// </summary>
        public int HashCode;

        /// <summary>
        /// The data chunks.
        /// </summary>
        public ChunkEntry[] Chunks;

        /// <summary>
        /// Gets the amount of data chunks.
        /// </summary>
        public int ChunksCount => Chunks.Length;

        /// <summary>
        /// Writes this instance to a stream.
        /// </summary>
        /// <param name="stream">The destination stream.</param>
        public void Write(SerializationStream stream)
        {
            stream.Write(1);
            stream.Write(InitialImage);
            stream.Write(DataUrl);
            stream.Write(PackageTime.Ticks);
            stream.Write(ChunksCount);
            for (int i = 0; i < Chunks.Length; i++)
            {
                var e = Chunks[i];
                stream.Write(e.Location);
                stream.Write(e.Size);
            }
            stream.Write(HashCode);
        }

        /// <summary>
        /// Reads header instance from a stream.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <param name="result">Result data</param>
        public static void Read(SerializationStream stream, out ContentStorageHeader result)
        {
            result = new ContentStorageHeader();
            var version = stream.ReadInt32();
            if (version == 1)
            {
                result.InitialImage = stream.ReadBoolean();
                result.DataUrl = stream.ReadString();
                result.PackageTime = new DateTime(stream.ReadInt64());
                int chunksCount = stream.ReadInt32();
                result.Chunks = new ChunkEntry[chunksCount];
                for (int i = 0; i < chunksCount; i++)
                {
                    result.Chunks[i].Location = stream.ReadInt32();
                    result.Chunks[i].Size = stream.ReadInt32();
                }
                result.HashCode = stream.ReadInt32();

                return;
            }

            throw new SerializationException($"Invald {nameof(ContentStorageHeader)} version.");
        }
    }
}
