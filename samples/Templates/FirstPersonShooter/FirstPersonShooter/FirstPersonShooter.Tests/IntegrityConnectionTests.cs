// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Building;       // For BuildingPlacementController (conceptual)
using FirstPersonShooter.Building.Pieces; // For BaseBuildingPiece, SnapPoint
using FirstPersonShooter.Core;           // For MaterialType
using System.Collections.Generic;
using System.Linq;

namespace FirstPersonShooter.Tests
{
    // --- Mock Building Piece for Integrity Tests ---
    public class MockIntegrityTestPiece : BaseBuildingPiece
    {
        private float _health = 100f;
        private MaterialType _material = MaterialType.Stone;

        public override float Health { get => _health; set => _health = value; }
        public override MaterialType StructureMaterialType { get => _material; set => _material = value; }

        public bool OnPieceDestroyedCalled { get; private set; }

        public MockIntegrityTestPiece(string name = "MockIntegrityPiece")
        {
            // Entity might be null if not added to one yet, handle gracefully for logging
            // this.Entity.Name = name; // Cannot set Entity.Name directly here if Entity is not yet created
        }

        public override void InitializeSnapPoints()
        {
            // Add a generic snap point if needed for some tests, though not strictly for connection logic.
            SnapPoints.Add(new SnapPoint { Type = "Generic" });
        }

        public override void OnPieceDestroyed()
        {
            base.OnPieceDestroyed(); // Calls base logic to notify connected pieces
            OnPieceDestroyedCalled = true;
            Log.Info($"MockIntegrityTestPiece '{Entity?.Name ?? "Unnamed"}' OnPieceDestroyed finished.");
        }

        public void ResetMockState()
        {
            OnPieceDestroyedCalled = false;
            ConnectedPieces.Clear(); // Clear connections from base class
        }
    }


    public class IntegrityConnectionTests : SyncScript
    {
        private int testsRun = 0;
        private int testsPassed = 0;

        public override void Start()
        {
            Log.Info("IntegrityConnectionTests: Starting tests...");

            TestAddRemoveConnection();
            TestOnPieceDestroyedNotification();
            TestConnectionOnPlacementConceptual();

            Log.Info($"IntegrityConnectionTests: Finished. {testsPassed}/{testsRun} tests passed.");
        }

        private void AssertTrue(bool condition, string testName, string message = "")
        {
            testsRun++;
            if (condition) { testsPassed++; Log.Info($"[SUCCESS] {testName} {message}"); }
            else { Log.Error($"[FAILURE] {testName} {message}"); }
        }
        
        private void AssertFalse(bool condition, string testName, string message = "") { AssertTrue(!condition, testName, message); }
        
        private void AssertEquals(int expected, int actual, string testName, string message = "")
        {
            testsRun++;
            if (expected == actual) { testsPassed++; Log.Info($"[SUCCESS] {testName} {message}"); }
            else { Log.Error($"[FAILURE] {testName} - Expected '{expected}', got '{actual}' {message}"); }
        }
        
        private void AssertContains<T>(List<T> list, T expectedItem, string testName, string message = "") where T : class
        {
            testsRun++;
            bool found = list.Contains(expectedItem);
            if (found) { testsPassed++; Log.Info($"[SUCCESS] {testName} {message}"); }
            else { Log.Error($"[FAILURE] {testName} - List does not contain expected item. {message}"); }
        }

        private void AssertNotContains<T>(List<T> list, T expectedItem, string testName, string message = "") where T : class
        {
            testsRun++;
            bool found = list.Contains(expectedItem);
            if (!found) { testsPassed++; Log.Info($"[SUCCESS] {testName} {message}"); }
            else { Log.Error($"[FAILURE] {testName} - List unexpectedly contains item. {message}"); }
        }
        
        private Entity CreateNamedEntity(string name)
        {
            var entity = new Entity(name);
            // Add to scene if needed for some script initializations, though not strictly for these tests if Start isn't vital.
            // this.Entity.Scene.Entities.Add(entity); 
            return entity;
        }

