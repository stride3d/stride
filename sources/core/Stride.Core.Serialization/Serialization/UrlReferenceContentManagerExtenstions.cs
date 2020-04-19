// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Threading.Tasks;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Extension methods of <see cref="IContentManager"/> to allow usage of <see cref="UrlReference"/> and <see cref="UrlReference{T}"/>.
    /// </summary>
    public static class UrlReferenceContentManagerExtenstions
    {
        /// <summary>
        /// Check if the specified asset url exists.
        /// </summary>
        /// <param name="content">The <see cref="IContentManager"/>.</param>
        /// <param name="urlReference">The URL.</param>
        /// <returns><c>true</c> if the specified asset url exists, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
        public static bool Exists(this IContentManager content, IUrlReference urlReference)
        {
            CheckArguments(content, urlReference);

            return content.Exists(urlReference.Url);
        }

        /// <summary>
        /// Opens the specified URL as a stream used for custom raw asset loading.
        /// </summary>
        /// <param name="content">The <see cref="IContentManager"/>.</param>
        /// <param name="urlReference">The URL to the raw asset.</param>
        /// <param name="streamFlags">The type of stream needed</param>
        /// <returns>A stream to the raw asset.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
        public static Stream OpenAsStream(this IContentManager content, UrlReference urlReference, StreamFlags streamFlags = StreamFlags.None)
        {
            CheckArguments(content, urlReference);

            return content.OpenAsStream(urlReference.Url, streamFlags);
        }

        /// <summary>
        /// Loads content from the specified URL.
        /// </summary>
        /// <typeparam name="T">The content type.</typeparam>
        /// <param name="content">The <see cref="IContentManager"/>.</param>
        /// <param name="urlReference">The URL to load from.</param>
        /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default"/>.</param>
        /// <returns>The loaded content.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
        public static T Load<T>(this IContentManager content, UrlReference<T> urlReference, ContentManagerLoaderSettings settings = null)
            where T : class
        {
            CheckArguments(content, urlReference);

            return content.Load<T>(urlReference.Url, settings);
        }

        /// <summary>
        /// Loads content from the specified URL asynchronously.
        /// </summary>
        /// <typeparam name="T">The content type.</typeparam>
        /// <param name="content">The <see cref="IContentManager"/>.</param>
        /// <param name="urlReference">The URL to load from.</param>
        /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default"/>.</param>
        /// <returns>The loaded content.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
        public static Task<T> LoadAsync<T>(this IContentManager content, UrlReference<T> urlReference, ContentManagerLoaderSettings settings = null)
            where T : class
        {
            CheckArguments(content, urlReference);

            return content.LoadAsync<T>(urlReference.Url, settings);
        }

        /// <summary>
        /// Gets a previously loaded asset from its URL.
        /// </summary>
        /// <typeparam name="T">The type of asset to retrieve.</typeparam>
        /// <param name="urlReference">The URL of the asset to retrieve.</param>
        /// <returns>The loaded asset, or <c>null</c> if the asset has not been loaded.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
        /// <remarks>This function does not increase the reference count on the asset.</remarks>
        public static T Get<T>(this IContentManager content, UrlReference<T> urlReference)
            where T : class
        {
            CheckArguments(content, urlReference);

            return content.Get<T>(urlReference.Url);
        }

        /// <summary>
        /// Gets whether an asset with the given URL is currently loaded.
        /// </summary>
        /// <param name="urlReference">The URL to check.</param>
        /// <param name="loadedManuallyOnly">If <c>true</c>, this method will return true only if an asset with the given URL has been manually loaded via <see cref="Load"/>, and not if the asset has been only loaded indirectly from another asset.</param>
        /// <returns><c>True</c> if an asset with the given URL is currently loaded, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
        public static bool IsLoaded(this IContentManager content, IUrlReference urlReference, bool loadedManuallyOnly = false)
        {
            CheckArguments(content, urlReference);

            return content.IsLoaded(urlReference.Url, loadedManuallyOnly);
        }

        /// <summary>
        /// Unloads the asset at the specified URL.
        /// </summary>
        /// <param name="urlReference">The URL.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
        public static void Unload(this IContentManager content, IUrlReference urlReference)
        {
            CheckArguments(content, urlReference);

            content.Unload(urlReference.Url);
        }

        private static void CheckArguments(IContentManager content, IUrlReference urlReference)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (urlReference == null || urlReference.IsEmpty)
            {
                throw new ArgumentNullException(nameof(urlReference));
            }
        }
    }
}
