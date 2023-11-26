// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;

namespace Stride.Engine
{
    /// <summary>
    /// A prefab that contains entities.
    /// </summary>
    [DataContract("Prefab")]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<Prefab>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Prefab>), Profile = "Content")]
    public sealed class Prefab
    {
        /// <summary>
        /// The entities.
        /// </summary>
        public List<Entity> Entities { get; } = new List<Entity>();

        /// <summary>
        /// Instantiates entities from a prefab that can be later added to a <see cref="Scene"/>.
        /// </summary>
        /// <returns>A collection of entities extracted from the prefab</returns>
        public List<Entity> Instantiate()
        {
            var newPrefab = EntityCloner.Clone(this);
            CreateNewIds(newPrefab.Entities);
            return newPrefab.Entities;
        }

        /// <summary>
        /// Creates new IDs for the specified entities and their components.
        /// </summary>
        /// <param name="entities">The entities to create new IDs for</param>
        private static void CreateNewIds(IEnumerable<Entity> entities)
        {
            var objects = new HashSet<object>();
            foreach (var entity in entities)
            {
                EntityCloner.CollectEntityTreeHelper(entity, objects);
            }

            foreach (var item in objects)
            {
                if (item is Entity entity)
                {
                    entity.Id = Guid.NewGuid();
                }
                else if (item is EntityComponent component)
                {
                    component.Id = Guid.NewGuid();
                }
            }
        }
    }
}
