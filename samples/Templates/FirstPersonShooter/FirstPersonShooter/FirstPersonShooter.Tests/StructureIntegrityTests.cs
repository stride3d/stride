// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Building;       // For StructureIntegrityManager
using FirstPersonShooter.Building.Pieces; // For BaseBuildingPiece, SnapPoint
using FirstPersonShooter.Core;           // For MaterialType
using System.Collections.Generic;
using System.Linq;

namespace FirstPersonShooter.Tests
{
    // --- Mock Building Piece for Integrity Tests ---
    public class MockStructuralPiece : BaseBuildingPiece
    {
        private float _health = 100f;
        private MaterialType _material = MaterialType.Stone;

        public override float Health { get => _health; set => _health = value; }
        public override MaterialType StructureMaterialType { get => _material; set => _material = value; }

        public bool DebugForceDestroyCalled { get; private set; }
        public string PieceName { get; } // For easier debugging

        public MockStructuralPiece(string name = "MockStructuralPiece")
        {
            PieceName = name;
        }
        
        public override void InitializeSnapPoints()
        {
            // No specific snap points needed for these tests, but base class requires override
            SnapPoints.Clear(); 
        }

        public override void OnPieceDestroyed()
        {
            // Log.Info($"MockStructuralPiece '{PieceName}' OnPieceDestroyed called (base logic will run).");
            base.OnPieceDestroyed(); // Important for connection cleanup and triggering neighbors
        }

        // Override Debug_ForceDestroy to set the flag *before* base logic that might remove from scene
        public new void Debug_ForceDestroy() // `new` keyword to hide base if not virtual, or override if it was virtual
        {
            Log.Info($"MockStructuralPiece '{PieceName}' Debug_ForceDestroy called.");
            DebugForceDestroyCalled = true;
            base.Debug_ForceDestroy(); // Call base to handle OnPieceDestroyed and scene removal
        }

        public void ResetMockState()
        {
            DebugForceDestroyCalled = false;
            IsAnchored = false; 
            ConnectedPieces.Clear();
        }
    }

    public class StructureIntegrityTests : SyncScript
    {
        private int testsRun = 0;
        private int testsPassed = 0;
        private StructureIntegrityManager sim; // Instance of the manager

        public override void Start()
        {
            Log.Info("StructureIntegrityTests: Starting tests...");

            // Setup StructureIntegrityManager instance for tests
            var simEntity = new Entity("TestSIM");
            sim = new StructureIntegrityManager();
            simEntity.Add(sim);
            this.Entity.Scene.Entities.Add(simEntity); // Add to scene so 'Instance' is set via its Start()
            // sim.Start(); // Stride calls this
            
            if (StructureIntegrityManager.Instance == null)
            {
                Log.Error("StructureIntegrityTests: StructureIntegrityManager.Instance is null after setup. Tests cannot proceed.");
                return;
            }

            TestAnchorPropagation();
            TestLosingAnchor();
            TestBasicCollapse();
            TestCascadingCollapseConceptual();

            Log.Info($"StructureIntegrityTests: Finished. {testsPassed}/{testsRun} tests passed.");
            
            // Cleanup SIM entity
            simEntity.Scene = null;
        }

        private void AssertTrue(bool condition, string testName, string message = "")
        {
            testsRun++;
            if (condition) { testsPassed++; Log.Info($"[SUCCESS] {testName} {message}"); }
            else { Log.Error($"[FAILURE] {testName} {message}"); }
        }
        private void AssertFalse(bool condition, string testName, string message = "") { AssertTrue(!condition, testName, message); }

        private Entity CreatePieceEntity(string name, BaseBuildingPiece pieceScript)
        {
            var entity = new Entity(name);
            entity.Add(pieceScript);
            this.Entity.Scene.Entities.Add(entity); // Add to scene for Start() and SIM to find
            // pieceScript.Start(); // Stride calls this
            return entity;
        }

        private void CleanupEntities(params Entity[] entities)
        {
            foreach(var entity in entities)
            {
                if (entity != null && entity.Scene != null)
                {
                    entity.Scene = null;
                }
            }
        }

