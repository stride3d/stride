// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;
using Xenko.Core.Yaml.Serialization;

namespace Xenko.Core.Assets.Serializers
{
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    public class ContentReferenceSerializer : AssetScalarSerializerBase
    {
        public static readonly ContentReferenceSerializer Default = new ContentReferenceSerializer();

        public override bool CanVisit(Type type)
        {
            return AssetRegistry.IsContentType(type);
        }

        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            AssetId guid;
            UFile location;
            if (!AssetReference.TryParse(fromScalar.Value, out guid, out location))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode asset reference [{0}]. Expecting format GUID:LOCATION".ToFormat(fromScalar.Value));
            }

            var instance = AttachedReferenceManager.CreateProxyObject(context.Descriptor.Type, guid, location);
            return instance;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            var attachedReference = AttachedReferenceManager.GetAttachedReference(objectContext.Instance);
            if (attachedReference == null)
                throw new YamlException($"Unable to extract asset reference from object [{objectContext.Instance}]");

            return $"{attachedReference.Id}:{attachedReference.Url}";
        }
    }
}
