// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;

namespace SpaceEscape.Background
{
    /// <summary>
    /// This class contains information needed to describe a hole in the background.
    /// </summary>
    [DataContract("BackgroundElement")]
    public class Hole
    {
        /// <summary>
        /// The area of the hole.
        /// </summary>
        public RectangleF Area { get; set; }

        /// <summary>
        /// The height of the hole.
        /// </summary>
        public float Height { get; set; }
    }
}
