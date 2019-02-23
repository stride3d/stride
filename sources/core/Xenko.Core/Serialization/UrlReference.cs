using System;
using System.Collections.Generic;
using System.Text;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// Represents a Url to an asset.
    /// </summary>
    public class UrlReference
    {
        /// <summary>
        /// Create a new <see cref="UrlReference"/> instance.
        /// </summary>
        /// <param name="url"></param>
        public UrlReference(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException($"{nameof(url)} cannot be null or empty.", nameof(url));
            }

            Url = url;
        }

        public string Url { get; }

    }

    /// <summary>
    /// Represents a Url to an asset of type <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class UrlReference<T> : UrlReference
        where T : class
    {
        /// <summary>
        /// Create a new <see cref="UrlReference{T}"/> instance.
        /// </summary>
        /// <param name="url"></param>
        public UrlReference(string url) : base(url)
        {
        }
    }
}
