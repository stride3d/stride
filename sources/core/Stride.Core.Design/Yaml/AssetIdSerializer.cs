// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml;

/// <summary>
/// A Yaml serializer for <see cref="Guid"/>
/// </summary>
[YamlSerializerFactory(YamlSerializerFactoryAttribute.Default)]
internal class AssetIdSerializer : AssetScalarSerializerBase
{
    public override bool CanVisit(Type type)
    {
        return type == typeof(AssetId);
    }

    public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
    {
        _ = AssetId.TryParse(fromScalar.Value, out var assetId);
        return assetId;
    }

    public override string ConvertTo(ref ObjectContext objectContext)
    {
        return ((AssetId)objectContext.Instance).ToString();
    }
}
