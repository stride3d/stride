// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Core.Mathematics;
using Stride.Physics; // For HitResult, CollisionFilterGroups, etc.
using FirstPersonShooter.Building.Defenses;
using FirstPersonShooter.Building.Pieces; // For TurretPiece
using FirstPersonShooter.Core; // For ITargetable
using System.Collections.Generic;
using System.Linq;

namespace FirstPersonShooter.Tests
{
    // --- Mocks for Turret Tests ---
    public class MockTargetable : ScriptComponent, ITargetable
    {
        public Vector3 TargetOffset { get; set; } = Vector3.Zero;
        public string TargetName { get; set; }

        public MockTargetable(string name) { TargetName = name; }

        public Vector3 GetTargetPosition() => Entity.Transform.Position + TargetOffset;
        public Entity GetEntity() => Entity;

        public override string ToString() => TargetName; // For better logging
    }

    public class MockTurretWeaponSystem : TurretWeaponSystem
    {
        public int FireAtCallCount { get; private set; }
        public Entity LastFiredAtTarget { get; private set; }
        public bool ShouldFireSuccessfully { get; set; } = true; // Controls mock FireAt return

        public override bool FireAt(Entity targetEntity)
        {
            // Call base.Update() to handle cooldown if its logic is desired (it is in this mock)
            // but the base FireAt will log. We want to control return and track calls.
            // So, manage cooldown here or just check base.fireCooldownRemaining.

            if (fireCooldownRemaining > 0f) // Accessing protected member
            {
                return false;
            }
            
            FireAtCallCount++;
            LastFiredAtTarget = targetEntity;
            // Log.Info($"MockTurretWeaponSystem: FireAt called for {targetEntity?.Name}. Call count: {FireAtCallCount}");

            if (ShouldFireSuccessfully)
            {
                fireCooldownRemaining = 1.0f / FireRate; // Set cooldown
                return true;
            }
            return false;
        }
        // Helper to access protected member for test setup
        public void SetFireCooldown(float cd) { fireCooldownRemaining = cd; }
        public float GetFireCooldown() { return fireCooldownRemaining; }
    }


    public class TurretTests : SyncScript
    {
        private int testsRun = 0;
        private int testsPassed = 0;
        private float mockDeltaTime = 0.1f; // For time simulation

        public override void Start()
        {
            Log.Info("TurretTests: Starting tests...");

            TestTargetSelection();
            TestWeaponFiringAndCooldown();
            TestTurretPieceCoordinationConceptual();
            TestTurretPieceSnapPointInitialization();

            Log.Info($"TurretTests: Finished. {testsPassed}/{testsRun} tests passed.");
        }

        private void AssertTrue(bool condition, string testName, string message = "")
        {
            testsRun++;
            if (condition) { testsPassed++; Log.Info($"[SUCCESS] {testName} {message}"); }
            else { Log.Error($"[FAILURE] {testName} {message}"); }
        }
        private void AssertFalse(bool condition, string testName, string message = "") { AssertTrue(!condition, testName, message); }
        private void AssertEquals<T>(T expected, T actual, string testName, string message = "")
        {
            testsRun++;
            bool areEqual = (expected == null && actual == null) || (expected != null && expected.Equals(actual));
            if (areEqual) { testsPassed++; Log.Info($"[SUCCESS] {testName} {message}"); }
            else { Log.Error($"[FAILURE] {testName} - Expected '{expected?.ToString() ?? "null"}', got '{actual?.ToString() ?? "null"}' {message}"); }
        }
        private void AssertNotNull(object obj, string testName, string message = "") { AssertTrue(obj != null, testName, message); }
        private void AssertNull(object obj, string testName, string message = "") { AssertTrue(obj == null, testName, message); }

        private Entity CreateTargetEntity(string name, Vector3 position, bool makeTargetable = true)
        {
            var entity = new Entity(name) { Transform = { Position = position } };
            if (makeTargetable)
            {
                entity.Add(new MockTargetable(name));
            }
            // Add a basic collider for raycast/overlap tests
            var staticCollider = new StaticColliderComponent();
            // staticCollider.CollisionGroup = CollisionFilterGroups.CharacterFilter; // Important for targeting
            entity.Add(staticCollider); // TurretTargetingSystem uses CharacterFilter
            
            this.Entity.Scene.Entities.Add(entity);
            return entity;
        }
        
        private void CleanupEntities(params Entity[] entities)
        {
            foreach(var entity in entities) { if (entity != null && entity.Scene != null) entity.Scene = null; }
        }


