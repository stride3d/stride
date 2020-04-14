// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Serializers;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;
using Stride.Engine;
using SerializerContext = Stride.Core.Yaml.Serialization.SerializerContext;

namespace Stride.Debugger.Target
{
    /// <summary>
    /// When serializing/deserializing Yaml for live objects, this serializer will handle those objects as reference (similar to Clone serializer).
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    public class CloneReferenceSerializer : ObjectSerializer
    {
        // TODO: We might want to share some of the recursive logic with PrefabAssetSerializer?
        // However, ThreadStatic would still need to be separated...
        [ThreadStatic]
        private static int recursionLevel;

        /// <summary>
        /// The list of live references during that serialization/deserialization cycle.
        /// </summary>
        [ThreadStatic] internal static List<object> References;

        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            if (CanVisit(typeDescriptor.Type))
                return this;

            return null;
        }

        private bool CanVisit(Type type)
        {
            // Also handles Entity, EntityComponent and Script
            return AssetRegistry.IsContentType(type)
                   || type == typeof(Entity) || typeof(Entity).IsAssignableFrom(type) || typeof(EntityComponent).IsAssignableFrom(type);
        }

        /// <inheritdoc/>
        protected override void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            if (recursionLevel >= 2)
            {
                // We are inside a Script
                // Transform everything into CloneReference for both serialization and deserialization
                if (objectContext.SerializerContext.IsSerializing)
                {
                    var index = References.Count;
                    objectContext.Tag = objectContext.Settings.TagTypeRegistry.TagFromType(objectContext.Instance.GetType());
                    References.Add(objectContext.Instance);
                    objectContext.Instance = new CloneReference { Id = index };
                }
                else
                {
                    objectContext.Instance = new CloneReference();
                }
            }

            base.CreateOrTransformObject(ref objectContext);
        }

        /// <inheritdoc/>
        protected override void TransformObjectAfterRead(ref ObjectContext objectContext)
        {
            if (recursionLevel >= 2)
            {
                // We are inside a Script
                if (!objectContext.SerializerContext.IsSerializing)
                {
                    if (objectContext.Instance is CloneReference)
                    {
                        objectContext.Instance = References[((CloneReference)objectContext.Instance).Id];
                        return;
                    }
                }
            }

            base.TransformObjectAfterRead(ref objectContext);
        }

        /// <inheritdoc/>
        public override void WriteYaml(ref ObjectContext objectContext)
        {
            recursionLevel++;

            try
            {
                base.WriteYaml(ref objectContext);
            }
            finally
            {
                recursionLevel--;
            }
        }

        /// <inheritdoc/>
        public override object ReadYaml(ref ObjectContext objectContext)
        {
            recursionLevel++;

            try
            {
                return base.ReadYaml(ref objectContext);
            }
            finally
            {
                recursionLevel--;
            }
        }

        /// <summary>
        /// Helper class used by CloneReferenceSerializer
        /// </summary>
        internal class CloneReference
        {
            public int Id;
        }
    }
}
