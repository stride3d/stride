using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;
using Xenko.Core.Yaml.Serialization;

namespace Xenko.Core.Assets.Serializers
{
    /// <summary>
    /// A Yaml serializer for <see cref="UrlReference"/>
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class UrlReferenceSerializer : AssetScalarSerializerBase
    {
        public override bool CanVisit(Type type)
        {
            return typeof(UrlReference).IsAssignableFrom(type);
        }

        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            if (!TryParse(fromScalar.Value, context.Descriptor.Type, out UrlReference urlReference))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode url reference [{0}]. Expecting format LOCATION".ToFormat(fromScalar.Value));
            }
            return urlReference;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            return objectContext.Instance.ToString();
        }

        /// <summary>
        /// Tries to parse an url reference in the format "[GUID/]GUID:Location". The first GUID is optional and is used to store the ID of the reference.
        /// </summary>
        /// <param name="urlReferenceText">The url reference.</param>
        /// <param name="id">The unique identifier of asset pointed by this reference.</param>
        /// <param name="url">The url.</param>
        /// <returns><c>true</c> if parsing was successful, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">urlReferenceText</exception>
        private static bool TryParse([NotNull] string urlReferenceText, out AssetId id, out string url)
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
        private static bool TryParse([NotNull] string urlReferenceText, Type urlReferenceType, out UrlReference urlReference)
        {
            if (urlReferenceText == null)
            {
                throw new ArgumentNullException(nameof(urlReferenceText));
            }

            if (urlReferenceType == null)
            {
                throw new ArgumentNullException(nameof(urlReferenceType));
            }

            if (!typeof(UrlReference).IsAssignableFrom(urlReferenceType))
            {
                throw new ArgumentException("Not a UrlReference type.", nameof(urlReferenceType));
            }
            
            urlReference = (UrlReference)Activator.CreateInstance(urlReferenceType, urlReferenceText);
            return true;
        }
    }
}
