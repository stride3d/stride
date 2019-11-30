using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.IO;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Serialization
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
        public static bool Exists(this IContentManager content, UrlReference urlReference)
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
        /// <returns></returns>
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
        /// <returns></returns>
        public static Task<T> LoadAsync<T>(this IContentManager content, UrlReference<T> urlReference, ContentManagerLoaderSettings settings = null)
            where T : class
        {
            CheckArguments(content, urlReference);

            return content.LoadAsync<T>(urlReference.Url, settings);
        }

        private static void CheckArguments(IContentManager content, UrlReference urlReference)
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
