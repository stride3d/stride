// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// Base class for <see cref="IUrlReference" /> implementations
    /// </summary>
    [DataContract("urlref", Inherited = true)]
    [DataStyle(DataStyle.Compact)]
    [ReferenceSerializer]
    public abstract class UrlReferenceBase : IUrlReference
    {
        private string url;

        /// <summary>
        /// Create a new <see cref="UrlReferenceBase"/> instance.
        /// </summary>
        protected UrlReferenceBase()
        {

        }

        /// <summary>
        /// Create a new <see cref="UrlReferenceBase"/> instance.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="url"/> is <c>null</c> or empty.</exception>
        protected UrlReferenceBase(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url), $"{nameof(url)} cannot be null or empty.");
            }

            this.url = url;
        }

        // <summary>
        /// Gets the Url of the referenced asset.
        /// </summary>
        [DataMember(10)]
        public string Url
        {
            get => url;
            internal set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(nameof(value), $"{nameof(Url)} cannot be null or empty.");
                }
                url = value;
            }
        }

        /// <summary>
        /// Gets whether the url is <c>null</c> or empty.
        /// </summary>
        [DataMemberIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(url);

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Url}";
        }
    }
}
