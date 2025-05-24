// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Physics;
using Stride.UnitTesting; // For Assert and other test utilities
using System.Collections.Generic;
using System.Threading.Tasks; // For Task.Delay in tests if needed for timing

// Game specific namespaces
using FirstPersonShooter.Building.Pieces;
using FirstPersonShooter.Building.Defenses;
using FirstPersonShooter.Building.Defenses.Strategies;
using FirstPersonShooter.Core;
using FirstPersonShooter.Player;   // For PlayerMarkerComponent
using FirstPersonShooter.AI;      // For CreatureMarkerComponent
using FirstPersonShooter.Weapons.Projectiles; // For BasicTurretProjectile (if ProjectileFireStrategy is tested directly)

namespace FirstPersonShooter.Tests
{
    public class TurretTests : GameTestBase
    {
        private Scene testScene;

        [TestInitialize]
        public void Setup()
        {
            // Load the game scene (or create a new test scene)
            // For isolated unit tests, a new minimal scene is often better.
            // Ensure Game.UpdateThreadCreated is true before scene operations if not running full game loop.
            // This is typically handled by GameTestBase or by starting the game.
            
            // Create a new scene for each test or manage entities carefully
            testScene = new Scene();
            Game.SceneSystem.SceneInstance = new SceneInstance(Services, testScene);
            
            // Ensure physics simulation is available for components that need it (like TurretTargetingSystem)
            var physics = new PhysicsProcessor();
            Game.Processors.Add(physics);
            Game.SceneSystem.Processors.Add(physics);
        
            // Wait for the scene to be ready if necessary, or manage script lifecycle manually.
            // For ScriptComponent.Start(), they need to be in a scene and game loop needs a few ticks,
            // or manually call Start/Update. GameTestBase might handle some of this.
            // We will manually call Start and Update in these tests for more control.
        }

        [TestCleanup]
        public void Teardown()
        {
            // Remove all entities from the test scene
            if (testScene != null)
            {
                foreach (var entity in new List<Entity>(testScene.Entities)) // Iterate over a copy
                {
                    testScene.Entities.Remove(entity);
                }
            }
            Game.SceneSystem.SceneInstance = null;
            var physics = Game.SceneSystem.Processors.Get<PhysicsProcessor>();
            if (physics != null)
            {
                Game.SceneSystem.Processors.Remove(physics);
                Game.Processors.Remove(physics);
            }
            testScene?.Dispose();
            testScene = null;
        }

        // --- Mock Components ---

        public class MockTargetable : ScriptComponent, ITargetable
        {
            public bool CanBeTargeted { get; set; } = true;
            public Vector3 TargetOffset { get; set; } = Vector3.Zero;
            public Vector3 GetTargetPosition() => Entity.Transform.Position + TargetOffset;
        }

        public class MockFireStrategy : ScriptComponent, ITurretFireStrategy
        {
            public float FireRate { get; set; } = 1f;
            public int FireCallCount { get; private set; }
            public bool IsReadyToFireValue { get; set; } = true; // Control readiness for tests
            public Entity LastTarget { get; private set; }
            public Vector3 LastMuzzlePosition { get; private set; }
            public Quaternion LastMuzzleRotation { get; private set; }
            private float currentCooldown = 0f;

            public void UpdateCooldown(float deltaTime) 
            {
                if (currentCooldown > 0) currentCooldown -= deltaTime;
                if (currentCooldown < 0) currentCooldown = 0;
            }
            public bool IsReadyToFire() => IsReadyToFireValue && currentCooldown <= 0;

            public bool Fire(Entity ownerTurret, Entity targetEntity, Vector3 muzzlePosition, Quaternion muzzleRotation, float gameDeltaTime)
            {
                if (!IsReadyToFire()) return false;
                FireCallCount++;
                LastTarget = targetEntity;
                LastMuzzlePosition = muzzlePosition;
                LastMuzzleRotation = muzzleRotation;
                currentCooldown = 1.0f / FireRate; // Simulate cooldown
                return true;
            }
            public void Reset() { FireCallCount = 0; LastTarget = null; currentCooldown = 0; }
        }
        
