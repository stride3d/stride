// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace SpaceEscape.Background
{
    /// <summary>
    /// The class contains information needed to describe a collidable object.
    /// </summary>
    public class Obstacle
    {
        /// <summary>
        /// The list of bounding boxes used to determine the collision with the obstacle.
        /// </summary>
        public List<BoundingBox> BoundingBoxes = new List<BoundingBox>(); 

        /// <summary>
        /// The entity representing the collidable object.
        /// </summary>
        public Entity Entity { get; set; }
    }
}
