// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Streaming;

namespace Stride.Graphics.Data
{
    /// <summary>
    /// Used internally to find the currently active textures streaming service
    /// </summary>
    public interface ITexturesStreamingProvider
    {
        /// <summary>
        /// Loads the texture in a streaming service.
        /// </summary>
        /// <param name="obj">The texture object.</param>
        /// <param name="imageDescription">The image description.</param>
        /// <param name="storageHeader">The storage header.</param>
        void FullyLoadTexture(Texture obj, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader);

        /// <summary>
        /// Registers the texture in a streaming service.
        /// </summary>
        /// <param name="obj">The texture object.</param>
        /// <param name="imageDescription">The image description.</param>
        /// <param name="storageHeader">The storage header.</param>
        void RegisterTexture(Texture obj, ref ImageDescription imageDescription, ref ContentStorageHeader storageHeader);

        /// <summary>
        /// Unregisters the texture.
        /// </summary>
        /// <param name="obj">The texture object.</param>
        void UnregisterTexture(Texture obj);
    }
}