        // --- Helper Methods ---
        private Entity CreateTestEntity(string name, Vector3 position)
        {
            var entity = new Entity(name) { Transform = { Position = position } };
            testScene.Entities.Add(entity);
            return entity;
        }

        private static void SimulateUpdate(ScriptComponent script, float deltaTime)
        {
            // Simulate a game tick for a specific script
            // For SyncScript, Update is called by the system. For ScriptComponent, we can call it directly for tests.
            // This helper is a bit simplistic; a real game loop involves more.
            var game = script.Game;
            if (game == null) return; // Cannot simulate if not in game hierarchy (or if Game is not set)
            
            // Ensure the script's Entity has been added to a scene and Start has been called.
            // For these tests, we'll manage Start/Update calls explicitly.
            var updateTime = new Stride.Games.GameTime(game.UpdateTime.Total, TimeSpan.FromSeconds(deltaTime));
            
            // This is a direct call, usually ScriptSystem would do this.
            // script.Update(updateTime); // Update() is protected for SyncScript
            // Instead, we'll call the public methods or simulate the conditions.
            // For testing specific component logic, direct calls to public methods are fine if Update() itself is simple.
            // For this suite, we will call public methods of the components or their Start/Update if they are ScriptComponent.
        }

        // Placeholder for GameTime, Stride.Games.GameTime
        private void SimulateGameTick(float deltaTime)
        {
            // This would ideally advance the game's time and trigger ScriptSystem.Update
            // For these tests, we'll often call component Update methods directly or test methods that are called by Update.
            // For components that rely on Game.UpdateTime, we might need a way to mock or advance it.
            // For now, we pass deltaTime directly to methods that need it.
            // Example: Game.UpdateTime = new GameTime(Game.UpdateTime.Total + TimeSpan.FromSeconds(deltaTime), TimeSpan.FromSeconds(deltaTime));
            // This is risky if not managed carefully with the actual game loop.
        }


        // --- Test Methods ---

        [TestMethod]
        public void TestTurretPowerRequirement()
        {
            // Setup TurretPiece
            var turretEntity = CreateTestEntity("TestTurret", Vector3.Zero);
            var turretPiece = new TurretPiece();
            turretEntity.Add(turretPiece);

            // Mock Targeting System
            var targetingSystem = new TurretTargetingSystem();
            turretEntity.Add(targetingSystem); // Assuming TurretPiece gets these from its own entity
            turretPiece.TargetingSystem = targetingSystem; // Or however it's assigned

            // Mock Weapon System with MockFireStrategy
            var weaponSystemEntity = CreateTestEntity("WeaponSystem", Vector3.Zero); // Can be same entity or child
            turretEntity.Add(weaponSystemEntity); // For simplicity, add to turret entity
            var weaponSystem = new TurretWeaponSystem();
            var fireStrategy = new MockFireStrategy();
            weaponSystemEntity.Add(weaponSystem);
            weaponSystemEntity.Add(fireStrategy); // Strategy on the same entity as weapon system
            
            // Call Start manually for components
            fireStrategy.Start(); 
            weaponSystem.Start(); // This will find fireStrategy
            targetingSystem.Start();
            turretPiece.Start(); // This will find its systems

            turretPiece.WeaponSystem = weaponSystem; // Ensure TurretPiece knows its weapon system

            // Create a mock target
            var targetEntity = CreateTestEntity("Target", new Vector3(5, 0, 0));
            targetEntity.Add(new MockTargetable());
            targetEntity.Add(new StaticColliderComponent()); // Required for physics overlap
            
            // Initial State: Powered
            turretPiece.IsPowered = true;
            targetingSystem.CurrentTargetFilter = TargetFilter.All; // Ensure it can see the target
            
            // Simulate a scan and target acquisition
            targetingSystem.Update(); // Initial update might trigger scan if timer is due
            for(int i=0; i< (int)(targetingSystem.ScanInterval / 0.016f) + 5; ++i) // Simulate time passing for scan
            {
                 SimulateGameTick(0.016f); // Advance game time notionally
                 targetingSystem.Update(); // Call update to process scan logic
            }
            Assert.IsNotNull(targetingSystem.CurrentTarget, "Target should be acquired when powered.");

            // Simulate turret update - should attempt to fire
            turretPiece.Update(); // TurretPiece update will call WeaponSystem.FireAt
            Assert.IsTrue(fireStrategy.FireCallCount > 0, "Turret should fire when powered and has target.");
            
            fireStrategy.Reset(); // Reset for next check

            // State: Unpowered
            turretPiece.IsPowered = false;
            Log.Info("Setting Turret IsPowered = false");
            
            // Simulate turret update - Target should be cleared
            turretPiece.Update(); 
            Assert.IsNull(targetingSystem.CurrentTarget, "Target should be cleared when turret is unpowered.");
            
            // Even if we manually set a target again, it should not fire
            targetingSystem.CurrentTarget = targetEntity; // Force a target
            turretPiece.Update();
            Assert.IsTrue(fireStrategy.FireCallCount == 0, "Turret should NOT fire when unpowered, even if target exists.");

            // Cleanup
            // Handled by Teardown
        }

