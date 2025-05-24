// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic; // For List
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Player; // For PlayerInput to get CameraComponent
using FirstPersonShooter.Building.Pieces; // For BaseBuildingPiece
using System.Linq; // Required for FirstOrDefault()

namespace FirstPersonShooter.Building
{
    public class BuildingPlacementController : SyncScript
    {
        public struct BuildableItem
        {
            public string Name;
            public Prefab PiecePrefab; // The actual piece to build
            public Prefab GhostPrefab;   // The ghost preview for this specific piece
        }

        public bool IsBuildingModeActive { get; private set; } = false;
        public List<BuildableItem> AvailableBuildableItems { get; set; } = new List<BuildableItem>();
        private int currentBuildableIndex = 0;
        private BuildableItem currentSelectedItem; 
        
        private Entity activeGhostEntity;
        public float MaxPlacementDistance { get; set; } = 10f;
        public float GridSnapSize { get; set; } = 1.0f;
        public float SnapRadius { get; set; } = 0.75f;
        public float RotationSnapAngle { get; set; } = 15.0f; // Degrees
        private float currentGhostRotationY = 0f;
        private bool canPlace = false;
        private CameraComponent playerCamera;
        private Simulation simulation;
        private BaseBuildingPiece lastSnappedToPiece = null; // For tracking connections

        public override void Start()
        {
            var playerInput = Entity.Get<PlayerInput>();
            if (playerInput != null && playerInput.Camera != null)
            {
                playerCamera = playerInput.Camera;
            }
            else
            {
                Log.Error("BuildingPlacementController: Player camera not found! Ensure PlayerInput component with an assigned Camera exists on the same entity.");
            }

            simulation = this.GetSimulation();
            if (simulation == null)
            {
                Log.Error("BuildingPlacementController: Physics simulation not found!");
            }

            if (AvailableBuildableItems == null || AvailableBuildableItems.Count == 0)
            {
                Log.Error("BuildingPlacementController: AvailableBuildableItems list is empty or null! Building mode will not function correctly.");
                return;
            }
            for(int i=0; i < AvailableBuildableItems.Count; ++i)
            {
                var item = AvailableBuildableItems[i];
                if (item.PiecePrefab == null || item.GhostPrefab == null)
                {
                    Log.Error($"BuildingPlacementController: BuildableItem '{item.Name ?? $"Unnamed Item at index {i}"}' has null PiecePrefab or GhostPrefab.");
                }
            }
            currentSelectedItem = AvailableBuildableItems[currentBuildableIndex]; 
            Log.Info($"Building controller initialized. Current item: {currentSelectedItem.Name}");
        }
        
        private void DestroyActiveGhost()
        {
            if (activeGhostEntity != null)
            {
                activeGhostEntity.Scene = null; 
                activeGhostEntity = null;
            }
        }

        private void CreateActiveGhost()
        {
            DestroyActiveGhost(); 

            if (currentSelectedItem.GhostPrefab == null) 
            {
                Log.Error($"Cannot create ghost for '{currentSelectedItem.Name}': GhostPrefab is not assigned.");
                return;
            }

            var instances = currentSelectedItem.GhostPrefab.Instantiate();
            if (instances == null || !instances.Any())
            {
                Log.Error($"Failed to instantiate GhostPrefab for '{currentSelectedItem.Name}'.");
                return;
            }
            activeGhostEntity = instances[0];

            if (this.Entity.Scene != null)
            {
                this.Entity.Scene.Entities.Add(activeGhostEntity);
            }
            else
            {
                Log.Error("BuildingPlacementController: Cannot add ghost to scene, controller's parent scene is null.");
                activeGhostEntity = null; 
                return;
            }
            currentGhostRotationY = 0f; 
        }

        public void ToggleBuildingMode()
        {
            IsBuildingModeActive = !IsBuildingModeActive;

            if (IsBuildingModeActive)
            {
                if (AvailableBuildableItems.Count == 0) 
                {
                    Log.Error("BuildingPlacementController: No buildable items available. Cannot activate building mode.");
                    IsBuildingModeActive = false; 
                    return;
                }
                if (currentSelectedItem.GhostPrefab == null)
                {
                     Log.Error($"BuildingPlacementController: GhostPrefab for current item '{currentSelectedItem.Name}' is null. Cannot activate building mode.");
                    IsBuildingModeActive = false; 
                    return;
                }
                CreateActiveGhost();
                Log.Info($"Building mode activated. Selected: {currentSelectedItem.Name}");
            }
            else
            {
                DestroyActiveGhost();
                Log.Info("Building mode deactivated.");
            }
        }
        
