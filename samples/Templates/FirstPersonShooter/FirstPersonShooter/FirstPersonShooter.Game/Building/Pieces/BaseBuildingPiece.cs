// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType
// No direct reference to SnapPoint here as it's used by derived classes in their InitializeSnapPoints or by BuildingPlacementController

namespace FirstPersonShooter.Building.Pieces
{
    public abstract class BaseBuildingPiece : ScriptComponent
    {
        /// <summary>
        /// The health of this building piece. Must be implemented by derived classes.
        /// </summary>
        public abstract float Health { get; set; }

        /// <summary>
        /// The material type of this structure, for impact sounds or damage calculations. Must be implemented by derived classes.
        /// </summary>
        public abstract MaterialType StructureMaterialType { get; set; }

        /// <summary>
        /// List of points where other building pieces can snap to this one.
        /// Populated by InitializeSnapPoints in derived classes.
        /// </summary>
        public List<SnapPoint> SnapPoints { get; protected set; } = new List<SnapPoint>();

        /// <summary>
        /// Abstract method to be implemented by derived classes to define their specific snap points.
        /// </summary>
        public abstract void InitializeSnapPoints();

        public List<BaseBuildingPiece> ConnectedPieces { get; private set; } = new List<BaseBuildingPiece>();

        public override void Start()
        {
            // Ensure SnapPoints list is initialized if derived class didn't do it (though it should)
            if (SnapPoints == null)
            {
                SnapPoints = new List<SnapPoint>();
            }
            InitializeSnapPoints();
            // Log.Info($"{GetType().Name} '{Entity?.Name ?? "Unnamed"}' initialized with {SnapPoints.Count} snap points.");
        }

        public void AddConnection(BaseBuildingPiece otherPiece)
        {
            if (otherPiece != null && !ConnectedPieces.Contains(otherPiece))
            {
                ConnectedPieces.Add(otherPiece);
                Log.Info($"{this.Entity?.Name ?? GetType().Name} connected to {otherPiece.Entity?.Name ?? otherPiece.GetType().Name}.");
            }
        }

        public void RemoveConnection(BaseBuildingPiece otherPiece)
        {
            if (otherPiece != null && ConnectedPieces.Contains(otherPiece))
            {
                ConnectedPieces.Remove(otherPiece);
                Log.Info($"{this.Entity?.Name ?? GetType().Name} disconnected from {otherPiece.Entity?.Name ?? otherPiece.GetType().Name}.");
            }
        }

        public virtual void OnPieceDestroyed()
        {
            Log.Info($"{this.Entity?.Name ?? GetType().Name} OnPieceDestroyed called. Notifying {ConnectedPieces.Count} connected pieces.");
            var piecesToNotify = new List<BaseBuildingPiece>(ConnectedPieces); // Iterate over a copy

            foreach (var piece in piecesToNotify)
            {
                piece.RemoveConnection(this);
                // Future: piece.CheckStructuralIntegrity(); or similar
            }
            ConnectedPieces.Clear();
            // Future: This is where structural integrity checks would begin if this piece was a support.
            // For example, if this piece was foundational and now has no support itself,
            // it might trigger a cascade of checks on pieces it was supporting.
        }

        public void Debug_ForceDestroy()
        {
            Log.Info($"Debug_ForceDestroy called on {this.Entity?.Name ?? GetType().Name}.");
            // Simulate health depletion or set a flag
            // For simplicity, we'll just call OnPieceDestroyed and remove the entity.
            // In a full system, setting Health = 0 would ideally trigger TakeDamage -> OnDestroyed.
            
            OnPieceDestroyed(); // Notify connections

            // Optionally, disable or remove the entity
            if (this.Entity != null && this.Entity.Scene != null)
            {
                Log.Info($"Removing {this.Entity.Name} from scene due to Debug_ForceDestroy.");
                this.Entity.Scene = null; 
            }
            // Or disable components:
            // this.Enabled = false;
            // this.Entity.Get<ModelComponent>()?.Enabled = false;
            // this.Entity.Get<ColliderComponent>()?.Enabled = false;
        }

        // Future common methods for all building pieces:
        // public virtual void TakeDamage(float amount, Entity attacker) { /* ... Health -= amount; if (Health <=0) OnDestroyed(); ... */ }
        // public virtual void Repair(float amount) { /* ... */ }
    }
}
