// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Navigation
{
    /// <summary>
    /// Result for a raycast query on a navigation mesh
    /// </summary>
    public struct NavigationRaycastResult
    {
        /// <summary>
        /// true if the raycast hit something
        /// </summary>
        public bool Hit;

        /// <summary>
        /// Position where the ray hit a non-walkable area boundary
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Normal of the non-walkable area boundary that was hit
        /// </summary>
        public Vector3 Normal;
    }
}
