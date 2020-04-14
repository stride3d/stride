// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Physics;

namespace Stride.Navigation
{
    /// <summary>
    /// Holds the cached result of building a scene into a navigation mesh, with input vertex data to allow incremental builds.
    /// </summary>
    [DataContract]
    internal class NavigationMeshCache
    {
        /// <summary>
        /// State of static colliders by their component Id that was used for building
        /// </summary>
        public Dictionary<Guid, NavigationMeshCachedObject> Objects =
            new Dictionary<Guid, NavigationMeshCachedObject>();
        
        /// <summary>
        /// The bounding boxes used for build
        /// </summary>
        public List<BoundingBox> BoundingBoxes = new List<BoundingBox>();
        
        /// <summary>
        /// Hash for the building settings used
        /// </summary>
        public int SettingsHash = 0;

        /// <summary>
        /// Registers a new processed object that is build into the navigation mesh
        /// </summary>
        /// <param name="collider">The collider that was processed</param>
        /// <param name="data">The collider vertex data that is generated for this entity</param>
        /// <param name="planes">Collection of infinite planes for this colliders, these are special since their size is not known until the bounding box are known</param>
        /// <param name="entityColliderHash">The hash of the entity and collider obtained with <see cref="NavigationMeshBuildUtils.HashEntityCollider"/></param>
        public void Add(StaticColliderComponent collider, NavigationMeshInputBuilder data, ICollection<Plane> planes, int entityColliderHash)
        {
            Objects.Add(collider.Id, new NavigationMeshCachedObject()
            {
                Guid = collider.Id,
                ParameterHash = entityColliderHash,
                Planes = new List<Plane>(planes),
                InputBuilder = data,
            });
        }
    }
}
