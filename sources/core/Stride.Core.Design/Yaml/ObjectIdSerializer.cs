// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Storage;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml;

/// <summary>
/// A Yaml serializer for <see cref="ObjectId"/>
/// </summary>
[YamlSerializerFactory(YamlSerializerFactoryAttribute.Default)]
internal class ObjectIdSerializer : AssetScalarSerializerBase
{
    public override bool CanVisit(Type type)
    {
        return type == typeof(ObjectId);
    }

    public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
    {
        _ = ObjectId.TryParse(fromScalar.Value, out var objectId);
        return objectId;
    }

    public override string ConvertTo(ref ObjectContext objectContext)
    {
        return ((ObjectId)objectContext.Instance).ToString();
    }
}