        private void TestTargetSelection()
        {
            var testName = "TestTargetSelection";
            Log.Info($"TurretTests: Running {testName}...");

            var turretEntity = new Entity("TestTurret_Targeting") { Transform = { Position = Vector3.Zero } };
            var targetingSystem = new TurretTargetingSystem { TargetingRange = 20f, ScanInterval = 0.1f };
            turretEntity.Add(targetingSystem);
            this.Entity.Scene.Entities.Add(turretEntity); // Add to scene for GetSimulation()

            // Targets
            var targetInRangeLOS = CreateTargetEntity("TargetInRangeLOS", new Vector3(10, 0, 0));
            var targetInRangeNoLOS = CreateTargetEntity("TargetInRangeNoLOS", new Vector3(0, 0, 10));
            var targetOutOfRange = CreateTargetEntity("TargetOutOfRange", new Vector3(30, 0, 0));
            var nonTargetableInRange = CreateTargetEntity("NonTargetableInRange", new Vector3(5,0,0), false);
            var obstacle = new Entity("Obstacle") { Transform = { Position = new Vector3(0,0,5) } }; // Between turret and TargetInRangeNoLOS
            obstacle.Add(new StaticColliderComponent()); // Make it collidable
            this.Entity.Scene.Entities.Add(obstacle);


            // --- Test Case: Closest target with LOS ---
            targetingSystem.Update(new GameTime()); // Trigger scan
            AssertEquals(targetInRangeLOS, targetingSystem.CurrentTarget, $"{testName} - Selects closest target with LOS");

            // --- Test Case: Target Present but LOS blocked ---
            // Move targetInRangeLOS so targetInRangeNoLOS is closer but blocked
            targetInRangeLOS.Transform.Position = new Vector3(12,0,0); // Further away
            targetInRangeNoLOS.Transform.Position = new Vector3(0,0,10); // Closer but needs LOS check
            // TurretTargetingSystem's LOS raycast is from turret to target.GetTargetPosition().
            // If obstacle is between them, it should fail.
            // The LOS raycast in TurretTargetingSystem tries to hit things *not* on CharacterFilter.
            // So obstacle needs to be on a different group or DefaultFilter.
            // For this test, we assume physics setup allows obstacle to block LOS.
            targetingSystem.Update(new GameTime()); // Trigger scan
            AssertEquals(targetInRangeLOS, targetingSystem.CurrentTarget, $"{testName} - Selects further target if closer one has no LOS");


            // --- Test Case: No valid targets ---
            targetInRangeLOS.Scene = null; // Remove valid target
            targetInRangeNoLOS.Scene = null;
            targetingSystem.Update(new GameTime()); // Trigger scan
            AssertNull(targetingSystem.CurrentTarget, $"{testName} - No target if only out-of-range or non-targetable or no LOS available");
            
            CleanupEntities(turretEntity, targetInRangeLOS, targetInRangeNoLOS, targetOutOfRange, nonTargetableInRange, obstacle);
        }

        private void TestWeaponFiringAndCooldown()
        {
            var testName = "TestWeaponFiringAndCooldown";
            Log.Info($"TurretTests: Running {testName}...");

            var weaponSystemEntity = new Entity("TestTurret_Weapon");
            var weaponSystem = new MockTurretWeaponSystem { FireRate = 2f }; // Use mock
            weaponSystemEntity.Add(weaponSystem);
            // weaponSystem.Start(); // Stride calls this

            var mockTarget = new Entity("MockTarget_WeaponTest");

            // Fire
            bool fired = weaponSystem.FireAt(mockTarget);
            AssertTrue(fired, $"{testName} - Fired successfully first time");
            AssertEquals(1, weaponSystem.FireAtCallCount, $"{testName} - FireAtCallCount is 1");
            AssertTrue(weaponSystem.GetFireCooldown() > 0.4f && weaponSystem.GetFireCooldown() <= 0.5f, $"{testName} - Cooldown set after firing");

            // Try fire again (should be on cooldown)
            fired = weaponSystem.FireAt(mockTarget);
            AssertFalse(fired, $"{testName} - Did not fire while on cooldown");
            AssertEquals(1, weaponSystem.FireAtCallCount, $"{testName} - FireAtCallCount still 1 (due to cooldown)");

            // Simulate time passage for cooldown
            var gameTime = new GameTime();
            float totalTime = 0f;
            while (totalTime < (1.0f / weaponSystem.FireRate) + mockDeltaTime)
            {
                weaponSystem.Update(gameTime); // Manually call Update
                totalTime += mockDeltaTime;
            }
            AssertTrue(weaponSystem.GetFireCooldown() <= 0.0001f, $"{testName} - Cooldown finished after time passage ({weaponSystem.GetFireCooldown()})");

            // Fire again
            fired = weaponSystem.FireAt(mockTarget);
            AssertTrue(fired, $"{testName} - Fired successfully after cooldown");
            AssertEquals(2, weaponSystem.FireAtCallCount, $"{testName} - FireAtCallCount is 2");
            
            CleanupEntities(weaponSystemEntity, mockTarget);
        }

