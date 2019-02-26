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
        public UrlReference(AssetId id, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException($"{nameof(url)} cannot be null or empty.", nameof(url));
            }

            Id = id;
            Url = url;
        }

        [DataMember(10)]
        public AssetId Id { get; }

        [DataMember(20)]
        public string Url { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            // WARNING: This should not be modified as it is used for serializing
            return $"{Id}:{Url}";
        }

        /// <summary>
        /// Tries to parse an url reference in the format "[GUID/]GUID:Location". The first GUID is optional and is used to store the ID of the reference.
        /// </summary>
        /// <param name="urlReferenceText">The url reference.</param>
        /// <param name="id">The unique identifier of asset pointed by this reference.</param>
        /// <param name="url">The url.</param>
        /// <returns><c>true</c> if parsing was successful, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">urlReferenceText</exception>
        public static bool TryParse([NotNull] string urlReferenceText, out AssetId id, out string url)
        {
            if (urlReferenceText == null) throw new ArgumentNullException(nameof(urlReferenceText));

            id = AssetId.Empty;
            url = null;
            var indexFirstSlash = urlReferenceText.IndexOf('/');
            var indexBeforelocation = urlReferenceText.IndexOf(':');
            if (indexBeforelocation < 0)
            {
                return false;
            }
            var startNextGuid = 0;
            if (indexFirstSlash > 0 && indexFirstSlash < indexBeforelocation)
            {
                startNextGuid = indexFirstSlash + 1;
            }

            if (!AssetId.TryParse(urlReferenceText.Substring(startNextGuid, indexBeforelocation - startNextGuid), out id))
            {
                return false;
            }

            url = urlReferenceText.Substring(indexBeforelocation + 1);

            return true;
        }

        /// <summary>
        /// Tries to parse an url reference in the format "GUID:Location".
        /// </summary>
        /// <param name="urlReferenceText">The asset reference.</param>
        /// <param name="urlReferenceType">The type of url reference to create.</param>
        /// <param name="urlReference">The reference.</param>
        /// <returns><c>true</c> if parsing was successful, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">urlReferenceText, urlReferenceType</exception>
        /// <exception cref="ArgumentException">If <see cref="urlReferenceType"/> is not of <see cref="UrlReference"/> or <see cref="UrlReference{T}"/>.</exception>
        public static bool TryParse([NotNull] string urlReferenceText, Type urlReferenceType, out UrlReference urlReference)
        {
            if (urlReferenceText == null)
            {
                throw new ArgumentNullException(nameof(urlReferenceText));
            }

            if (urlReferenceType == null)
            {
                throw new ArgumentNullException(nameof(urlReferenceType));
            }

            if(!typeof(UrlReference).IsAssignableFrom(urlReferenceType))
            {
                throw new ArgumentException("Not a UrlReference type.",nameof(urlReferenceType));
            }

            urlReference = null;
            AssetId assetId;
            string url;
            if (!TryParse(urlReferenceText, out assetId, out url))
            {
                return false;
            }
            urlReference = (UrlReference)Activator.CreateInstance(urlReferenceType, assetId, url);
            return true;
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
        public UrlReference(AssetId id, string url) : base(id, url)
        {
        }

    }

    
}
