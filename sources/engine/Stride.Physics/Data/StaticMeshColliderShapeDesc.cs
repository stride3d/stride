// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;
using System.Collections.Generic;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticMeshColliderShapeDesc>))]
    [DataContract("StaticMeshColliderShapeDesc")]
    [Display(600, "StaticMesh")]
    public class StaticMeshColliderShapeDesc : IAssetColliderShapeDesc
    {

        [Display(Browsable = false)]
        [DataMember(10)]
        [NotNull]
        public List<Vector3> Faces { get; set; } = new List<Vector3>();

        /// Model asset from where the engine will derive the convex hull.
        /// </userdoc>
        [DataMember(30)]
        public Model Model;

        /// <userdoc>
        /// The offset with the real graphic mesh.
        /// </userdoc>
        [DataMember(31)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(32)]
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <userdoc>
        /// The scaling of the generated convex hull.
        /// </userdoc>
        [DataMember(45)]
        public Vector3 Scaling = Vector3.One;

        public bool Match(object obj)
        {
            return obj is StaticMeshColliderShapeDesc concDesc
                   && concDesc.Model == Model
                   && concDesc.LocalOffset == LocalOffset
                   && concDesc.LocalRotation == LocalRotation
                   && concDesc.Scaling == Scaling;
        }

        public ColliderShape CreateShape()
        {
            if (Faces == null || Faces.Count <= 0)
            {
                return null;
            }

            return new StaticMeshColliderShape(Faces, Scaling)
            {
                NeedsCustomCollisionCallback = true,
            };
        }
    }
}
