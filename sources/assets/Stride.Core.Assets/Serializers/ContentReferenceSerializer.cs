// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets.Serializers;

[YamlSerializerFactory(YamlAssetProfile.Name)]
public class ContentReferenceSerializer : AssetScalarSerializerBase
{
    public static readonly ContentReferenceSerializer Default = new();

    public override bool CanVisit(Type type)
    {
        return AssetRegistry.IsExactContentType(type);
    }

    public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
    {
        if (!AssetReference.TryParse(fromScalar.Value, out var guid, out var location))
        {
            throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to decode asset reference [{0}]. Expecting format GUID:LOCATION".ToFormat(fromScalar.Value));
        }

        var instance = AttachedReferenceManager.CreateProxyObject(context.Descriptor.Type, guid, location);
        return instance;
    }

    public override string ConvertTo(ref ObjectContext objectContext)
    {
        var attachedReference = AttachedReferenceManager.GetAttachedReference(objectContext.Instance)
            ?? throw new YamlException($"Unable to extract asset reference from object [{objectContext.Instance}]");
        return $"{attachedReference.Id}:{attachedReference.Url}";
    }
}
