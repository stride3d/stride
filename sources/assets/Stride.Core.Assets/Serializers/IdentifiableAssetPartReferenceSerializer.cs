// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets.Serializers
{
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    public sealed class IdentifiableAssetPartReferenceSerializer : ScalarOrObjectSerializer
    {
        public override bool CanVisit(Type type)
        {
            return type == typeof(IdentifiableAssetPartReference);
        }

        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            Guid guid;
            if (!Guid.TryParse(fromScalar.Value, out guid))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode asset part reference [{0}]. Expecting an ENTITY_GUID".ToFormat(fromScalar.Value));
            }

            var result = context.Instance as IdentifiableAssetPartReference ?? (IdentifiableAssetPartReference)(context.Instance = new IdentifiableAssetPartReference());
            result.Id = guid;

            return result;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            return ((IdentifiableAssetPartReference)objectContext.Instance).Id.ToString();
        }
    }
}