        [TestMethod]
        public void TestTurretWeaponSystemWithProjectileStrategy()
        {
            var weaponSystemEntity = CreateTestEntity("WeaponSystem", Vector3.Zero);
            var weaponSystem = new TurretWeaponSystem();
            var projectileStrategy = new ProjectileFireStrategy { FireRate = 2f };
            
            // Create a dummy prefab entity for ProjectilePrefab
            var dummyProjectilePrefabEntity = new Entity("DummyProjectile");
            // A prefab needs at least one component to be valid for some operations, or just be an entity.
            // For this test, BasicTurretProjectile script itself is not strictly needed on the prefab,
            // as we are not testing the projectile's flight, just the strategy's attempt to fire.
            // However, the ProjectileFireStrategy does try to Get<BasicTurretProjectile> from the instance.
            dummyProjectilePrefabEntity.Add(new BasicTurretProjectile()); // Add the script so Get<> doesn't fail log
            var dummyPrefab = new Prefab(dummyProjectilePrefabEntity);
            projectileStrategy.ProjectilePrefab = dummyPrefab;

            weaponSystemEntity.Add(weaponSystem);
            weaponSystemEntity.Add(projectileStrategy);

            // Call Start manually
            projectileStrategy.Start(); // Checks for prefab
            weaponSystem.Start();   // Finds strategy

            var mockTarget = CreateTestEntity("Target", new Vector3(10, 0, 0));
            mockTarget.Add(new MockTargetable());

            Assert.IsTrue(weaponSystem.FireStrategy is ProjectileFireStrategy, "FireStrategy should be ProjectileFireStrategy.");
            Assert.IsTrue(projectileStrategy.IsReadyToFire(), "Strategy should be ready to fire initially.");

            bool fired = weaponSystem.FireAt(mockTarget);
            Assert.IsTrue(fired, "FireAt should return true when strategy fires.");
            // ProjectileFireStrategy logs, but doesn't have an easy "FireCallCount".
            // We can check if it's no longer ready to fire (i.e., cooldown initiated).
            Assert.IsFalse(projectileStrategy.IsReadyToFire(), "Strategy should be on cooldown after firing.");

            // Simulate time passing for cooldown
            float timeToCooldown = 1.0f / projectileStrategy.FireRate;
            projectileStrategy.UpdateCooldown(timeToCooldown + 0.01f); // Advance time past cooldown
            Assert.IsTrue(projectileStrategy.IsReadyToFire(), "Strategy should be ready to fire again after cooldown.");

            // Test firing when ProjectilePrefab is null
            projectileStrategy.ProjectilePrefab = null;
            fired = weaponSystem.FireAt(mockTarget);
            Assert.IsFalse(fired, "FireAt should return false if ProjectilePrefab is null in strategy.");
        }