        private void TestAnchorPropagation()
        {
            var testName = "TestAnchorPropagation";
            Log.Info($"StructureIntegrityTests: Running {testName}...");

            var pieceA = new MockStructuralPiece("PieceA_Prop") { IsGroundPiece = true, IsAnchored = true }; // Initial anchor
            var pieceB = new MockStructuralPiece("PieceB_Prop");
            var pieceC = new MockStructuralPiece("PieceC_Prop");

            var entityA = CreatePieceEntity("EntityA_Prop", pieceA);
            var entityB = CreatePieceEntity("EntityB_Prop", pieceB);
            var entityC = CreatePieceEntity("EntityC_Prop", pieceC);

            pieceA.AddConnection(pieceB); pieceB.AddConnection(pieceA);
            pieceB.AddConnection(pieceC); pieceC.AddConnection(pieceB);
            
            // Before SIM update, only A should be anchored
            AssertTrue(pieceA.IsAnchored, $"{testName} - Pre-SIM: A is anchored");
            AssertFalse(pieceB.IsAnchored, $"{testName} - Pre-SIM: B is not anchored");
            AssertFalse(pieceC.IsAnchored, $"{testName} - Pre-SIM: C is not anchored");

            StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(pieceA);

            AssertTrue(pieceA.IsAnchored, $"{testName} - Post-SIM: A is anchored");
            AssertTrue(pieceB.IsAnchored, $"{testName} - Post-SIM: B is anchored");
            AssertTrue(pieceC.IsAnchored, $"{testName} - Post-SIM: C is anchored");
            
            CleanupEntities(entityA, entityB, entityC);
        }

        private void TestLosingAnchor()
        {
            var testName = "TestLosingAnchor";
            Log.Info($"StructureIntegrityTests: Running {testName}...");

            var pieceA = new MockStructuralPiece("PieceA_Loss") { IsGroundPiece = true, IsAnchored = true };
            var pieceB = new MockStructuralPiece("PieceB_Loss");
            var pieceC = new MockStructuralPiece("PieceC_Loss");
            
            var entityA = CreatePieceEntity("EntityA_Loss", pieceA);
            var entityB = CreatePieceEntity("EntityB_Loss", pieceB);
            var entityC = CreatePieceEntity("EntityC_Loss", pieceC);

            pieceA.AddConnection(pieceB); pieceB.AddConnection(pieceA);
            pieceB.AddConnection(pieceC); pieceC.AddConnection(pieceB);
            StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(pieceA); // All should be anchored initially

            AssertTrue(pieceB.IsAnchored, $"{testName} - Pre-Disconnect: B is anchored");
            AssertTrue(pieceC.IsAnchored, $"{testName} - Pre-Disconnect: C is anchored");

            // Disconnect A from B
            pieceA.RemoveConnection(pieceB);
            pieceB.RemoveConnection(pieceA);
            
            StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(pieceB); // Update starting from B

            AssertTrue(pieceA.IsAnchored, $"{testName} - Post-Disconnect: A (ground piece) remains anchored");
            AssertFalse(pieceB.IsAnchored, $"{testName} - Post-Disconnect: B is NOT anchored");
            AssertFalse(pieceC.IsAnchored, $"{testName} - Post-Disconnect: C is NOT anchored");
            
            CleanupEntities(entityA, entityB, entityC);
        }

