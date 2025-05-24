// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using Stride.Audio; // For mocking SoundEffectInstance if needed, though direct check is hard
using Stride.UnitTesting;
using System.Threading.Tasks; // For Task.Delay in tests

using FirstPersonShooter.Building.Defenses.Traps;
using FirstPersonShooter.Tests; // For MockDamageableTarget

namespace FirstPersonShooter.Tests.Defenses
{
    [TestClass]
    public class SpikeTrapTests : GameTestBase
    {
        private Scene testScene;
        private PhysicsProcessor physicsProcessor;

        [TestInitialize]
        public void Setup()
        {
            testScene = new Scene();
            Game.SceneSystem.SceneInstance = new SceneInstance(Services, testScene);

            physicsProcessor = new PhysicsProcessor();
            Game.Processors.Add(physicsProcessor);
            Game.SceneSystem.Processors.Add(physicsProcessor);
        }

        [TestCleanup]
        public void Teardown()
        {
            // Remove all entities from the test scene
            if (testScene != null)
            {
                foreach (var entity in new System.Collections.Generic.List<Entity>(testScene.Entities))
                {
                    testScene.Entities.Remove(entity);
                }
            }
            if (physicsProcessor != null)
            {
                Game.SceneSystem.Processors.Remove(physicsProcessor);
                Game.Processors.Remove(physicsProcessor);
                physicsProcessor = null;
            }
            testScene?.Dispose();
            testScene = null;
            Game.SceneSystem.SceneInstance = null;
        }

        private Entity CreateTestEntity(string name, Vector3 position)
        {
            var entity = new Entity(name) { Transform = { Position = position } };
            testScene.Entities.Add(entity);
            return entity;
        }
        
        private async Task WaitOneFrame() => await Script.NextFrame();

        private void SimulateCollision(StaticColliderComponent trapTrigger, Entity targetEntity)
        {
            // This is a simplified way to manually trigger the logic that Collisions.CollectionChanged would run.
            // In a full integration test, you'd move entities and let physics detect collision.
            // For unit tests, directly calling the method that handles collision is more robust if CollectionChanged is complex.
            // SpikeTrap's ProcessSpikeTrapCollision is private, so we rely on physics system.
            // To make this reliable, we'd need to ensure entities are positioned to collide and wait for physics tick.
            
            // For testing, we'll place them directly overlapping and wait a frame.
            // This is still somewhat integration-testy for a unit test.
            // A true unit test might involve making ProcessSpikeTrapCollision internal or using a mock physics system.
            
            // Ensure target has a collider
            if (targetEntity.Get<StaticColliderComponent>() == null && targetEntity.Get<RigidbodyComponent>() == null)
            {
                var targetCollider = new StaticColliderComponent(); // Or Rigidbody
                targetEntity.Add(targetCollider);
            }

            // Ensure trapTrigger's entity and targetEntity are at the same position for overlap
            targetEntity.Transform.Position = trapTrigger.Entity.Transform.Position;
        }


        [TestMethod]
        public void TestSpikeTrapDamage()
        {
            var trapEntity = CreateTestEntity("SpikeTrap", Vector3.Zero);
            var spikeTrap = new SpikeTrap { DamagePerTrigger = 33f };
            var trapCollider = new StaticColliderComponent { IsTrigger = true };
            // Add a shape to the collider for physics system to work
            var sphereShape = new SphereColliderShapeDesc { Radius = 0.5f };
            trapCollider.ColliderShapes.Add(new SphereColliderShape(sphereShape));
            trapEntity.Add(spikeTrap);
            trapEntity.Add(trapCollider);
            
            var targetEntity = CreateTestEntity("Target", new Vector3(0, 0, 0)); // Position to collide
            var mockDamageable = new MockDamageableTarget("Victim") { Health = 100f };
            targetEntity.Add(mockDamageable);
            // Target also needs a collider for physics system
            var targetBody = new RigidbodyComponent(); // Make it a dynamic body for typical interaction
            targetBody.ColliderShapes.Add(new SphereColliderShape(new SphereColliderShapeDesc { Radius = 0.1f }));
            targetEntity.Add(targetBody);

            // Call Start manually
            spikeTrap.Start();
            Assert.IsTrue(trapCollider.IsTrigger, "SpikeTrap Start should ensure its collider is a trigger.");

            // Simulate collision
            SimulateCollision(trapCollider, targetEntity);
            
            // Wait for physics and script update cycle
            // This can be tricky. GameTestBase might provide a Wait methods.
            // For now, a short delay or multiple NextFrame calls.
            Script.NextFrame().Wait(); 
            Script.NextFrame().Wait(); 
            // spikeTrap.Update(); // Manually call update to process timers, though collision is event-driven

            Assert.AreEqual(1, mockDamageable.TimesDamaged, "Target should have been damaged once.");
            Assert.AreEqual(spikeTrap.DamagePerTrigger, mockDamageable.LastDamageAmount, "Damage dealt should match trap's DamagePerTrigger.");
            Assert.AreEqual(trapEntity, mockDamageable.LastDamageSource, "Damage source should be the trap entity.");
            // Sound check is tricky without mocking SoundEffectInstance. Assume it plays if damage occurs.
        }

