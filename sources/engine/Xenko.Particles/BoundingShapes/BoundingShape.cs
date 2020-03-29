// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.DebugDraw;

namespace Xenko.Particles.BoundingShapes
{
    [DataContract("BoundingShape")]
    public abstract class BoundingShape
    {
        [DataMemberIgnore]
        public bool Dirty { get; set; } = true;

        // ReSharper disable once InconsistentNaming
        public abstract BoundingBox GetAABB(Vector3 translation, Quaternion rotation, float scale);

        /// <summary>
        /// Should the Bounding shape's bounds be displayed as a debug draw
        /// </summary>
        /// <userdoc>
        /// Display the Bounding shape's boinds as a wireframe debug shape. Temporary feature (will be removed later)!
        /// </userdoc>
        [DataMember(-1)]
        [DefaultValue(false)]
        public bool DebugDraw { get; set; } = false;

        public virtual bool TryGetDebugDrawShape(out DebugDrawShape debugDrawShape, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            debugDrawShape = DebugDrawShape.None;
            scale = Vector3.One;
            translation = Vector3.Zero;
            rotation = Quaternion.Identity;
            return false;
        }
    }
}