        private void TestAddRemoveConnection()
        {
            var testName = "TestAddRemoveConnection";
            Log.Info($"IntegrityConnectionTests: Running {testName}...");

            var pieceAEntity = CreateNamedEntity("PieceA_ConnTest");
            var pieceA = new MockIntegrityTestPiece();
            pieceAEntity.Add(pieceA);
            // pieceA.Start(); // BaseBuildingPiece.Start initializes SnapPoints and calls InitializeSnapPoints

            var pieceBEntity = CreateNamedEntity("PieceB_ConnTest");
            var pieceB = new MockIntegrityTestPiece();
            pieceBEntity.Add(pieceB);
            // pieceB.Start();

            // Add connection A -> B
            pieceA.AddConnection(pieceB);
            AssertContains(pieceA.ConnectedPieces, pieceB, $"{testName} - PieceA's connections contain PieceB");
            AssertEquals(1, pieceA.ConnectedPieces.Count, $"{testName} - PieceA has 1 connection");
            AssertEquals(0, pieceB.ConnectedPieces.Count, $"{testName} - PieceB has 0 connections (unidirectional for now)");

            // Add connection B -> A (making it bidirectional for test)
            pieceB.AddConnection(pieceA);
            AssertContains(pieceB.ConnectedPieces, pieceA, $"{testName} - PieceB's connections contain PieceA");

            // Remove connection A -> B
            pieceA.RemoveConnection(pieceB);
            AssertNotContains(pieceA.ConnectedPieces, pieceB, $"{testName} - PieceA's connections no longer contain PieceB");
            AssertEquals(0, pieceA.ConnectedPieces.Count, $"{testName} - PieceA has 0 connections after removal");
            AssertContains(pieceB.ConnectedPieces, pieceA, $"{testName} - PieceB still connected to PieceA (Remove is one-way)");
            
            // Remove non-existent connection
            pieceA.RemoveConnection(pieceB); // Should do nothing, no error
            AssertEquals(0, pieceA.ConnectedPieces.Count, $"{testName} - PieceA connection count unchanged after removing non-existent");
        }

        private void TestOnPieceDestroyedNotification()
        {
            var testName = "TestOnPieceDestroyedNotification";
            Log.Info($"IntegrityConnectionTests: Running {testName}...");

            var pieceAEntity = CreateNamedEntity("PieceA_DestroyTest");
            var pieceA = new MockIntegrityTestPiece();
            pieceAEntity.Add(pieceA);

            var pieceBEntity = CreateNamedEntity("PieceB_DestroyTest");
            var pieceB = new MockIntegrityTestPiece();
            pieceBEntity.Add(pieceB);

            var pieceCEntity = CreateNamedEntity("PieceC_DestroyTest");
            var pieceC = new MockIntegrityTestPiece();
            pieceCEntity.Add(pieceC);
            
            // Setup connections: A-B, B-A, A-C, C-A
            pieceA.AddConnection(pieceB); pieceB.AddConnection(pieceA);
            pieceA.AddConnection(pieceC); pieceC.AddConnection(pieceA);

            AssertEquals(2, pieceA.ConnectedPieces.Count, $"{testName} - Pre-destroy: PieceA has 2 connections");
            AssertEquals(1, pieceB.ConnectedPieces.Count, $"{testName} - Pre-destroy: PieceB has 1 connection");
            AssertEquals(1, pieceC.ConnectedPieces.Count, $"{testName} - Pre-destroy: PieceC has 1 connection");

            // Destroy Piece A
            // pieceA.Debug_ForceDestroy(); // This also removes entity from scene.
            // For this test, just call OnPieceDestroyed to check connection logic.
            pieceA.OnPieceDestroyed(); 

            AssertTrue(pieceA.OnPieceDestroyedCalled, $"{testName} - PieceA.OnPieceDestroyedCalled is true");
            AssertEquals(0, pieceA.ConnectedPieces.Count, $"{testName} - Post-destroy: PieceA's connections cleared");
            AssertNotContains(pieceB.ConnectedPieces, pieceA, $"{testName} - Post-destroy: PieceA removed from PieceB's connections");
            AssertEquals(0, pieceB.ConnectedPieces.Count, $"{testName} - Post-destroy: PieceB has 0 connections");
            AssertNotContains(pieceC.ConnectedPieces, pieceA, $"{testName} - Post-destroy: PieceA removed from PieceC's connections");
            AssertEquals(0, pieceC.ConnectedPieces.Count, $"{testName} - Post-destroy: PieceC has 0 connections");
        }

