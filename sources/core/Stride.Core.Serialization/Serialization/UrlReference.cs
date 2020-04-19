// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Represents a Url to an asset.
    /// </summary>
    [DataSerializer(typeof(UrlReferenceDataSerializer))]
    public sealed class UrlReference : UrlReferenceBase
    {
        /// <summary>
        /// Create a new <see cref="UrlReference"/> instance.
        /// </summary>
        public UrlReference()
        {

        }

        /// <summary>
        /// Create a new <see cref="UrlReference"/> instance.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="url"/> is <c>null</c> or empty.</exception>
        public UrlReference(string url) : base(url)
        {
        }
    }

    /// <summary>
    /// Represents a Url to an asset of type <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T">The type off asset.</typeparam>
    [DataSerializer(typeof(UrlReferenceDataSerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class UrlReference<T> : UrlReferenceBase
        where T : class
    {
        /// <summary>
        /// Create a new <see cref="UrlReference{T}"/> instance.
        /// </summary>
        public UrlReference()
        {

        }

        /// <summary>
        /// Create a new <see cref="UrlReference{T}"/> instance.
        /// </summary>
        /// <param name="url"></param>
        /// <exception cref="ArgumentNullException">If <paramref name="url"/> is <c>null</c> or empty.</exception>
        public UrlReference(string url) : base(url)
        {
        }
    }
}