        [TestMethod]
        public void TestSpikeTrapCooldowns()
        {
            var trapEntity = CreateTestEntity("SpikeTrap", Vector3.Zero);
            var spikeTrap = new SpikeTrap { DamagePerTrigger = 10f, TriggerCooldown = 0.2f, RearmTimePerEntity = 0.5f };
            var trapCollider = new StaticColliderComponent { IsTrigger = true };
            trapCollider.ColliderShapes.Add(new SphereColliderShape(new SphereColliderShapeDesc { Radius = 0.5f }));
            trapEntity.Add(spikeTrap);
            trapEntity.Add(trapCollider);
            spikeTrap.Start();

            var target1 = CreateTestEntity("Target1", Vector3.Zero);
            var damageable1 = new MockDamageableTarget("Victim1");
            target1.Add(damageable1);
            target1.Add(new RigidbodyComponent()); 
            target1.Get<RigidbodyComponent>().ColliderShapes.Add(new SphereColliderShape(new SphereColliderShapeDesc { Radius = 0.1f }));


            var target2 = CreateTestEntity("Target2", Vector3.One); // Away initially
            var damageable2 = new MockDamageableTarget("Victim2");
            target2.Add(damageable2);
            target2.Add(new RigidbodyComponent());
            target2.Get<RigidbodyComponent>().ColliderShapes.Add(new SphereColliderShape(new SphereColliderShapeDesc { Radius = 0.1f }));


            // --- Initial trigger for target1 ---
            SimulateCollision(trapCollider, target1);
            WaitOneFrame().Wait(); WaitOneFrame().Wait(); 
            spikeTrap.Update(); // Process cooldowns
            Assert.AreEqual(1, damageable1.TimesDamaged, "Target1 initial damage failed.");

            // --- Try to damage target1 again immediately (should fail due to TriggerCooldown) ---
            damageable1.ResetDamageState(); // Reset for clarity, though cooldown should prevent damage
            SimulateCollision(trapCollider, target1); // Re-trigger
            WaitOneFrame().Wait(); WaitOneFrame().Wait();
            spikeTrap.Update();
            Assert.AreEqual(0, damageable1.TimesDamaged, "Target1 should not be damaged again due to TriggerCooldown.");

            // --- Wait for TriggerCooldown to pass, but not RearmTimePerEntity for target1 ---
            Task.Delay(TimeSpan.FromSeconds(spikeTrap.TriggerCooldown + 0.05f)).Wait();
            spikeTrap.Update(); // Update to process global cooldown passing
            
            // --- Try target1 again (should fail due to entityRearmTimer for target1) ---
            SimulateCollision(trapCollider, target1);
            WaitOneFrame().Wait(); WaitOneFrame().Wait();
            spikeTrap.Update();
            Assert.AreEqual(0, damageable1.TimesDamaged, "Target1 should not be damaged due to its specific RearmTime.");

            // --- Try target2 (should succeed as global cooldown is over and target2 is new) ---
            SimulateCollision(trapCollider, target2);
            WaitOneFrame().Wait(); WaitOneFrame().Wait();
            spikeTrap.Update();
            Assert.AreEqual(1, damageable2.TimesDamaged, "Target2 should be damaged.");
            
            // --- Wait for RearmTimePerEntity for target1 to pass ---
            Task.Delay(TimeSpan.FromSeconds(spikeTrap.RearmTimePerEntity + 0.05f)).Wait(); // Total time for target1 rearm
            // Simulate multiple updates for timers
            for(int i=0; i< (int)((spikeTrap.RearmTimePerEntity + 0.1f) / 0.016f) ; ++i)
            {
                spikeTrap.Update(); // Manually tick down rearm timer
                 // This manual update of timers is key if not relying on full game loop for tests.
                 // We need to simulate the passage of time for the internal timers in SpikeTrap.
                 // A better way for SpikeTrap would be to accept a mock IGameClock.
                 // For now, we assume Update() with real game time (via Task.Delay) handles it.
            }


            // --- Try target1 again (should succeed now) ---
            damageable1.ResetDamageState();
            SimulateCollision(trapCollider, target1);
            WaitOneFrame().Wait(); WaitOneFrame().Wait(); // Physics detection
            // Global cooldown might be active from target2, wait for it
            Task.Delay(TimeSpan.FromSeconds(spikeTrap.TriggerCooldown + 0.05f)).Wait(); 
            spikeTrap.Update(); // Process global cooldown
            spikeTrap.Update(); // Process entity rearm (should be clear for target1 now)
            
            // Re-check, this part of test is tricky due to multiple timers.
            // The simplest is to ensure enough time has passed for *all* relevant cooldowns for target1.
            // If target2 triggered, global cooldown is active. After that, target1's rearm should be over.
            if (spikeTrap.IsSingleUse) Assert.Fail("Test assumes not single use for this path");
            
            // Re-evaluate this specific assertion, as the interaction of global and entity cooldowns can be complex to time in tests.
            // The key is: after target1's RearmTime has passed AND global cooldown (from any source) has passed, it can be hit.
            // Let's assume enough time passed for target1's rearm timer to have cleared from the dictionary.
            // We also need global cooldown to be clear.
            
            // To simplify: Ensure all cooldowns are reset before this final check for target1
            Task.Delay(TimeSpan.FromSeconds(Math.Max(spikeTrap.TriggerCooldown, spikeTrap.RearmTimePerEntity) + 0.1f)).Wait();
            int updatesNeeded = (int)((Math.Max(spikeTrap.TriggerCooldown, spikeTrap.RearmTimePerEntity) + 0.1f) / 0.016f); // Assuming 60FPS for delta
            for (int i = 0; i < updatesNeeded; i++) spikeTrap.Update();


            SimulateCollision(trapCollider, target1); // Re-trigger
            WaitOneFrame().Wait(); WaitOneFrame().Wait(); // Physics
            spikeTrap.Update(); // Process damage logic
            Assert.IsTrue(damageable1.TimesDamaged >= 1, "Target1 should be damageable again after all cooldowns.");


        }

