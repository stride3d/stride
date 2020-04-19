// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    [ContentSerializer(typeof(DataContentSerializer<ColliderShapeAssetDesc>))]
    [DataContract("ColliderShapeAssetDesc")]
    [Display(50, "Asset")]
    public class ColliderShapeAssetDesc : IInlineColliderShapeDesc
    {
        /// <userdoc>
        /// The reference to the collider Shape asset.
        /// </userdoc>
        [DataMember(10)]
        public PhysicsColliderShape Shape { get; set; }

        public bool Match(object obj)
        {
            var other = obj as ColliderShapeAssetDesc;
            if (other == null) return false;

            if (other.Shape == null || Shape == null)
                return other.Shape == Shape;

            if (other.Shape.Descriptions == null || Shape.Descriptions == null)
                return other.Shape.Descriptions == Shape.Descriptions;

            if (other.Shape.Descriptions.Count != Shape.Descriptions.Count)
                return false;

            if (other.Shape.Descriptions.Where((t, i) => !t.Match(Shape.Descriptions[i])).Any())
                return false;

            // TODO: shouldn't we return true here?
            return other.Shape == Shape;
        }

        public ColliderShape CreateShape()
        {
            if (Shape == null)
            {
                return null;
            }

            if (Shape.Shape == null)
            {
                Shape.Shape = PhysicsColliderShape.Compose(Shape.Descriptions);
            }

            return this.Shape.Shape;
        }
    }
}
