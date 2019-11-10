using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Xenko.Core.Annotations;
using Xenko.Core.Assets;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Serialization.Serialization.Serializers;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// Represents a Url to an asset.
    /// </summary>
    [DataContract("urlref", Inherited = true)]
    [DataStyle(DataStyle.Compact)]
    [DataSerializer(typeof(UrlReferenceDataSerializer))]
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

        [DataMember(10)]
        public string Url { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            // WARNING: This should not be modified as it is used for serializing
            return $"{Url}";
        }

      
    }

    /// <summary>
    /// Represents a Url to an asset of type <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T">The type off asset.</typeparam>
    [DataStyle(DataStyle.Compact)]
    [DataSerializer(typeof(UrlReferenceDataSerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class UrlReference<T> : UrlReference
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
