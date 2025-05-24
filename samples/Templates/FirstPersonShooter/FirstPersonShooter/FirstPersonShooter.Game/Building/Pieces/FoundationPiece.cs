// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic; // For List
using Stride.Core.Mathematics; // For Vector3, Quaternion, MathUtil
using Stride.Core.Mathematics; // For Vector3, Quaternion, MathUtil
using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.Building.Pieces
{
    public class FoundationPiece : BaseBuildingPiece
    {
        private float health = 500f;
        private MaterialType structureMaterialType = MaterialType.Wood;

        public FoundationPiece() // Constructor
        {
            this.IsGroundPiece = true; // Foundations can be ground anchors
        }

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

        // Assuming foundation is a 2x2x0.5 cube (Length x Width x Height) with origin at bottom-center.
        private const float PieceWidth = 2.0f;
        private const float PieceDepth = 2.0f;
        private const float PieceHeight = 0.5f; 

        public override void InitializeSnapPoints()
        {
            SnapPoints.Clear(); // Ensure it's clean if Start is called multiple times (though not typical for scripts)

            float halfWidth = PieceWidth / 2.0f;
            float halfDepth = PieceDepth / 2.0f;
            float topSurfaceY = PieceHeight; // Y position of the top surface relative to piece origin

            // Snap points on top surface corners (for stacking foundations or other corner items)
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(-halfWidth, topSurfaceY, -halfDepth), LocalRotation = Quaternion.Identity, Type = "FoundationTopCorner" });
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(halfWidth, topSurfaceY, -halfDepth), LocalRotation = Quaternion.Identity, Type = "FoundationTopCorner" });
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(-halfWidth, topSurfaceY, halfDepth), LocalRotation = Quaternion.Identity, Type = "FoundationTopCorner" });
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(halfWidth, topSurfaceY, halfDepth), LocalRotation = Quaternion.Identity, Type = "FoundationTopCorner" });

            // Snap points on top surface mid-edges (primarily for walls or other edge-aligning pieces)
            // Front edge (-Z direction if Z is forward)
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(0, topSurfaceY, -halfDepth), LocalRotation = Quaternion.Identity, Type = "FoundationEdge" });
            // Back edge (+Z direction)
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(0, topSurfaceY, halfDepth), LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(180)), Type = "FoundationEdge" });
            // Left edge (-X direction)
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(-halfWidth, topSurfaceY, 0), LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(-90)), Type = "FoundationEdge" });
            // Right edge (+X direction)
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(halfWidth, topSurfaceY, 0), LocalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(90)), Type = "FoundationEdge" });
            
            // Optional: Snap point at the center of the top surface (for pillars, etc.)
            SnapPoints.Add(new SnapPoint { LocalOffset = new Vector3(0, topSurfaceY, 0), LocalRotation = Quaternion.Identity, Type = "FoundationTopCenter" });
        }

        // Future methods:
        // public void TakeDamage(float amount) { /* ... */ }
        // public void Repair(float amount) { /* ... */ }
        // public void OnDestroyed() { /* ... */ }
    }
}
