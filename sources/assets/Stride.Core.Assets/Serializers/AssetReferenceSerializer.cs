// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets.Serializers;

/// <summary>
/// A Yaml serializer for <see cref="AssetReference"/>
/// </summary>
[YamlSerializerFactory(YamlAssetProfile.Name)]
internal class AssetReferenceSerializer : AssetScalarSerializerBase
{
    public override bool CanVisit(Type type)
    {
        return typeof(AssetReference).IsAssignableFrom(type);
    }

    public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
    {
        if (!AssetReference.TryParse(fromScalar.Value, out var id, out var location))
        {
            throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode asset reference [{0}]. Expecting format GUID:LOCATION".ToFormat(fromScalar.Value));
        }
        return AssetReference.New(id, new UFile(ReferenceSerializationHelper.RestoreLocation(ref context, location.FullPath)));
    }

    public override string ConvertTo(ref ObjectContext objectContext)
    {
        var assetReference = (AssetReference)objectContext.Instance;
        return ReferenceSerializationHelper.FormatReference(ref objectContext, assetReference.Id, assetReference.Location);
    }
}
