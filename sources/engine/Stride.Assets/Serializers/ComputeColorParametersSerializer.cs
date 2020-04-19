// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Serializers;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Assets.Serializers
{
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class ComputeColorParametersSerializer : DictionaryWithIdsSerializer, IDataCustomVisitor
    {
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.Type;
            return CanVisit(type) ? this : null;
        }

        protected override void WriteDictionaryItems(ref ObjectContext objectContext)
        {
            //TODO: make SortKeyForMapping accessible in object context since it modifies the behavior of the serializer for children of the ComputeColorParameters
            var savedSettings = objectContext.Settings.SortKeyForMapping;
            objectContext.Settings.SortKeyForMapping = false;
            base.WriteDictionaryItems(ref objectContext);
            objectContext.Settings.SortKeyForMapping = savedSettings;
        }

        public bool CanVisit(Type type)
        {
            return typeof(ComputeColorParameters).IsAssignableFrom(type);
        }

        public void Visit(ref VisitorContext context)
        {
            // Visit a ComputeColorParameters without visiting properties
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }
    }
}
