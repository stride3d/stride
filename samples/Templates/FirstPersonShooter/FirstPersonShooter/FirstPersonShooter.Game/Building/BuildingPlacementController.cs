// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using FirstPersonShooter.Player; // For PlayerInput to get CameraComponent

namespace FirstPersonShooter.Building
{
    public class BuildingPlacementController : SyncScript
    {
using System.Linq; // Required for FirstOrDefault()

namespace FirstPersonShooter.Building
{
    public class BuildingPlacementController : SyncScript
    {
        public bool IsBuildingModeActive { get; private set; } = false;
        public Prefab GhostPreviewPrefab { get; set; } // Assign in editor
        public Prefab FoundationBuildablePrefab { get; set; } // Assign in editor - The actual foundation
        private Entity activeGhostEntity;
        public float MaxPlacementDistance { get; set; } = 10f;
        public float GridSnapSize { get; set; } = 1.0f;
        public float RotationSnapAngle { get; set; } = 15.0f; // Degrees
        private float currentGhostRotationY = 0f;
        private bool canPlace = false;
        private CameraComponent playerCamera;
        private Simulation simulation;

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

            // Explicit null checks for prefabs
            if (GhostPreviewPrefab == null)
            {
                Log.Error("BuildingPlacementController: GhostPreviewPrefab is not assigned in the editor!");
            }
            if (FoundationBuildablePrefab == null)
            {
                Log.Error("BuildingPlacementController: FoundationBuildablePrefab is not assigned in the editor!");
            }
        }

        public void ToggleBuildingMode()
        {
            IsBuildingModeActive = !IsBuildingModeActive;

            if (IsBuildingModeActive)
            {
                if (GhostPreviewPrefab == null)
                {
                    Log.Error("BuildingPlacementController: GhostPreviewPrefab is not assigned. Cannot activate building mode.");
                    IsBuildingModeActive = false; // Revert
                    return;
                }

                var instances = GhostPreviewPrefab.Instantiate();
                if (instances == null || instances.Count == 0)
                {
                    Log.Error("BuildingPlacementController: Failed to instantiate GhostPreviewPrefab.");
                    IsBuildingModeActive = false; // Revert
                    return;
                }
                activeGhostEntity = instances[0];
                // Ensure material is semi-transparent (usually set on prefab, but can be forced here if needed)
                // Example: activeGhostEntity.Get<ModelComponent>()?.Materials[0].Passes[0].Parameters.Set(MaterialKeys.AlphaBlend, true); 
                //          activeGhostEntity.Get<ModelComponent>()?.Materials[0].Passes[0].Parameters.Set(MaterialKeys.DiffuseColor, new Color4(0.5f, 1f, 0.5f, 0.5f));
                
                // Add to the same scene as this controller entity
                if (this.Entity.Scene != null)
                {
                    this.Entity.Scene.Entities.Add(activeGhostEntity);
                }
                else
                {
                    Log.Error("BuildingPlacementController: Cannot add ghost to scene, controller's parent scene is null.");
                    IsBuildingModeActive = false; // Revert
                    activeGhostEntity = null; // Don't keep reference if not added
                    return;
                }
                currentGhostRotationY = 0f; // Reset rotation
                Log.Info("Building mode activated.");
            }
            else
            {
                if (activeGhostEntity != null)
                {
                    activeGhostEntity.Scene = null; // Remove from scene
                    activeGhostEntity = null;
                }
                Log.Info("Building mode deactivated.");
            }
        }

        public void RotateGhost(bool clockwise)
        {
            if (IsBuildingModeActive && activeGhostEntity != null)
            {
                currentGhostRotationY += (clockwise ? 1 : -1) * RotationSnapAngle;
                // Normalize angle (optional, but good practice)
                currentGhostRotationY = currentGhostRotationY % 360;
                if (currentGhostRotationY < 0) currentGhostRotationY += 360;

                activeGhostEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(currentGhostRotationY));
                Log.Info($"Ghost rotated to {currentGhostRotationY} degrees.");
            }
        }