        [TestMethod]
        public void TestTargetFiltering()
        {
            var targetingEntity = CreateTestEntity("TargetingSystemOwner", Vector3.Zero);
            var targetingSystem = new TurretTargetingSystem { ScanInterval = 0.1f }; // Short scan interval for faster test
            targetingEntity.Add(targetingSystem);
            targetingSystem.Start();

            // Create mock targets
            var playerTarget = CreateTestEntity("PlayerTarget", new Vector3(1, 0, 0));
            playerTarget.Add(new MockTargetable());
            playerTarget.Add(new PlayerMarkerComponent());
            playerTarget.Add(new StaticColliderComponent());


            var creatureTarget = CreateTestEntity("CreatureTarget", new Vector3(2, 0, 0));
            creatureTarget.Add(new MockTargetable());
            creatureTarget.Add(new CreatureMarkerComponent());
            creatureTarget.Add(new StaticColliderComponent());

            var neutralTarget = CreateTestEntity("NeutralTarget", new Vector3(3, 0, 0)); // Furthest, to ensure filter works
            neutralTarget.Add(new MockTargetable());
            neutralTarget.Add(new StaticColliderComponent());
            
            // Helper to simulate scan cycles
            async Task PerformScanCycle()
            {
                targetingSystem.Update(); // Force scan logic if timer is already 0
                await Task.Delay(TimeSpan.FromSeconds(targetingSystem.ScanInterval + 0.05f)); // Wait for scan
                targetingSystem.Update(); // Process results of scan
            }

            // Test Players filter
            Log.Info("Testing Player Filter");
            targetingSystem.CurrentTargetFilter = TargetFilter.Players;
            PerformScanCycle().Wait(); // Using .Wait() for simplicity in test, consider async test method
            Assert.AreEqual(playerTarget, targetingSystem.CurrentTarget, "Should target player with Players filter.");

            // Test Creatures filter
            Log.Info("Testing Creature Filter");
            targetingSystem.CurrentTargetFilter = TargetFilter.Creatures;
            PerformScanCycle().Wait();
            Assert.AreEqual(creatureTarget, targetingSystem.CurrentTarget, "Should target creature with Creatures filter.");
            
            // Test All filter (should pick closest, which is playerTarget)
            Log.Info("Testing All Filter");
            targetingSystem.CurrentTargetFilter = TargetFilter.All;
            PerformScanCycle().Wait();
            Assert.AreEqual(playerTarget, targetingSystem.CurrentTarget, "Should target closest (player) with All filter.");
        }
        
        [TestMethod]
        public void TestWeaponFiringAndCooldownOnStrategy()
        {
            var strategyEntity = CreateTestEntity("StrategyOwner", Vector3.Zero);
            var strategy = new MockFireStrategy { FireRate = 2.0f };
            strategyEntity.Add(strategy);
            strategy.Start();

            Assert.IsTrue(strategy.IsReadyToFire(), "Strategy should be ready to fire initially.");
            
            var mockOwner = CreateTestEntity("Owner", Vector3.Zero);
            var mockTarget = CreateTestEntity("Target", Vector3.UnitX);

            bool fired = strategy.Fire(mockOwner, mockTarget, Vector3.Zero, Quaternion.Identity, 0.016f);
            Assert.IsTrue(fired, "Strategy should fire successfully.");
            Assert.AreEqual(1, strategy.FireCallCount, "FireCallCount should be 1 after one fire attempt.");
            Assert.IsFalse(strategy.IsReadyToFire(), "Strategy should not be ready to fire immediately after firing.");

            // Simulate time for cooldown
            float deltaTime = 0.016f;
            float totalTime = 0f;
            float cooldownDuration = 1.0f / strategy.FireRate; // Should be 0.5s

            while(totalTime < cooldownDuration)
            {
                strategy.UpdateCooldown(deltaTime);
                totalTime += deltaTime;
                if(totalTime < cooldownDuration)
                    Assert.IsFalse(strategy.IsReadyToFire(), $"Strategy should still be on cooldown at {totalTime}s.");
            }
            strategy.UpdateCooldown(deltaTime); // One more tick to ensure cooldown passes
             Assert.IsTrue(strategy.IsReadyToFire(), "Strategy should be ready to fire after cooldown period.");

            // Test firing again
            fired = strategy.Fire(mockOwner, mockTarget, Vector3.Zero, Quaternion.Identity, deltaTime);
            Assert.IsTrue(fired, "Strategy should fire again after cooldown.");
            Assert.AreEqual(2, strategy.FireCallCount, "FireCallCount should be 2 after second fire.");
        }

