// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Physics
{
    public enum ColliderShapeTypes
    {
        /// <summary>
        ///     3D and 2D ( a plane )
        /// </summary>
        Box,

        /// <summary>
        ///     3D and 2D ( a circle )
        /// </summary>
        Sphere,

        /// <summary>
        ///     3D only
        /// </summary>
        Cylinder,

        /// <summary>
        ///     3D and 2D
        /// </summary>
        Capsule,

        ConvexHull,

        Compound,

        StaticPlane,

        StaticMesh,

        Cone,

        Heightfield,
    }
}
