// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;

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
