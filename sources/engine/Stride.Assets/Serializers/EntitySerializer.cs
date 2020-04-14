// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Serializers;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml.Serialization;
using Xenko.Core.Yaml.Serialization.Serializers;
using Xenko.Engine;

namespace Xenko.Assets.Serializers
{
    /// <summary>
    /// A serializer for the <see cref="Entity"/> type.
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    public class EntitySerializer : ObjectSerializer
    {
        /// <inheritdoc/>
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return typeDescriptor.Type == typeof(Entity) ? this : null;
        }

        /// <inheritdoc/>
        protected override void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            base.CreateOrTransformObject(ref objectContext);

            // When deserializing, we don't keep the TransformComponent created when the Entity is created
            if (!objectContext.SerializerContext.IsSerializing)
            {
                var entity = (Entity)objectContext.Instance;
                entity.Components.Clear();
            }
        }
    }
}
