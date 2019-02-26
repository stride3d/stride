using System;
using Xenko.Core;
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
            UrlReference urlReference;
            if (!UrlReference.TryParse(fromScalar.Value, context.Descriptor.Type, out urlReference))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode url reference [{0}]. Expecting format GUID:LOCATION".ToFormat(fromScalar.Value));
            }
            return urlReference;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            return objectContext.Instance.ToString();
        }
    }
}
