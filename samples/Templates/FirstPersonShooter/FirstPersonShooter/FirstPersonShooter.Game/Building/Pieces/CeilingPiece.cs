// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.Building.Pieces
{
    public class CeilingPiece : BaseBuildingPiece
    {
        private float health = 250f;
        private MaterialType structureMaterialType = MaterialType.Wood;

        public override float Health
        {
            get => health;
            set => health = value;
        }

        public override MaterialType StructureMaterialType
        {
            get => structureMaterialType;
            set => structureMaterialType = value;
        }

        // Ceiling dimensions (example: 2x2 units wide/deep, 0.2 units thick)
        // Assume origin is at the bottom-center of the ceiling piece.
        private const float CeilingWidth = 2.0f;  // X-axis
        private const float CeilingDepth = 2.0f;  // Z-axis
        private const float CeilingThickness = 0.2f; // Y-axis

        public override void InitializeSnapPoints()
        {
            SnapPoints.Clear();

            float halfWidth = CeilingWidth / 2.0f;
            float halfDepth = CeilingDepth / 2.0f;
            // float topY = CeilingThickness; // If origin is bottom-center
            // float bottomY = 0;

            // Snap points on the bottom edges to connect to WallTops or other CeilingEdges
            // These points are on the underside of the ceiling piece.
            // Rotations are set so that their "forward" (typically +Z in local space of snap point)
            // points outwards from the edge.

            // Edge along +X axis (at Z = 0, if origin is center-center)
            // If origin is bottom-center, Z is relative to that.
            // Let's assume bottom-center for consistency with Foundation/Wall.
            // LocalOffset Y will be 0 for bottom surface connections.

            // Front edge (Local -Z)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(0, 0, -halfDepth),
                LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(0)), // Facing -Z
                Type = "CeilingEdge"
            });
            // Back edge (Local +Z)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(0, 0, halfDepth),
                LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(180)), // Facing +Z
                Type = "CeilingEdge"
            });
            // Left edge (Local -X)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(-halfWidth, 0, 0),
                LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(-90)), // Facing -X
                Type = "CeilingEdge"
            });
            // Right edge (Local +X)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(halfWidth, 0, 0),
                LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(90)), // Facing +X
                Type = "CeilingEdge"
            });

            // Optional: Snap point on top surface center (for stacking something on the ceiling)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(0, CeilingThickness, 0), // Top surface center
                LocalRotation = Quaternion.Identity,
                Type = "CeilingTopCenter"
            });
        }
    }
}
