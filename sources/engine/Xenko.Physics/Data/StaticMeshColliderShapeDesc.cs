// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Xenko.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<StaticMeshColliderShapeDesc>))]
    [DataContract("StaticMeshColliderShapeDesc")]
    [Display(500, "Static Mesh")]
    public class StaticMeshColliderShapeDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// Model asset from which the engine will derive the collider shape.
        /// </userdoc>
        [DataMember(10)]
        public Model Model;

        /// <userdoc>
        /// The local offset of the collider shape.
        /// </userdoc>
        [DataMember(20)]
        public Vector3 LocalOffset;

        /// <userdoc>
        /// The local rotation of the collider shape.
        /// </userdoc>
        [DataMember(30)]
        public Quaternion LocalRotation = Quaternion.Identity;

        /// <userdoc>
        /// The scaling of the collider shape.
        /// </userdoc>
        [DataMember(40)]
        public Vector3 Scaling = Vector3.One;


        public bool Match(object obj)
        {
            if (obj is StaticMeshColliderShapeDesc other)
            {
                return other.Model == Model
                    && other.LocalOffset == LocalOffset
                    && other.LocalRotation == LocalRotation 
                    && other.Scaling == Scaling;
            }

            return false;
        }

        public ColliderShape CreateShape()
        {
            if(Model == null)
                return null;

            return StaticMeshColliderShape.FromModel(Model, LocalOffset, LocalRotation, Scaling);
        }
    }
}
