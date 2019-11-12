using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.IO;
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
            AssetId guid;
            UFile location;
            if (!AssetReference.TryParse(fromScalar.Value, out guid, out location))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode url reference [{0}]. Expecting format GUID:LOCATION".ToFormat(fromScalar.Value));
            }

            //TODO: IT would be better if this could use the UrlReferenceHelper class.
            var urlReference = (UrlReference)Activator.CreateInstance(context.Descriptor.Type, location.FullPath);

            var attachedReference = AttachedReferenceManager.GetOrCreateAttachedReference(urlReference);
            attachedReference.Id = guid;
            attachedReference.Url = location.FullPath;
            attachedReference.IsProxy = true;

            return urlReference;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            if(objectContext.Instance is UrlReference urlReference)
            {
                var attachedReference = AttachedReferenceManager.GetAttachedReference(urlReference);

                if(attachedReference != null)
                {
                    return $"{attachedReference.Id}:{urlReference.Url}";
                }
            }

            throw new YamlException($"Unable to extract url reference from object [{objectContext.Instance}]");
        }

       
    }
}