        private void TestConnectionOnPlacementConceptual()
        {
            var testName = "TestConnectionOnPlacementConceptual";
            Log.Info($"IntegrityConnectionTests: Running {testName}...");

            // Setup mock BuildingPlacementController
            var playerEntity = CreateNamedEntity("Player_PlaceConn");
            var controllerEntity = CreateNamedEntity("BPC_PlaceConn");
            var controller = new BuildingPlacementController();
            controllerEntity.Add(controller);
            // BPC needs PlayerInput on its entity to get camera, simplified here.
            var playerInput = new PlayerInput();
            var cameraEntity = CreateNamedEntity("Cam_PlaceConn");
            cameraEntity.Add(new CameraComponent());
            playerInput.Camera = cameraEntity.Get<CameraComponent>();
            controllerEntity.Add(playerInput);
            if(this.Entity.Scene != null) this.Entity.Scene.Entities.Add(controllerEntity);
            if(this.Entity.Scene != null) this.Entity.Scene.Entities.Add(cameraEntity);


            // Mock an existing piece in the "world"
            var existingPieceEntity = CreateNamedEntity("ExistingPiece");
            var existingPiece = new MockIntegrityTestPiece();
            existingPieceEntity.Add(existingPiece);
            if(this.Entity.Scene != null) this.Entity.Scene.Entities.Add(existingPieceEntity);
            // existingPiece.Start();

            // Mock a new piece being placed
            var newPieceEntity = CreateNamedEntity("NewPiece");
            var newPiece = new MockIntegrityTestPiece();
            newPieceEntity.Add(newPiece);
            // newPiece.Start(); // Start would be called after it's added to scene by BPC

            // Simulate that BPC is about to place newPiece and it snapped to existingPiece
            controller.SetLastSnappedToPieceInternal(existingPiece); // Test helper to set internal state

            // Create a mock buildable item for the BPC
            var buildableItem = new BuildingPlacementController.BuildableItem
            {
                Name = "MockItem",
                PiecePrefab = new Prefab(new List<Entity> { newPieceEntity }), // Prefab that "instantiates" our newPieceEntity
                GhostPrefab = new Prefab() // Ghost not directly relevant here
            };
            controller.AvailableBuildableItems.Add(buildableItem);
            controller.Start(); // Initialize controller with item
            controller.SetCurrentSelectedItemInternal(buildableItem); // Test helper

            // Simulate placement conditions
            controller.SetIsBuildingModeActiveInternal(true);
            controller.SetCanPlaceInternal(true);
            controller.SetActiveGhostEntityInternal(new Entity()); // Needs a non-null ghost

            // Call TryPlaceBuilding
            bool placed = controller.TryPlaceBuilding();

            AssertTrue(placed, $"{testName} - TryPlaceBuilding succeeded conceptually");
            
            // Check connections
            // Note: BPC.TryPlaceBuilding adds the *instantiated* entity to the scene.
            // The `newPiece` we have here is the "prefab" version.
            // We need to get the script from the *actually placed* entity.
            // This test setup is becoming complex due to direct instantiation.
            // A more robust test would involve checking the scene for the newly added entity.
            
            // Conceptual assertion:
            // If BPC.TryPlaceBuilding worked as intended, it would have:
            // 1. Instantiated buildableItem.PiecePrefab (which we've set to return newPieceEntity)
            // 2. Gotten the BaseBuildingPiece script from it (which is newPiece)
            // 3. Called AddConnection on newPiece and existingPiece.
            
            // We check the original newPiece and existingPiece instances used in setup.
            // This assumes the BPC's Instantiate().FirstOrDefault() returns the entity we prepared.
            if (placed)
            {
                // Find the placed entity if it was added to scene.
                var placedEntityInScene = this.Entity.Scene.Entities.FirstOrDefault(e => e.Name == "NewPiece");
                var placedScript = placedEntityInScene?.Get<MockIntegrityTestPiece>();

                if (placedScript != null)
                {
                    AssertContains(placedScript.ConnectedPieces, existingPiece, $"{testName} - New piece connected to existing piece");
                    AssertContains(existingPiece.ConnectedPieces, placedScript, $"{testName} - Existing piece connected to new piece");
                }
                else
                {
                    AssertTrue(false, testName, "Placed entity or its script not found in scene for connection check.");
                }
            }
             Log.Warning($"{testName}: This test's verification of connections is simplified. Relies on specific mock prefab behavior and scene state.");
        }
    }

    // Test extensions for BuildingPlacementController
    public static class BuildingPlacementControllerIntegrityTestExtensions
    {
        public static void SetLastSnappedToPieceInternal(this BuildingPlacementController c, BaseBuildingPiece piece) { c.lastSnappedToPiece = piece; }
        public static void SetCurrentSelectedItemInternal(this BuildingPlacementController c, BuildingPlacementController.BuildableItem item) { c.currentSelectedItem = item; }
        public static void SetIsBuildingModeActiveInternal(this BuildingPlacementController c, bool active) { c.IsBuildingModeActive = active; }
        public static void SetCanPlaceInternal(this BuildingPlacementController c, bool canPlace) { c.canPlace = canPlace; }
        public static void SetActiveGhostEntityInternal(this BuildingPlacementController c, Entity ghost) { c.activeGhostEntity = ghost; }
    }
}
