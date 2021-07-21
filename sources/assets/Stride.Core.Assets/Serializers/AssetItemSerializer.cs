// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Assets.Serializers
{
    /// <summary>
    /// A Yaml Serializer for <see cref="AssetBase"/>. Because this type is immutable
    /// we need to implement a special serializer.
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class AssetItemSerializer : ObjectSerializer, IDataCustomVisitor
    {
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return CanVisit(typeDescriptor.Type) ? this : null;
        }

        protected override void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            objectContext.Instance = objectContext.SerializerContext.IsSerializing ? new AssetItemMutable((AssetItem)objectContext.Instance) : new AssetItemMutable();
        }

        protected override void TransformObjectAfterRead(ref ObjectContext objectContext)
        {
            objectContext.Instance = ((AssetItemMutable)objectContext.Instance).ToAssetItem();
        }

        private class AssetItemMutable
        {
            public AssetItemMutable()
            {
            }

            public AssetItemMutable(AssetItem item)
            {
                Location = item.Location;
                SourceFolder = item.SourceFolder;
                Asset = item.Asset;
                AlternativePath = item.AlternativePath;
            }

            [DataMember(0)]
            public UFile Location;

            [DataMember(1)]
            [DefaultValue(null)]
            public UDirectory SourceFolder;

            [DataMember(2)]
            public Asset Asset;

            [DataMember(3)]
            public UFile AlternativePath;

            public AssetItem ToAssetItem()
            {
                return new AssetItem(Location, Asset) { SourceFolder = SourceFolder, AlternativePath = AlternativePath };
            }
        }

        public bool CanVisit(Type type)
        {
            return type == typeof(AssetItem);
        }

        public void Visit(ref VisitorContext context)
        {
            context.Visitor.VisitObject(context.Instance, context.Descriptor, true);
        }
    }
}
