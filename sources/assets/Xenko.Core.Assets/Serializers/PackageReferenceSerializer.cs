// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;
using Xenko.Core.Yaml.Serialization;

namespace Xenko.Core.Assets.Serializers
{
    /// <summary>
    /// A Yaml serializer for <see cref="PackageReference"/>
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class PackageReferenceSerializer : AssetScalarSerializerBase
    {
        public override bool CanVisit(Type type)
        {
            return type == typeof(PackageReference);
        }

        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            PackageReference packageReference;
            if (!PackageReference.TryParse(fromScalar.Value, out packageReference))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode package reference [{0}]. Expecting format GUID:LOCATION".ToFormat(fromScalar.Value));
            }
            return packageReference;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            return objectContext.Instance.ToString();
        }

        public override void Visit(ref VisitorContext context)
        {
            // For a package reference, we visit its members to allow to update the paths when loading, saving
            context.Visitor.VisitObject(context.Instance, context.Descriptor, true);
        }
    }
}