        private void TestBasicCollapse()
        {
            var testName = "TestBasicCollapse";
            Log.Info($"StructureIntegrityTests: Running {testName}...");

            var pieceA = new MockStructuralPiece("PieceA_Collapse") { IsGroundPiece = true, IsAnchored = true };
            var pieceB = new MockStructuralPiece("PieceB_Collapse");

            var entityA = CreatePieceEntity("EntityA_Collapse", pieceA);
            var entityB = CreatePieceEntity("EntityB_Collapse", pieceB);

            pieceA.AddConnection(pieceB); pieceB.AddConnection(pieceA);
            StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(pieceA); // B should be anchored

            AssertTrue(pieceB.IsAnchored, $"{testName} - Pre-Collapse: B is anchored");
            AssertFalse(pieceB.DebugForceDestroyCalled, $"{testName} - Pre-Collapse: B DebugForceDestroyCalled is false");

            // Disconnect A from B
            pieceA.RemoveConnection(pieceB);
            pieceB.RemoveConnection(pieceA);
            
            StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(pieceB); // This should find B unanchored and destroy it

            AssertFalse(pieceB.IsAnchored, $"{testName} - Post-Collapse: B is not anchored");
            AssertTrue(pieceB.DebugForceDestroyCalled, $"{testName} - Post-Collapse: B DebugForceDestroyCalled is true");
            AssertNull(entityB.Scene, $"{testName} - Post-Collapse: EntityB removed from scene"); // Debug_ForceDestroy in Mock removes from scene
            
            CleanupEntities(entityA); // entityB already removed
        }

        private void TestCascadingCollapseConceptual()
        {
            var testName = "TestCascadingCollapseConceptual";
            Log.Info($"StructureIntegrityTests: Running {testName}...");

            var pieceA = new MockStructuralPiece("PieceA_Cascade") { IsGroundPiece = true, IsAnchored = true };
            var pieceB = new MockStructuralPiece("PieceB_Cascade");
            var pieceC = new MockStructuralPiece("PieceC_Cascade");

            var entityA = CreatePieceEntity("EntityA_Cascade", pieceA);
            var entityB = CreatePieceEntity("EntityB_Cascade", pieceB);
            var entityC = CreatePieceEntity("EntityC_Cascade", pieceC);

            pieceA.AddConnection(pieceB); pieceB.AddConnection(pieceA);
            pieceB.AddConnection(pieceC); pieceC.AddConnection(pieceB);
            StructureIntegrityManager.Instance.UpdateAnchorStatusForStructure(pieceA); // All anchored

            AssertTrue(pieceA.IsAnchored && pieceB.IsAnchored && pieceC.IsAnchored, $"{testName} - Pre-Cascade: All pieces anchored");
            AssertFalse(pieceA.DebugForceDestroyCalled, $"{testName} - Pre-Cascade: A not destroyed");
            AssertFalse(pieceB.DebugForceDestroyCalled, $"{testName} - Pre-Cascade: B not destroyed");
            AssertFalse(pieceC.DebugForceDestroyCalled, $"{testName} - Pre-Cascade: C not destroyed");

            // Destroy A. This calls A.OnPieceDestroyed -> notifies B.
            // B.RemoveConnection(A) is called.
            // SIM.UpdateAnchorStatusForStructure(B) is called from A.OnPieceDestroyed.
            // SIM finds B unanchored, calls B.Debug_ForceDestroy.
            // B.OnPieceDestroyed -> notifies C.
            // C.RemoveConnection(B) is called.
            // SIM.UpdateAnchorStatusForStructure(C) is called from B.OnPieceDestroyed.
            // SIM finds C unanchored, calls C.Debug_ForceDestroy.
            
            pieceA.Debug_ForceDestroy(); // Initiate cascade

            AssertTrue(pieceA.DebugForceDestroyCalled, $"{testName} - Post-Cascade: A DebugForceDestroyCalled is true");
            AssertTrue(pieceB.DebugForceDestroyCalled, $"{testName} - Post-Cascade: B DebugForceDestroyCalled is true");
            AssertTrue(pieceC.DebugForceDestroyCalled, $"{testName} - Post-Cascade: C DebugForceDestroyCalled is true");
            
            AssertNull(entityA.Scene, $"{testName} - Post-Cascade: EntityA removed");
            AssertNull(entityB.Scene, $"{testName} - Post-Cascade: EntityB removed");
            AssertNull(entityC.Scene, $"{testName} - Post-Cascade: EntityC removed");
            
            // No entities to cleanup as they should all be removed from scene by Debug_ForceDestroy
        }
    }
}