        public void CycleBuildableItem(bool next)
        {
            if (AvailableBuildableItems.Count == 0) return;

            currentBuildableIndex += (next ? 1 : -1);
            if (currentBuildableIndex >= AvailableBuildableItems.Count)
            {
                currentBuildableIndex = 0;
            }
            else if (currentBuildableIndex < 0)
            {
                currentBuildableIndex = AvailableBuildableItems.Count - 1;
            }
            currentSelectedItem = AvailableBuildableItems[currentBuildableIndex];
            Log.Info($"Selected buildable item: {currentSelectedItem.Name}");

            if (IsBuildingModeActive)
            {
                CreateActiveGhost(); 
            }
        }

        public void RotateGhost(bool clockwise)
        {
            if (IsBuildingModeActive && activeGhostEntity != null)
            {
                currentGhostRotationY += (clockwise ? 1 : -1) * RotationSnapAngle;
                currentGhostRotationY = currentGhostRotationY % 360;
                if (currentGhostRotationY < 0) currentGhostRotationY += 360;

                activeGhostEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(currentGhostRotationY));
            }
        }

        public override void Update()
        {
            if (!IsBuildingModeActive || activeGhostEntity == null || playerCamera == null || simulation == null)
            {
                return;
            }
            
            lastSnappedToPiece = null; // Reset at the beginning of each update cycle

            Matrix cameraWorldMatrix = playerCamera.Entity.Transform.WorldMatrix;
            Vector3 raycastStart = cameraWorldMatrix.TranslationVector;
            Vector3 raycastForward = cameraWorldMatrix.Forward;

            var hitResult = simulation.Raycast(raycastStart, raycastStart + raycastForward * MaxPlacementDistance);

            if (hitResult.Succeeded)
            {
                Vector3 finalPosition = hitResult.Point;
                Quaternion finalRotation = Quaternion.RotationY(MathUtil.DegreesToRadians(currentGhostRotationY));
                bool snappedToPiece = false;

                var hitPiece = hitResult.Collider.Entity?.Get<BaseBuildingPiece>();

                if (hitPiece != null)
                {
                    float closestSnapDistance = SnapRadius; 
                    SnapPoint bestSnapPoint = default; 
                    Vector3 worldBestSnapPosition = Vector3.Zero;
                    Quaternion worldBestSnapRotation = Quaternion.Identity;

                    foreach (var snapPoint in hitPiece.SnapPoints)
                    {
                        Matrix.Multiply(ref snapPoint.LocalRotation.AsMatrix(), ref hitPiece.Entity.Transform.WorldMatrix, out Matrix snapPointWorldMatrix);
                        snapPointWorldMatrix.TranslationVector = Vector3.Transform(snapPoint.LocalOffset, hitPiece.Entity.Transform.WorldMatrix);
                        
                        Vector3 currentWorldSnapPosition = snapPointWorldMatrix.TranslationVector;
                        Quaternion currentWorldSnapRotation = Quaternion.RotationMatrix(snapPointWorldMatrix);
                        float distanceToSnap = Vector3.Distance(hitResult.Point, currentWorldSnapPosition);

                        if (distanceToSnap < closestSnapDistance)
                        {
                            Log.Info($"Potential snap: Ghost ({currentSelectedItem.Name}) to {hitPiece.GetType().Name} ({hitPiece.Entity.Name}), point type '{snapPoint.Type}' at distance {distanceToSnap}.");
                            closestSnapDistance = distanceToSnap;
                            bestSnapPoint = snapPoint;
                            worldBestSnapPosition = currentWorldSnapPosition;
                            worldBestSnapRotation = currentWorldSnapRotation;
                            snappedToPiece = true;
                        }
                    }

                    if (snappedToPiece)
                    {
                        finalPosition = worldBestSnapPosition;
                        finalRotation = worldBestSnapRotation * Quaternion.RotationY(MathUtil.DegreesToRadians(currentGhostRotationY)); 
                        lastSnappedToPiece = hitPiece; // Store the piece we snapped to
                    }
                }
                
                if (!snappedToPiece) 
                {
                    finalPosition.X = MathF.Round(finalPosition.X / GridSnapSize) * GridSnapSize;
                    finalPosition.Y = MathF.Round(finalPosition.Y / GridSnapSize) * GridSnapSize;
                    finalPosition.Z = MathF.Round(finalPosition.Z / GridSnapSize) * GridSnapSize;
                    // lastSnappedToPiece is already null or retains value from a previous successful snap if no new snap this frame
                }
                
                activeGhostEntity.Transform.Position = finalPosition;
                activeGhostEntity.Transform.Rotation = finalRotation;

                canPlace = true; 
                if (Vector3.Distance(raycastStart, finalPosition) > MaxPlacementDistance + SnapRadius) 
                {
                    canPlace = false;
                }
            }
            else
            {
                canPlace = false;
            }
        }

