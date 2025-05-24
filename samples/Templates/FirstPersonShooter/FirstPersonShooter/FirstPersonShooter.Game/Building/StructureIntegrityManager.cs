// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Engine;
using FirstPersonShooter.Building.Pieces; // For BaseBuildingPiece

namespace FirstPersonShooter.Building
{
    public class StructureIntegrityManager : SyncScript
    {
        public static StructureIntegrityManager Instance { get; private set; }

        public override void Start()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(this.Entity); // Optional: if manager should persist across scenes
            }
            else
            {
                Log.Warning("StructureIntegrityManager: Another instance already exists. Destroying this one.");
                // Destroy(this.Entity); // Destroy the whole entity
                this.Entity.Remove(this); // Just remove this script component
            }
        }

        public override void Cancel() // Called when script is removed or entity is destroyed
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Updates the IsAnchored status for all pieces within the structure connected to the given piece.
        /// </summary>
        /// <param name="pieceInStructure">A piece that is part of the structure to be updated.</param>
        public void UpdateAnchorStatusForStructure(BaseBuildingPiece pieceInStructure)
        {
            if (pieceInStructure == null)
            {
                Log.Error("UpdateAnchorStatusForStructure: pieceInStructure is null.");
                return;
            }

            // Step 1: Identify all connected pieces (the "structure").
            var structurePieces = new HashSet<BaseBuildingPiece>();
            var piecesToVisit = new Queue<BaseBuildingPiece>();

            piecesToVisit.Enqueue(pieceInStructure);
            structurePieces.Add(pieceInStructure);

            while (piecesToVisit.Count > 0)
            {
                var currentPiece = piecesToVisit.Dequeue();
                foreach (var connectedPiece in currentPiece.ConnectedPieces)
                {
                    if (connectedPiece != null && structurePieces.Add(connectedPiece)) // Add returns true if item was added (not already present)
                    {
                        piecesToVisit.Enqueue(connectedPiece);
                    }
                }
            }

            // Step 2: Reset anchor status for this entire structure.
            foreach (var piece in structurePieces)
            {
                piece.IsAnchored = false;
            }

            // Step 3: Re-propagate anchor status from ground-anchored pieces within this structure.
            var anchorPropagationQueue = new Queue<BaseBuildingPiece>();
            int anchoredCount = 0;

            // Find initial anchors within the identified structure
            foreach (var piece in structurePieces)
            {
                // A piece is an initial anchor if it's a ground piece AND it's considered on the ground.
                // The "on the ground" part is tricky here. We assume that if IsGroundPiece is true,
                // and it was placed without snapping OR it was snapped to an already anchored piece,
                // it would have had IsAnchored = true set by BuildingPlacementController.
                // For this re-evaluation, we primarily rely on IsGroundPiece.
                // A more robust check for "on the ground" might involve a raycast downwards if it has no supporting connections.
                // For now: if it's a ground piece and its current IsAnchored state (before reset) was true, it's a root.
                // OR, more simply, if it's a ground piece and it's not supported by anything else in *this specific structure*
                // that isn't also a ground piece. This simplified version assumes IsGroundPiece = true means it can *be* an anchor.
                // The BuildingPlacementController sets initial IsAnchored for ground pieces on terrain.
                
                // For this pass, we consider a piece a valid root for anchor propagation if:
                // 1. It's a "GroundPiece" (e.g., a foundation).
                // 2. AND it *was* considered anchored before this whole structure reset.
                //    This means it was either placed on terrain or connected to an already anchored structure.
                //    BuildingPlacementController's `newlyPlacedPieceScript.IsAnchored = true;` for ground pieces
                //    placed on terrain handles the initial state.
                //    If a structure is now detached, this logic will correctly de-anchor it unless it has its own ground pieces.

                // Let's refine: a piece in the structure becomes an anchor source if it's a GroundPiece
                // AND (it has no connections OR all its connections are to pieces that also need it for support).
                // Simpler: if IsGroundPiece is true, it *can* be an anchor source if it's effectively on the ground.
                // The BuildingPlacementController sets initial IsAnchored for ground pieces on terrain.
                // When a piece is destroyed, we re-evaluate. If a piece is IsGroundPiece and still connected to "ground"
                // (which means it must have been placed there initially or connected to another ground piece), it can re-anchor.

                // The most straightforward way is: if a piece is IsGroundPiece, it can start an anchor chain.
                // The initial IsAnchored = true for ground pieces placed on terrain is handled by BuildingPlacementController.
                // So, if a piece is IsGroundPiece and its IsAnchored was true (from placement or previous update), it can start a chain.
                // The reset in Step 2 clears this. So, we need to identify pieces that *should* be ground-connected.
                // The most reliable way is to assume any IsGroundPiece can start an anchor chain if it's part of the structure.
                // The placement controller should have set IsAnchored = true for ground pieces on terrain.
                // If a piece is IsGroundPiece and not connected to anything below it that supports it, it's a ground anchor.
                // This is becoming complex. Let's use the rule: if piece.IsGroundPiece is true, it's a potential source.
                // The placement controller is responsible for setting IsAnchored = true for the *first* ground piece.
                // This means, if a piece is IsGroundPiece AND was previously anchored (before reset), it's a valid starting point.
                // This is still tricky. Let's assume for now: If a piece in the structure is IsGroundPiece, it *can* be an anchor.
                // The check for whether it *actually* is (i.e. touching ground) is implicitly handled by initial placement.

                // A piece is a root anchor if it's a GroundPiece AND (it was placed on terrain OR it's connected to another piece that's already confirmed as an anchor source in this pass)
                // The most robust for re-evaluation: iterate ground pieces. If a ground piece has no *supporting* connections (connections below it),
                // or if its connections are also ground pieces that are themselves anchored, it's anchored.

                // Simplified for this implementation: Any IsGroundPiece within the structure starts as an anchor.
                // The BuildingPlacementController is responsible for the initial IsAnchored = true for pieces placed on terrain.
                // If a piece is IsGroundPiece and was previously anchored, it's a valid starting anchor.
                // After the reset, if IsGroundPiece is true, it can start a new chain.
                if (piece.IsGroundPiece) // All ground pieces in the structure are potential starting points for anchoring
                {
                    piece.IsAnchored = true; // Re-anchor it
                    anchorPropagationQueue.Enqueue(piece);
                    anchoredCount++;
                }
            }
            
            if(anchorPropagationQueue.Count == 0 && structurePieces.Count > 0)
            {
                Log.Warning($"StructureIntegrityManager: Structure containing {pieceInStructure.Entity?.Name ?? "Unknown"} has no ground pieces. It will not be anchored.");
                // All pieces remain IsAnchored = false. Future: they should be destroyed.
                // For now, just logging. Destruction logic will be separate.
                foreach (var piece in structurePieces)
                {
                    if (piece.IsAnchored) Log.Error("Error in logic: piece is anchored but no ground pieces found as start."); // Should not happen
                    // piece.MarkForDestruction(); // Conceptual
                }
                Log.Info($"StructureIntegrityManager: Update complete for {pieceInStructure.Entity?.Name ?? "Unknown"}. Structure pieces: {structurePieces.Count}, Anchored: {anchoredCount}. Structure is FLOATING.");
                return;
            }


            while (anchorPropagationQueue.Count > 0)
            {
                var currentPiece = anchorPropagationQueue.Dequeue();
                // currentPiece.IsAnchored is already true if it's in this queue

                foreach (var neighbor in currentPiece.ConnectedPieces)
                {
                    if (neighbor != null && structurePieces.Contains(neighbor) && !neighbor.IsAnchored)
                    {
                        neighbor.IsAnchored = true;
                        anchorPropagationQueue.Enqueue(neighbor);
                        anchoredCount++;
                    }
                }
            }
            Log.Info($"StructureIntegrityManager: Anchor propagation complete for structure starting with {pieceInStructure.Entity?.Name ?? "Unknown"}. Total pieces in structure: {structurePieces.Count}, Anchored: {anchoredCount}.");

            // Step 4: Collapse unanchored pieces within this structure.
            if (anchoredCount < structurePieces.Count) // Only proceed if there are unanchored pieces
            {
                var piecesToDestroy = new List<BaseBuildingPiece>();
                foreach (var piece in structurePieces)
                {
                    if (!piece.IsAnchored)
                    {
                        piecesToDestroy.Add(piece);
                    }
                }

                if (piecesToDestroy.Count > 0)
                {
                    Log.Info($"StructureIntegrityManager: Found {piecesToDestroy.Count} unanchored pieces in structure of {pieceInStructure.Entity?.Name ?? "Unknown"} to collapse.");
                    foreach (var pieceToCollapse in piecesToDestroy)
                    {
                        // Check if piece still exists in scene, as a previous Debug_ForceDestroy in a cascade might have already removed it.
                        if (pieceToCollapse.Entity != null && pieceToCollapse.Entity.Scene != null)
                        {
                            Log.Info($"Collapsing unanchored piece: {pieceToCollapse.Entity?.Name ?? "UnknownPiece"}. Was IsGroundPiece: {pieceToCollapse.IsGroundPiece}");
                            pieceToCollapse.Debug_ForceDestroy(); // This will call OnPieceDestroyed, potentially triggering more checks.
                        }
                        else
                        {
                            Log.Info($"Piece {pieceToCollapse.Entity?.Name ?? "UnknownPiece (already destroyed)"} marked for collapse was already removed from scene, skipping Debug_ForceDestroy.");
                        }
                    }
                }
            }
            else
            {
                Log.Info($"StructureIntegrityManager: All {structurePieces.Count} pieces in structure of {pieceInStructure.Entity?.Name ?? "Unknown"} are anchored. No collapse needed.");
            }
        }
    }
}
