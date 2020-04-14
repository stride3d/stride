// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Stride.Animations;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Rendering;

namespace Stride.Engine.Design
{
    /// <summary>
    /// Provides method for deep cloning of en <see cref="Entity"/>.
    /// </summary>
    [DataSerializerGlobal(typeof(CloneSerializer<Effect>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<SpriteSheet>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<SamplerState>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<Texture>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<Mesh>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<Model>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<AnimationClip>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<Sound>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<string>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<OfflineRasterizedSpriteFont>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<RuntimeRasterizedSpriteFont>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<SignedDistanceFieldSpriteFont>), Profile = "Clone")]
    public class EntityCloner
    {
        private static readonly CloneContext cloneContext = new CloneContext();
        private static SerializerSelector cloneSerializerSelector = null;
        public static readonly PropertyKey<CloneContext> CloneContextProperty = new PropertyKey<CloneContext>("CloneContext", typeof(EntityCloner));

        // CloneObject TLS used to clone entities, so that we don't create one everytime we clone
        [ThreadStatic]
        private static HashSet<object> clonedObjectsTLS;

        private static HashSet<object> ClonedObjects()
        {
            return clonedObjectsTLS ?? (clonedObjectsTLS = new HashSet<object>());
        }

        /// <summary>
        /// Clones the specified prefab.
        /// <see cref="Entity"/>, children <see cref="Entity"/> and their <see cref="EntityComponent"/> will be cloned.
        /// Other assets will be shared.
        /// </summary>
        /// <param name="prefab">The prefab to clone.</param>
        /// <returns>A cloned prefab</returns>
        public static Prefab Clone(Prefab prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            var clonedObjects = ClonedObjects();
            try
            {
                foreach (var entity in prefab.Entities)
                {
                    CollectEntityTreeHelper(entity, clonedObjects);
                }
                return Clone(clonedObjects, null, prefab);
            }
            finally
            {
                clonedObjects.Clear();
            }
        }

        /// <summary>
        /// Clones the specified entity.
        /// <see cref="Entity"/>, children <see cref="Entity"/> and their <see cref="EntityComponent"/> will be cloned.
        /// Other assets will be shared.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A cloned entity</returns>
        public static Entity Clone(Entity entity)
        {
            var clonedObjects = ClonedObjects();
            try
            {
                CollectEntityTreeHelper(entity, clonedObjects);
                return Clone(clonedObjects, null, entity);
            }
            finally
            {
                clonedObjects.Clear();
            }
        }

        /// <summary>
        /// Collect entities and components recursively from an entity and add them to a hashset.
        /// </summary>
        /// <param name="entity">The entity to collect</param>
        /// <param name="entityAndComponents">The collected entities and components</param>
        internal static void CollectEntityTreeHelper(Entity entity, HashSet<object> entityAndComponents)
        {
            // Already processed
            if (!entityAndComponents.Add(entity))
                return;

            foreach (var component in entity.Components)
            {
                entityAndComponents.Add(component);
            }

            var transformationComponent = entity.Transform;
            if (transformationComponent != null)
            {
                foreach (var child in transformationComponent.Children)
                {
                    CollectEntityTreeHelper(child.Entity, entityAndComponents);
                }
            }
        }

        /// <summary>
        /// Clones the specified object, taking special care of <see cref="Entity"/>, <see cref="EntityComponent"/> and external assets.
        /// User can optionally provides list of cloned objects (list of data reference objects that should be cloned)
        /// and mapped objects (list of data reference objects that should be ducplicated using the given instance).
        /// </summary>
        /// <param name="clonedObjects">The cloned objects.</param>
        /// <param name="mappedObjects">The mapped objects.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The cloned object.</returns>
        private static T Clone<T>(HashSet<object> clonedObjects, TryGetValueFunction<object, object> mappedObjects, T entity) where T : class
        {
            if (cloneSerializerSelector == null)
            {
                cloneSerializerSelector = new SerializerSelector(true, false, "Default", "Clone");
            }

            // Initialize CloneContext
            lock (cloneContext)
            {
                try
                {
                    cloneContext.EntitySerializerSelector = cloneSerializerSelector;

                    cloneContext.ClonedObjects = clonedObjects;
                    cloneContext.MappedObjects = mappedObjects;

                    // Serialize
                    var memoryStream = cloneContext.MemoryStream;
                    var writer = new BinarySerializationWriter(memoryStream);
                    writer.Context.SerializerSelector = cloneSerializerSelector;
                    writer.Context.Set(CloneContextProperty, cloneContext);
                    writer.SerializeExtended(entity, ArchiveMode.Serialize, null);

                    // Deserialization reuses this list and expect it to be empty at the beginning.
                    cloneContext.SerializedObjects.Clear();

                    // Deserialize
                    T result = null;
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var reader = new BinarySerializationReader(memoryStream);
                    reader.Context.SerializerSelector = cloneSerializerSelector;
                    reader.Context.Set(CloneContextProperty, cloneContext);
                    reader.SerializeExtended(ref result, ArchiveMode.Deserialize, null);

                    return result;
                }
                finally
                {
                    cloneContext.Cleanup();
                }
            }
        }

        public delegate bool TryGetValueFunction<in TKey, TResult>(TKey key, out TResult result);

        /// <summary>
        /// Helper class for cloning <see cref="Entity"/>.
        /// </summary>
        public class CloneContext
        {
            public void Cleanup()
            {
                MemoryStream.SetLength(0);
                MappedObjects = null;
                SerializedObjects.Clear();
                ClonedObjects = null;
                SharedObjects.Clear();
                EntitySerializerSelector = null;
            }

            public MemoryStream MemoryStream = new MemoryStream(4096);

            public TryGetValueFunction<object, object> MappedObjects;

            public readonly HashSet<object> SerializedObjects = new HashSet<object>();

            /// <summary>
            /// Lists objects that should be cloned.
            /// </summary>
            public HashSet<object> ClonedObjects;

            /// <summary>
            /// Stores objects that should be reused in the new cloned instance.
            /// </summary>
            public readonly List<object> SharedObjects = new List<object>();

            /// <summary>
            /// Special serializer that goes through <see cref="EntitySerializerSelector"/> and <see cref="CloneEntityComponentSerializer{T}"/>.
            /// </summary>
            public SerializerSelector EntitySerializerSelector;
        }
    }
}
