// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.DebugDraw;

namespace Stride.Particles.BoundingShapes
{
    [DataContract("BoundingSpheretatic")]
    [Display("Uniform AABB")]
    public class BoundingSphereStatic : BoundingShape
    {
        /// <summary>
        /// Fixed radius of the <see cref="BoundingSphereStatic"/>
        /// </summary>
        /// <userdoc>
        /// Fixed radius of the bounding sphere. Gets calculated as a AABB, which is a cube with corners (-R, -R, -R) - (+R, +R, +R)
        /// </userdoc>
        [DataMember(20)]
        [Display("Distance")]
        public float Radius { get; set; } = 1f;

        [DataMemberIgnore]
        private BoundingBox cachedBox;
        
        public override BoundingBox GetAABB(Vector3 translation, Quaternion rotation, float scale)
        {
            if (Dirty)
            {
                var r = Radius*scale;

                cachedBox = new BoundingBox(new Vector3(-r, -r, -r) + translation, new Vector3(r, r, r) + translation);
            }

            return cachedBox;
        }

        public override bool TryGetDebugDrawShape(out DebugDrawShape debugDrawShape, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            if (!DebugDraw)
                return base.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale);

            debugDrawShape = DebugDrawShape.Cube;
            scale = new Vector3(Radius, Radius, Radius);
            translation = Vector3.Zero;
            rotation = Quaternion.Identity;
            return true;
        }

    }
}