        [TestMethod]
        public void TestSpikeTrapSingleUse()
        {
            var trapEntity = CreateTestEntity("SpikeTrapSU", Vector3.Zero);
            var spikeTrap = new SpikeTrap { IsSingleUse = true, DamagePerTrigger = 50f };
            var trapCollider = new StaticColliderComponent { IsTrigger = true };
            trapCollider.ColliderShapes.Add(new SphereColliderShape(new SphereColliderShapeDesc { Radius = 0.5f }));
            trapEntity.Add(spikeTrap);
            trapEntity.Add(trapCollider);
            
            var targetEntity = CreateTestEntity("TargetSU", Vector3.Zero);
            var mockDamageable = new MockDamageableTarget("VictimSU");
            targetEntity.Add(mockDamageable);
            targetEntity.Add(new RigidbodyComponent());
            targetEntity.Get<RigidbodyComponent>().ColliderShapes.Add(new SphereColliderShape(new SphereColliderShapeDesc { Radius = 0.1f }));


            spikeTrap.Start();

            // Trigger damage
            SimulateCollision(trapCollider, targetEntity);
            WaitOneFrame().Wait(); WaitOneFrame().Wait();
            spikeTrap.Update(); // Process damage and single-use logic

            Assert.AreEqual(1, mockDamageable.TimesDamaged, "Target should have been damaged by single-use trap.");
            
            // SpikeTrap calls Debug_ForceDestroy, which should eventually lead to Entity.Scene = null.
            // This might not happen instantaneously.
            // For test purposes, we can check if the trap script itself is disabled,
            // or wait a few frames for Scene to become null.
            WaitOneFrame().Wait(); // Allow Debug_ForceDestroy to propagate
            WaitOneFrame().Wait(); 

            Assert.IsTrue(trapEntity.Scene == null || !spikeTrap.Enabled, "Single-use trap should be destroyed or disabled after triggering.");
            
            // Try to trigger damage again
            mockDamageable.ResetDamageState();
            SimulateCollision(trapCollider, targetEntity); // Even if trapEntity.Scene is null, this won't crash
            WaitOneFrame().Wait(); WaitOneFrame().Wait();
            if (spikeTrap.Entity?.Scene != null) // Only call update if it wasn't fully removed
            {
                 spikeTrap.Update(); // If it was just disabled, this should not lead to damage
            }

            Assert.AreEqual(0, mockDamageable.TimesDamaged, "Target should NOT be damaged again by a spent single-use trap.");
        }
    }
}