        [TestMethod]
        public void TestTurretPieceCoordination()
        {
            // Setup TurretPiece
            var turretEntity = CreateTestEntity("TestTurret", Vector3.Zero);
            var turretPiece = new TurretPiece();
            turretEntity.Add(turretPiece);
            turretPiece.TurretYawPart = CreateTestEntity("YawPart", Vector3.Zero); // Simplified setup
            turretPiece.TurretPitchPart = CreateTestEntity("PitchPart", Vector3.Zero);
            turretPiece.TurretPitchPart.Transform.Parent = turretPiece.TurretYawPart.Transform;


            // Mock Targeting System
            var targetingSystem = new TurretTargetingSystem { TargetingRange = 20f, ScanInterval = 0.1f };
            turretEntity.Add(targetingSystem);
            turretPiece.TargetingSystem = targetingSystem;

            // Weapon System with MockFireStrategy
            var weaponSystem = new TurretWeaponSystem();
            var fireStrategy = new MockFireStrategy { FireRate = 5f }; // High fire rate
            turretEntity.Add(weaponSystem); // Add to same entity for simplicity
            turretEntity.Add(fireStrategy); 
            turretPiece.WeaponSystem = weaponSystem;

            // Call Start for all components
            fireStrategy.Start();
            weaponSystem.Start(); // Finds strategy
            targetingSystem.Start();
            turretPiece.Start(); // Finds its systems

            // Create a mock target
            var targetEntity = CreateTestEntity("Target", new Vector3(10, 2, 0)); // Position it
            targetEntity.Add(new MockTargetable());
            targetEntity.Add(new StaticColliderComponent());

            // Simulate game running for a bit to allow target acquisition and aiming
            turretPiece.IsPowered = true;
            targetingSystem.CurrentTargetFilter = TargetFilter.All;

            for (int i = 0; i < 100; i++) // Simulate ~1.6 seconds of updates (enough for scan, aim, fire)
            {
                float dt = 0.016f;
                SimulateGameTick(dt); // Notionally advance game time

                // Manually call updates in order
                targetingSystem.Update();
                turretPiece.Update(); // This will internally call weaponSystem.UpdateCooldown via its strategy, then potentially FireAt
                weaponSystem.Update(); // weaponSystem.Update calls strategy.UpdateCooldown
                
                if (targetingSystem.CurrentTarget != null && fireStrategy.FireCallCount > 0)
                    break; // Stop if target acquired and fired
                
                // For tests, Task.Delay might be too slow or unreliable.
                // Direct calls are better for unit logic.
            }
            
            Assert.IsNotNull(targetingSystem.CurrentTarget, "Turret should have acquired the target.");
            Assert.AreEqual(targetEntity, targetingSystem.CurrentTarget, "Turret should target the correct entity.");
            Assert.IsTrue(fireStrategy.FireCallCount > 0, "Turret should have attempted to fire at the target.");
            
            // Check if TurretYawPart and TurretPitchPart tried to rotate (simplified check)
            // This requires more complex math to verify exact rotation, so we'll skip for this test
            // but one might check if TurretYawPart.Transform.Rotation is not Quaternion.Identity if target is not forward.
            // For example, if target is at (10,0,0) and turret at (0,0,0), yaw should be identity.
            // If target at (0,0,10), yaw should be different.
            // This part is more complex and might be integration testing.
        }
    }
}
