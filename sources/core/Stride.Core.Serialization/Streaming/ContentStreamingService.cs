// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1405 // Debug.Assert must provide message text
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Stride.Core.Extensions;

namespace Stride.Core.Streaming
{
    /// <summary>
    /// Streamable resources content management service.
    /// </summary>
    public class ContentStreamingService : IDisposable
    {
        private readonly Dictionary<string, ContentStorage> containers = new Dictionary<string, ContentStorage>();

        /// <summary>
        /// The unused data chunks lifetime.
        /// </summary>
        public TimeSpan UnusedDataChunksLifetime = TimeSpan.FromSeconds(3);
        
        /// <summary>
        /// Gets the storage container.
        /// </summary>
        /// <param name="storageHeader">The storage header.</param>
        /// <returns>Content Storage container.</returns>
        public ContentStorage GetStorage(ref ContentStorageHeader storageHeader)
        {
            ContentStorage result;

            lock (containers)
            {
                if (!containers.TryGetValue(storageHeader.DataUrl, out result))
                {
                    result = new ContentStorage(this);
                    containers.Add(storageHeader.DataUrl, result);
                }
                result.Init(ref storageHeader);
            }

            Debug.Assert(result != null && result.Url == storageHeader.DataUrl);
            return result;
        }

        /// <summary>
        /// Updates this service.
        /// </summary>
        public void Update()
        {
            lock (containers)
            {
                foreach (var e in containers)
                    e.Value.ReleaseUnusedChunks();
            }
        }

        /// <summary>
        /// Performs resources disposing.
        /// </summary>
        public void Dispose()
        {
            lock (containers)
            {
                containers.ForEach(x => x.Value.ReleaseChunks());
                containers.Clear();
            }
        }

        internal void UnregisterStorage(ContentStorage storage)
        {
            lock (containers)
            {
                containers.Remove(storage.Url);
            }
        }
    }
}
