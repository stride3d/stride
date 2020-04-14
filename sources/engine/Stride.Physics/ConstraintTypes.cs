// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Physics
{
    public enum ConstraintTypes
    {
        /// <summary>
        ///     The translation vector of the matrix to create this will represent the pivot, the rest is ignored
        /// </summary>
        Point2Point,

        Hinge,

        Slider,

        ConeTwist,

        Generic6DoF,

        Generic6DoFSpring,

        /// <summary>
        ///     The translation vector of the matrix to create this will represent the axis, the rest is ignored
        /// </summary>
        Gear,
    }
}
