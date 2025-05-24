// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.Building.Pieces
{
    public class WallPiece : BaseBuildingPiece
    {
        private float health = 300f;
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

        // Wall dimensions (example: 2 units wide, 3 units high, 0.2 units thick)
        // Assume origin is at the bottom-center of the wall.
        private const float WallWidth = 2.0f;  // X-axis
        private const float WallHeight = 3.0f; // Y-axis
        private const float WallThickness = 0.2f; // Z-axis

        public override void InitializeSnapPoints()
        {
            SnapPoints.Clear();

            float halfWidth = WallWidth / 2.0f;
            float halfThickness = WallThickness / 2.0f;

            // Bottom edge center (to connect to foundations or other wall tops)
            // Snaps with its back face (-Z) aligned with the surface it's placed on.
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(0, 0, 0), // Origin is bottom-center
                LocalRotation = Quaternion.Identity, // Aligns with parent snap point's rotation
                Type = "WallBottom"
            });

            // Top edge center (for ceilings or stacking another wall)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(0, WallHeight, 0),
                LocalRotation = Quaternion.Identity,
                Type = "WallTop"
            });
            
            // Side edges for connecting other walls (mid-height)
            // Left side (-X)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(-halfWidth, WallHeight / 2f, 0),
                LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(-90)), // Pointing outwards from left
                Type = "WallSide"
            });

            // Right side (+X)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(halfWidth, WallHeight / 2f, 0),
                LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(90)), // Pointing outwards from right
                Type = "WallSide"
            });

            // Optional: Snap points on the faces for attaching items to walls (e.g., lights, decorations)
            // Front face (+Z, assuming wall is placed with -Z against foundation edge)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(0, WallHeight / 2f, halfThickness),
                LocalRotation = Quaternion.Identity, // Facing "out" of the wall's front face
                Type = "WallSurface"
            });
            // Back face (-Z)
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = new Vector3(0, WallHeight / 2f, -halfThickness),
                LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(180)), // Facing "out" of the wall's back face
                Type = "WallSurface"
            });
        }
    }
}
