// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Xenko.Core.IO;

namespace Xenko.Core.Serialization.Contents
{
    /// <summary>
    /// Interface of the asset manager.
    /// </summary>
    public interface IContentManager
    {
        /// <summary>
        /// Check if the specified asset url exists.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns><c>true</c> if the specified asset url exists, <c>false</c> otherwise.</returns>
        bool Exists(string url);

        /// <summary>
        /// Opens the specified URL as a stream used for custom raw asset loading.
        /// </summary>
        /// <param name="url">The URL to the raw asset.</param>
        /// <param name="streamFlags">The type of stream needed</param>
        /// <returns>A stream to the raw asset.</returns>
        Stream OpenAsStream(string url, StreamFlags streamFlags = StreamFlags.None);

        /// <summary>
        /// Loads content from the specified URL.
        /// </summary>
        /// <typeparam name="T">The content type.</typeparam>
        /// <param name="url">The URL to load from.</param>
        /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default"/>.</param>
        /// <returns></returns>
        T Load<T>(string url, ContentManagerLoaderSettings settings = null) where T : class;

        /// <summary>
        /// Loads content from the specified URL asynchronously.
        /// </summary>
        /// <typeparam name="T">The content type.</typeparam>
        /// <param name="url">The URL to load from.</param>
        /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default"/>.</param>
        /// <returns></returns>
        Task<T> LoadAsync<T>(string url, ContentManagerLoaderSettings settings = null) where T : class;

        /// <summary>
        /// Gets a previously loaded asset from its URL.
        /// </summary>
        /// <typeparam name="T">The type of asset to retrieve.</typeparam>
        /// <param name="url">The URL of the asset to retrieve.</param>
        /// <returns>The loaded asset, or <c>null</c> if the asset has not been loaded.</returns>
        /// <remarks>This function does not increase the reference count on the asset.</remarks>
        T Get<T>(string url) where T : class;

        /// <summary>
        /// Gets whether an asset with the given URL is currently loaded.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <param name="loadedManuallyOnly">If <c>true</c>, this method will return true only if an asset with the given URL has been manually loaded via <see cref="Load"/>, and not if the asset has been only loaded indirectly from another asset.</param>
        /// <returns><c>True</c> if an asset with the given URL is currently loaded, <c>false</c> otherwise.</returns>
        bool IsLoaded(string url, bool loadedManuallyOnly = false);

        /// <summary>
        /// Unloads the specified object.
        /// </summary>
        /// <param name="obj">The object to unload.</param>
        void Unload(object obj);

        /// <summary>
        /// Unloads the asset at the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        void Unload(string url);

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        /// <value>The serializer.</value>
        ContentSerializer Serializer { get; }
    }
}
