// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine.Design;

namespace Xenko.Engine
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
            return newPrefab.Entities;
        }

        private Entity packed;

    /// <summary>
    /// Converts a Prefab into a single Entity that has all entities as children. Makes it easier to use with an EntityPool
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public Entity PackToEntity() {
        if (packed == null) {
            List<Entity> roots = new List<Entity>();
            for (int i = 0; i < Entities.Count; i++) {
                if (Entities[i].Transform.Parent == null)
                    roots.Add(Entities[i]);
            }
            if (roots.Count == 1) {
                packed = roots[0];
            } else {
                packed = new Entity();
                for (int i = 0; i < roots.Count; i++) {
                    roots[i].Transform.Parent = packed.Transform;
                }
            }
        }
        return packed;
    }

    }
}
