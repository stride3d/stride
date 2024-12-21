// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;
using System.Collections.Generic;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticMeshColliderShapeDesc>))]
    [DataContract("StaticMeshColliderShapeDesc")]
    [Display(600, "Static Mesh")]
    public class StaticMeshColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Model asset from which the engine will create the collider.
        /// </userdoc>
        [DataMember(30)]
        public Model Model;

        public bool Match(object obj)
        {
            return obj is StaticMeshColliderShapeDesc concDesc && concDesc.Model == Model;
        }
        
        public ColliderShape CreateShape(IServiceRegistry services)
        {
            if (Model == null || Model.Meshes.Count == 0)
                return null;
            
            return new StaticMeshColliderShape(Model, services)
            {
                NeedsCustomCollisionCallback = true,
                Description = this,
            };
        }
    }
}