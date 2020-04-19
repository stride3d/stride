// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A serializer that is run before the <see cref="RoutingSerializer"/> and allow to push and pop contextual properties.
    /// </summary>
    internal class ContextAttributeSerializer : ChainedSerializer
    {
        /// <summary>
        /// A structure containing all the information needed to pop contextual properties after serialization.
        /// </summary>
        private struct ContextToken
        {
            /// <summary>
            /// Indicates if we entered a context where no collection should be identifiable anymore, indicated by the <see cref="NonIdentifiableCollectionItemsAttribute"/>.
            /// </summary>
            public bool NonIdentifiableItems;
        }

        /// <inheritdoc/>
        public override void WriteYaml(ref ObjectContext objectContext)
        {
            var token = PreSerialize(ref objectContext);
            base.WriteYaml(ref objectContext);
            PostSerialize(ref objectContext, token);
        }

        /// <inheritdoc/>
        public override object ReadYaml(ref ObjectContext objectContext)
        {
            var token = PreSerialize(ref objectContext);
            var result = base.ReadYaml(ref objectContext);
            PostSerialize(ref objectContext, token);
            return result;
        }

        private static ContextToken PreSerialize(ref ObjectContext objectContext)
        {
            var token = new ContextToken();
            // Check if we enter a context where collection items are not identifiable anymore (see doc of the related attribute)
            if (objectContext.Descriptor.Type.GetCustomAttribute<NonIdentifiableCollectionItemsAttribute>(true) != null)
            {
                if (!objectContext.SerializerContext.Properties.ContainsKey(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey))
                {
                    token.NonIdentifiableItems = true;
                    objectContext.SerializerContext.Properties.Add(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey, true);
                }
            }
            return token;
        }

        private static void PostSerialize(ref ObjectContext objectContext, ContextToken contextToken)
        {
            // Restore contexts that must be restored
            if (contextToken.NonIdentifiableItems)
            {
                objectContext.SerializerContext.Properties.Remove(CollectionWithIdsSerializerBase.NonIdentifiableCollectionItemsKey);
            }
        }
    }
}
