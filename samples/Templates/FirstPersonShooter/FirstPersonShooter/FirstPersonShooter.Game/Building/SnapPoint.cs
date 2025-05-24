// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace FirstPersonShooter.Building
{
    /// <summary>
    /// Defines a point on a building piece where other pieces can snap to.
    /// </summary>
    public struct SnapPoint
    {
        /// <summary>
        /// Position relative to the building piece's origin.
        /// </summary>
        public Vector3 LocalOffset;

        /// <summary>
        /// Orientation of the snap point (e.g., for aligning walls).
        /// </summary>
        public Quaternion LocalRotation;

        /// <summary>
        /// Type of snap point, e.g., "FoundationEdge", "WallSocket", "SurfaceTop".
        /// </summary>
        public string Type;

        // Optional: Could add a field for allowed connection types or sizes.
        // public SnapPointType AllowedConnections; 
    }
}
