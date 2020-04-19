// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Rendering;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<ConvexHullColliderShapeDesc>))]
    [DataContract("ConvexHullColliderShapeDesc")]
    [Display(500, "Convex Hull")]
    public class ConvexHullColliderShapeDesc : IAssetColliderShapeDesc
    {

        [Display(Browsable = false)]
        [DataMember(10)]
        public List<List<List<Vector3>>> ConvexHulls; // Multiple meshes -> Multiple Hulls -> Hull points

        [Display(Browsable = false)]
        [DataMember(20)]
        public List<List<List<uint>>> ConvexHullsIndices; // Multiple meshes -> Multiple Hulls -> Hull tris

        /// <userdoc>
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

        /// <userdoc>
        /// If this is not checked, the contained parameters are ignored and only a simple convex hull of the model will be generated.
        /// </userdoc>
        [DataMember(50)]
        [NotNull]
        public ConvexHullDecompositionParameters Decomposition { get; set; } = new ConvexHullDecompositionParameters();

        public bool Match(object obj)
        {
            var other = obj as ConvexHullColliderShapeDesc;
            if (other == null)
                return false;

            if (other.LocalOffset != LocalOffset || other.LocalRotation != LocalRotation)
                return false;

            return other.Model == Model &&
                   other.Scaling == Scaling &&
                   other.Decomposition.Match(Decomposition);
        }

        public ColliderShape CreateShape()
        {
            if (ConvexHulls == null) return null;
            ColliderShape shape;

            //Optimize performance and focus on less shapes creation since this shape could be nested

            if (ConvexHulls.Count == 1)
            {
                if (ConvexHulls[0].Count == 1 && ConvexHullsIndices[0][0].Count > 0)
                {
                    shape = new ConvexHullColliderShape(ConvexHulls[0][0], ConvexHullsIndices[0][0], Scaling)
                    {
                        NeedsCustomCollisionCallback = true,
                    };

                    //shape.UpdateLocalTransformations();
                    shape.Description = this;

                    return shape;
                }

                if (ConvexHulls[0].Count <= 1) return null;

                var subCompound = new CompoundColliderShape
                {
                    NeedsCustomCollisionCallback = true,
                };

                for (var i = 0; i < ConvexHulls[0].Count; i++)
                {
                    var verts = ConvexHulls[0][i];
                    var indices = ConvexHullsIndices[0][i];

                    if (indices.Count == 0) continue;

                    var subHull = new ConvexHullColliderShape(verts, indices, Scaling);
                    //subHull.UpdateLocalTransformations();
                    subCompound.AddChildShape(subHull);
                }

                //subCompound.UpdateLocalTransformations();
                subCompound.Description = this;

                return subCompound;
            }

            if (ConvexHulls.Count <= 1) return null;

            var compound = new CompoundColliderShape
            {
                NeedsCustomCollisionCallback = true,
            };

            for (var i = 0; i < ConvexHulls.Count; i++)
            {
                var verts = ConvexHulls[i];
                var indices = ConvexHullsIndices[i];

                if (verts.Count == 1)
                {
                    if (indices[0].Count == 0) continue;

                    var subHull = new ConvexHullColliderShape(verts[0], indices[0], Scaling);
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

                        var subHull = new ConvexHullColliderShape(subVerts, subIndex, Scaling);
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