        private void TestTurretPieceCoordinationConceptual()
        {
            var testName = "TestTurretPieceCoordinationConceptual";
            Log.Info($"TurretTests: Running {testName}...");

            var turretPieceEntity = new Entity("TestTurret_Coordination");
            var turretPiece = new TurretPiece { RotationSpeed = 360f }; // Fast rotation for test
            
            var targetingSystem = new TurretTargetingSystem { ScanInterval = 0.05f }; // Scan frequently
            var weaponSystem = new MockTurretWeaponSystem { FireRate = 10f }; // Fire frequently
            
            var yawPart = new Entity("YawPart_Test");
            var pitchPart = new Entity("PitchPart_Test");
            // Parent pitch to yaw, yaw to turret entity for correct local/world transforms
            yawPart.AddChild(pitchPart);
            turretPieceEntity.AddChild(yawPart);

            turretPiece.TargetingSystem = targetingSystem;
            turretPiece.WeaponSystem = weaponSystem;
            turretPiece.TurretYawPart = yawPart;
            turretPiece.TurretPitchPart = pitchPart;
            // WeaponSystem.MuzzlePointEntity could be PitchPart or another child of PitchPart

            turretPieceEntity.Add(turretPiece);
            turretPieceEntity.Add(targetingSystem); // Add systems to same entity for simplicity
            turretPieceEntity.Add(weaponSystem);
            
            this.Entity.Scene.Entities.Add(turretPieceEntity);
            // Stride calls Start on all of them.

            var target = CreateTargetEntity("Target_CoordTest", new Vector3(10, 2, 0)); // Target at (10,2,0)

            // Simulate a few frames of updates
            var gameTime = new GameTime();
            Quaternion initialYaw = yawPart.Transform.Rotation;
            Quaternion initialPitch = pitchPart.Transform.Rotation;
            int initialFireCount = weaponSystem.FireAtCallCount;

            for (int i = 0; i < 30; i++) // Simulate ~0.5 seconds if mockDeltaTime is ~0.016
            {
                targetingSystem.Update(gameTime); // Scan for targets
                turretPiece.Update(gameTime);   // Rotate and maybe fire
                weaponSystem.Update(gameTime);  // Update weapon cooldown
                // Thread.Sleep((int)(mockDeltaTime * 1000)); // Not ideal in tests
            }
            
            AssertNotNull(targetingSystem.CurrentTarget, $"{testName} - Target should be acquired");
            AssertEquals(target, targetingSystem.CurrentTarget, $"{testName} - Correct target acquired");

            // Check rotation (conceptual: they should have changed from initial)
            // Exact values depend on RotationSpeed, deltaTime, and Slerp behavior.
            AssertFalse(yawPart.Transform.Rotation.Equals(initialYaw), $"{testName} - Yaw part should have rotated");
            AssertFalse(pitchPart.Transform.Rotation.Equals(initialPitch), $"{testName} - Pitch part should have rotated");
            
            // Check firing (conceptual: it should have fired if aimed)
            // The dot product check in TurretPiece.Update determines aiming.
            // If rotation is fast enough, it should aim and fire.
            AssertTrue(weaponSystem.FireAtCallCount > initialFireCount, $"{testName} - WeaponSystem.FireAt should have been called (Fired {weaponSystem.FireAtCallCount} times)");

            CleanupEntities(turretPieceEntity, target);
        }

        private void TestTurretPieceSnapPointInitialization()
        {
            var testName = "TestTurretPieceSnapPointInitialization";
            var turretPiece = new TurretPiece();
            var turretEntity = new Entity("TurretSnapTest").Add(turretPiece);
            // turretPiece.Start(); // Stride calls this if in scene
            this.Entity.Scene.Entities.Add(turretEntity); // Add to scene to trigger Start()

            AssertTrue(turretPiece.SnapPoints.Count > 0, $"{testName} - TurretPiece has snap points initialized.");
            bool foundBaseSnap = turretPiece.SnapPoints.Any(sp => sp.Type == "TurretBase");
            AssertTrue(foundBaseSnap, $"{testName} - TurretPiece has a 'TurretBase' snap point.");
            
            CleanupEntities(turretEntity);
        }
    }
}
