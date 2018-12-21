// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Rendering;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticMeshColliderShapeDesc>))]
    [DataContract("StaticMeshColliderShapeDesc")]
    [Display(500, "Static Mesh")]
    public class StaticMeshColliderShapeDesc : IAssetColliderShapeDesc
    {
#if XENKO_PLATFORM_WINDOWS_DESKTOP

        [Display(Browsable = false)]
#endif
        [DataMember(10)]
        public List<List<List<Vector3>>> Points; // Multiple meshes -> Multiple Hulls -> Hull points

#if XENKO_PLATFORM_WINDOWS_DESKTOP

        [Display(Browsable = false)]
#endif
        [DataMember(20)]
        public List<List<List<uint>>> Indices; // Multiple meshes -> Multiple Hulls -> Hull tris

        /// <userdoc>
        /// Model asset from where the engine will derive the collider shape.
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
        /// The scaling of the collider shape.
        /// </userdoc>
        [DataMember(45)]
        public Vector3 Scaling = Vector3.One;


        public bool Match(object obj)
        {
            var other = obj as StaticMeshColliderShapeDesc;
            if (other == null)
                return false;

            if (other.LocalOffset != LocalOffset || other.LocalRotation != LocalRotation)
                return false;

            return other.Model == Model &&
                   other.Scaling == Scaling;
        }

        public ColliderShape NewShapeFromDesc()
        {
            if (Points == null) return null;
            

            if (Points.Count == 1)
            {
                if (Points[0].Count == 1 && Indices[0][0].Count > 0)
                {
                    var shape = new StaticMeshColliderShape(Points[0][0], Indices[0][0], Scaling)
                    {
                        NeedsCustomCollisionCallback = true,
                    };

                    //shape.UpdateLocalTransformations();
                    shape.Description = this;

                    return shape;
                }

                if (Points[0].Count <= 1) return null;

                var subCompound = new CompoundColliderShape
                {
                    NeedsCustomCollisionCallback = true,
                };

                for (var i = 0; i < Points[0].Count; i++)
                {
                    var verts = Points[0][i];
                    var indices = Indices[0][i];

                    if (indices.Count == 0) continue;

                    var subHull = new StaticMeshColliderShape(verts, indices, Scaling);
                    //subHull.UpdateLocalTransformations();
                    subCompound.AddChildShape(subHull);
                }

                //subCompound.UpdateLocalTransformations();
                subCompound.Description = this;

                return subCompound;
            }

            if (Points.Count <= 1) return null;

            var compound = new CompoundColliderShape
            {
                NeedsCustomCollisionCallback = true,
            };

            for (var i = 0; i < Points.Count; i++)
            {
                var verts = Points[i];
                var indices = Indices[i];

                if (verts.Count == 1)
                {
                    if (indices[0].Count == 0) continue;

                    var subHull = new StaticMeshColliderShape(verts[0], indices[0], Scaling);
                    //subHull.UpdateLocalTransformations();
                    compound.AddChildShape(subHull);
                }
                else if (verts.Count > 1)
                {
                    var subCompound = new CompoundColliderShape();

                    for (var b = 0; b < verts.Count; b++)
                    {
                        var subVerts = verts[b];
                        var subIndex = indices[b];

                        if (subIndex.Count == 0) continue;

                        var subHull = new StaticMeshColliderShape(subVerts, subIndex, Scaling);
                        //subHull.UpdateLocalTransformations();
                        subCompound.AddChildShape(subHull);
                    }

                    //subCompound.UpdateLocalTransformations();

                    compound.AddChildShape(subCompound);
                }
            }

            //compound.UpdateLocalTransformations();
            compound.Description = this;

            return compound;
        }
    }
}
