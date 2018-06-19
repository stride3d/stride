// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Serializers;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;
using Xenko.Core.Yaml.Serialization.Serializers;
using Xenko.Engine;
using SerializerContext = Xenko.Core.Yaml.Serialization.SerializerContext;

namespace Xenko.Debugger.Target
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
