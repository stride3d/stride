// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
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
            return UrlReferenceHelper.IsUrlReferenceType(type);
        }

        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            if (!AssetReference.TryParse(fromScalar.Value, out var guid, out var location))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode url reference [{0}]. Expecting format GUID:LOCATION".ToFormat(fromScalar.Value));
            }

            var urlReference = UrlReferenceHelper.CreateReference(context.Descriptor.Type, guid, location.FullPath);

            return urlReference;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            if (objectContext.Instance is UrlReference urlReference)
            {
                var attachedReference = AttachedReferenceManager.GetAttachedReference(urlReference);

                if (attachedReference != null)
                {
                    return $"{attachedReference.Id}:{urlReference.Url}";
                }
            }

            throw new YamlException($"Unable to extract url reference from object [{objectContext.Instance}]");
        }


    }
}
