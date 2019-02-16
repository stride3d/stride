// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Rendering;

namespace Xenko.Physics
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
    }
}