        public bool TryPlaceBuilding()
        {
            if (IsBuildingModeActive && activeGhostEntity != null && canPlace)
            {
                if (currentSelectedItem.PiecePrefab == null) 
                {
                    Log.Error($"Cannot place item '{currentSelectedItem.Name}': PiecePrefab not set.");
                    return false;
                }

                var newBuildingPieceEntity = currentSelectedItem.PiecePrefab.Instantiate().FirstOrDefault();
                if (newBuildingPieceEntity != null)
                {
                    newBuildingPieceEntity.Transform.Position = activeGhostEntity.Transform.Position;
                    newBuildingPieceEntity.Transform.Rotation = activeGhostEntity.Transform.Rotation;
                    
                    var newlyPlacedPieceScript = newBuildingPieceEntity.Get<BaseBuildingPiece>();

                    if (this.Entity.Scene != null)
                    {
                        this.Entity.Scene.Entities.Add(newBuildingPieceEntity);
                        Log.Info($"Placed {currentSelectedItem.Name} at {newBuildingPieceEntity.Transform.Position}.");

                        if (newlyPlacedPieceScript != null)
                        {
                            // Initial Anchor Check for ground pieces placed on "terrain" (i.e., not snapped)
                            if (newlyPlacedPieceScript.IsGroundPiece && lastSnappedToPiece == null)
                            {
                                newlyPlacedPieceScript.IsAnchored = true;
                                Log.Info($"{newlyPlacedPieceScript.Entity.Name} is directly anchored as it's a ground piece placed on terrain/nothing.");
                            }
                            // Or, if snapped to an already anchored piece, it also becomes anchored.
                            else if (lastSnappedToPiece != null && lastSnappedToPiece.IsAnchored)
                            {
                                newlyPlacedPieceScript.IsAnchored = true;
                                Log.Info($"{newlyPlacedPieceScript.Entity.Name} is anchored by connecting to an anchored piece: {lastSnappedToPiece.Entity.Name}.");
                            }


                            // Establish connections
                            if (lastSnappedToPiece != null)
                            {
                                newlyPlacedPieceScript.AddConnection(lastSnappedToPiece);
                                lastSnappedToPiece.AddConnection(newlyPlacedPieceScript);
                                Log.Info($"Connected {newlyPlacedPieceScript.Entity.Name} with {lastSnappedToPiece.Entity.Name}.");
                            }
                            
                            // Update anchor status for the entire structure this piece is now part of
                            if (StructureIntegrityManager.Instance != null)
                            {
                                StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(newlyPlacedPieceScript);
                            }
                            else
                            {
                                Log.Warning("StructureIntegrityManager.Instance is null. Cannot update anchor status.");
                            }
                        }
                        return true; 
                    }
                    else
                    {
                        Log.Error("BuildingPlacementController: Cannot place building, controller's parent scene is null.");
                        newBuildingPieceEntity = null; 
                        return false;
                    }
                }
                else
                {
                    Log.Error($"Failed to instantiate PiecePrefab for '{currentSelectedItem.Name}'.");
                    return false;
                }
            }
            Log.Warning("TryPlaceBuilding: Conditions not met for placement (Mode active? Ghost exists? Can place?).");
            return false;
        }
    }
}