        public override void Update()
        {
            if (!IsBuildingModeActive || activeGhostEntity == null || playerCamera == null || simulation == null)
            {
                return;
            }

            Matrix cameraWorldMatrix = playerCamera.Entity.Transform.WorldMatrix; // More direct way to get world matrix
            Vector3 raycastStart = cameraWorldMatrix.TranslationVector;
            Vector3 raycastForward = cameraWorldMatrix.Forward;

            var hitResult = simulation.Raycast(raycastStart, raycastStart + raycastForward * MaxPlacementDistance);

            if (hitResult.Succeeded)
            {
                Vector3 targetPosition = hitResult.Point;
                
                // Snap position to grid
                targetPosition.X = MathF.Round(targetPosition.X / GridSnapSize) * GridSnapSize;
                targetPosition.Y = MathF.Round(targetPosition.Y / GridSnapSize) * GridSnapSize; // Snapping Y might need adjustment based on desired behavior (e.g., on terrain or stacking)
                targetPosition.Z = MathF.Round(targetPosition.Z / GridSnapSize) * GridSnapSize;
                
                activeGhostEntity.Transform.Position = targetPosition;
                activeGhostEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(currentGhostRotationY)); // Ensure rotation is also updated

                // Placement Validity Check (Basic)
                canPlace = true;
                if (Vector3.Distance(raycastStart, targetPosition) > MaxPlacementDistance + 0.5f) // Small buffer for snapping
                {
                    canPlace = false;
                }
                // Future: Add collision checks for activeGhostEntity (e.g., ConvexSweep)
                // For now, just logging. A visual change to the ghost's material would be ideal.
                // Example: var ghostModel = activeGhostEntity.Get<ModelComponent>();
                // if (ghostModel != null && ghostModel.Materials.Count > 0) {
                //    var material = ghostModel.Materials[0]; // Assuming one material
                //    material.Passes[0].Parameters.Set(MaterialKeys.EmissiveColor, canPlace ? Color.Green*0.2f : Color.Red*0.2f); // Needs emissive material
                // }
                Log.Info($"Ghost at {targetPosition}. Can place: {canPlace}");
            }
            else
            {
                canPlace = false;
                // Optionally hide or move ghost far away
                // activeGhostEntity.Transform.Position = new Vector3(0, -1000, 0); // Move far
                Log.Info($"No valid placement surface found. Can place: {canPlace}");
            }
        }

        public bool TryPlaceBuilding()
        {
            if (IsBuildingModeActive && activeGhostEntity != null && canPlace)
            {
                if (FoundationBuildablePrefab == null)
                {
                    Log.Error("Cannot place foundation: FoundationBuildablePrefab not set.");
                    return false; // Indicate failure due to missing prefab specifically
                }

                var newBuildingPieceEntity = FoundationBuildablePrefab.Instantiate().FirstOrDefault();
                if (newBuildingPieceEntity != null)
                {
                    newBuildingPieceEntity.Transform.Position = activeGhostEntity.Transform.Position;
                    newBuildingPieceEntity.Transform.Rotation = activeGhostEntity.Transform.Rotation;

                    if (this.Entity.Scene != null)
                    {
                        this.Entity.Scene.Entities.Add(newBuildingPieceEntity);
                        Log.Info($"Placed Foundation at {newBuildingPieceEntity.Transform.Position}.");
                        // Future: Consume resources, start cooldown, etc.
                        return true; // Successfully placed
                    }
                    else
                    {
                        Log.Error("BuildingPlacementController: Cannot place building, controller's parent scene is null.");
                        // Attempt to clean up the instantiated entity if it wasn't added to a scene
                        // (though if scene is null, it likely wasn't "alive" yet to be cleaned by Stride)
                        newBuildingPieceEntity = null; 
                        return false; // Failed to place due to scene issue
                    }
                }
                else
                {
                    Log.Error("Failed to instantiate FoundationBuildablePrefab.");
                    return false; // Failed to instantiate
                }
            }
            Log.Warning("TryPlaceBuilding: Conditions not met for placement (Mode active? Ghost exists? Can place?).");
            return false;
        }
    }
}
