// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Weapons.Ranged; // For Pistol
using FirstPersonShooter.Player; // For PlayerInput (to mock OwnerEntity with Camera)

namespace FirstPersonShooter.Tests
{
    public class PistolMechanicsTests : SyncScript
    {
        private int testsRun = 0;
        private int testsPassed = 0;

        // Mock a simple game time progression for reload test
        private float mockDeltaTime = 0.016f; // Approx 60 FPS

        public override void Start()
        {
            Log.Info("PistolMechanicsTests: Starting tests...");

            TestAmmoDecrement();
            TestFireOnEmptyMagAutoReload();
            TestReloadLogic();
            TestNoActionWhileReloading();

            Log.Info($"PistolMechanicsTests: Finished. {testsPassed}/{testsRun} tests passed.");
        }

        private void AssertTrue(bool condition, string testName, string message = "")
        {
            testsRun++;
            if (condition)
            {
                testsPassed++;
                Log.Info($"[SUCCESS] {testName} {message}");
            }
            else
            {
                Log.Error($"[FAILURE] {testName} {message}");
            }
        }
        
        private void AssertFalse(bool condition, string testName, string message = "")
        {
            AssertTrue(!condition, testName, message);
        }

        private void AssertEquals(int expected, int actual, string testName, string message = "")
        {
            testsRun++;
            if (expected == actual)
            {
                testsPassed++;
                Log.Info($"[SUCCESS] {testName} {message}");
            }
            else
            {
                Log.Error($"[FAILURE] {testName} - Expected '{expected}', got '{actual}' {message}");
            }
        }
        
        private void AssertEquals(float expected, float actual, string testName, string message = "")
        {
            testsRun++;
            bool areEqual = System.Math.Abs(expected - actual) < 0.0001f;
            if (areEqual)
            {
                testsPassed++;
                Log.Info($"[SUCCESS] {testName} {message}");
            }
            else
            {
                Log.Error($"[FAILURE] {testName} - Expected '{expected}', got '{actual}' {message}");
            }
        }

        // Helper to create a Pistol with a mock owner for testing PrimaryAction's raycast
        private Pistol CreateTestPistolWithOwner()
        {
            var ownerEntity = new Entity("TestPistolOwner");
            var playerInput = new PlayerInput(); // PlayerInput is needed by Pistol to find camera
            var cameraComponent = new CameraComponent(); // Mock camera
            
            // Stride requires components to be part of an entity to be 'live'
            var cameraEntity = new Entity("TestPistolOwnerCamera");
            cameraEntity.Add(cameraComponent);
            // Add cameraEntity as a child of ownerEntity or ensure PlayerInput.Camera can be set.
            // For simplicity, assign camera directly if PlayerInput allows, or ensure Get<PlayerInput>().Camera works.
            // PlayerInput.Camera is a public property.
            playerInput.Camera = cameraComponent;
            ownerEntity.Add(playerInput);
            
            // Add camera to scene so its ViewMatrix can be calculated (or ensure it has a valid default)
            // This is tricky in unit tests. Pistol's PrimaryAction will try to use playerInput.Camera.ViewMatrix.
            // For now, we rely on default behavior or that Stride initializes it enough.
            // If raycasting needs to be truly tested, a scene setup is better.
            // Here, we focus on ammo and reload, raycast is secondary for these tests.

            var pistolEntity = new Entity("TestPistol");
            var pistol = new Pistol();
            pistolEntity.Add(pistol);
            pistol.OnEquip(ownerEntity); // Set OwnerEntity
            
            // Call Start on components to mimic Stride's lifecycle if they have init logic
            // playerInput.Start(); // if any
            // pistol.Start(); // if any
            
            return pistol;
        }


        private void TestAmmoDecrement()
        {
            var testName = "TestAmmoDecrement";
            Log.Info($"PistolMechanicsTests: Running {testName}...");

            var pistol = CreateTestPistolWithOwner();
            int initialAmmo = pistol.CurrentAmmo;

            pistol.PrimaryAction(); // Fire once

            AssertEquals(initialAmmo - 1, pistol.CurrentAmmo, $"{testName} - Ammo decreased by 1");
            // Durability check (optional, but good to note it's also affected)
            // AssertEquals(initialDurability - 0.1f, pistol.Durability, $"{testName} - Durability decreased");
        }

        private void TestFireOnEmptyMagAutoReload()
        {
            var testName = "TestFireOnEmptyMagAutoReload";
            Log.Info($"PistolMechanicsTests: Running {testName}...");

            var pistol = CreateTestPistolWithOwner();
            // Manually set ammo to 0. In Pistol, CurrentAmmo has private set, so need to simulate this.
            // For this test, let's assume we can fire it until empty.
            int maxAmmo = pistol.MaxAmmo;
            for(int i=0; i < maxAmmo; ++i)
            {
                if(pistol.CurrentAmmo > 0) pistol.PrimaryAction(); // Empty the magazine
            }
            AssertEquals(0, pistol.CurrentAmmo, $"{testName} - Ammo is 0 after firing all shots");

            // Pistol's Update needs to run to clear attackCooldownRemaining if it was set by last shot.
            // Simulate one frame pass to ensure attack cooldown is not blocking the reload trigger.
            var gameTime = new GameTime(); 
            for(int i=0; i < (int)( (1.0f/pistol.AttackRate) / mockDeltaTime) + 1 ; ++i) 
            {
                 // Accessing Game.UpdateTime directly is not possible in SyncScript's Start method easily.
                 // Pistol's Update method uses Game.UpdateTime.
                 // We will call Update on pistol and assume it gets a valid delta, or mock it.
                 // For this test, the key is that PrimaryAction on empty calls Reload.
                 // The internal cooldowns are managed by Pistol's Update.
            }
            // We cannot directly pass a mock GameTime to pistol.Update().
            // The reload logic in Pistol.Update relies on Game.UpdateTime.
            // This test will check if Reload() is *initiated*.

            pistol.PrimaryAction(); // Attempt to fire on empty mag

            // Check if reload was initiated (isReloading is private, check reloadCooldownRemaining)
            // Accessing private fields like 'isReloading' or 'reloadCooldownRemaining' is not direct.
            // We infer from behavior: if Reload() was called, reloadCooldownRemaining would be > 0.
            // For a white-box test, we'd need accessors or make fields internal.
            // Here, we check if CurrentAmmo is still 0 (shot didn't happen) and if a reload *started*.
            // The actual reload completion is tested in TestReloadLogic.
            // Pistol's Reload() sets reloadCooldownRemaining = ReloadTime.
            // We can't query reloadCooldownRemaining directly.
            // We can check if it's NOT MaxAmmo (which it would be if it instantly reloaded somehow)
            // This test is tricky without more visibility into Pistol's state.
            // Let's assume Reload() was called if CurrentAmmo is still 0.
            // The log "Pistol reloading..." from Pistol.Reload() would be an indicator.
            
            // A better check: if ammo is 0, and we fire, it should call Reload().
            // Reload() sets isReloading = true and reloadCooldownRemaining = ReloadTime.
            // If we then call Update() for ReloadTime, ammo should be full.
            // So, this test will morph into: fire on empty, then simulate time, then check ammo.

            Log.Info($"{testName}: Fired on empty. Simulating reload time...");
            float totalSimulatedTime = 0f;
            // GameTime is needed by Pistol.Update(). We create a dummy one.
            // Stride's Update methods are usually called by the engine with a valid GameTime.
            // In a test script's Start method, Game.UpdateTime might not be ideal or always available as expected.
            // We will manually call Update on the Pistol.
            // This is a significant simplification of Stride's game loop.
            while(totalSimulatedTime < pistol.ReloadTime + mockDeltaTime) 
            {
                // Manually calling Update on the component. This is not standard Stride practice for game logic
                // but can be used in tests if the Update method is self-contained or dependencies are managed.
                // Pistol's Update uses Game.UpdateTime.Elapsed.TotalSeconds.
                // This will use the actual frame time of the test runner, which is not ideal for unit test determinism.
                // For this test, we'll assume Update makes progress on reload.
                pistol.Update(gameTime); // Pass a dummy gameTime. Actual delta is from Game.UpdateTime
                totalSimulatedTime += mockDeltaTime; // Use our mock delta for loop control
                // To make this robust, Pistol's Update() should ideally accept a deltaTime.
            }
            
            // This part of the test might be flaky due to reliance on actual frame times if Game.UpdateTime is used by Pistol.
            // If Pistol.Update strictly used a passed GameTime, it would be more testable.
            // Given Pistol.Update uses Game.UpdateTime, we can only hope enough real time passed.
            // A more reliable test would be to expose 'isReloading' or 'reloadCooldownRemaining'.
            // For now, we expect the log "Pistol reloaded" to appear from Pistol.Update.
            AssertEquals(pistol.MaxAmmo, pistol.CurrentAmmo, $"{testName} - Ammo is full after auto-reload sequence");
        }

        private void TestReloadLogic()
        {
            var testName = "TestReloadLogic";
            Log.Info($"PistolMechanicsTests: Running {testName}...");

            var pistol = CreateTestPistolWithOwner();
            pistol.PrimaryAction(); // Fire once to reduce ammo
            int ammoAfterShot = pistol.CurrentAmmo;
            AssertTrue(ammoAfterShot < pistol.MaxAmmo, $"{testName} - Ammo reduced before reload");

            pistol.Reload();
            // Assert that pistol is now in reloading state (again, private fields)
            // The log "Pistol reloading..." should appear.

            // Simulate passage of time by calling Update
            float totalSimulatedTime = 0f;
            var gameTime = new GameTime(); // Dummy
            while(totalSimulatedTime < pistol.ReloadTime + mockDeltaTime)
            {
                pistol.Update(gameTime); // Relies on Game.UpdateTime within Pistol.Update()
                totalSimulatedTime += mockDeltaTime;
            }

            AssertEquals(pistol.MaxAmmo, pistol.CurrentAmmo, $"{testName} - Ammo is full after reload");
            // AssertFalse(pistol.isReloading, $"{testName} - isReloading is false after reload"); // Requires accessor
        }

        private void TestNoActionWhileReloading()
        {
            var testName = "TestNoActionWhileReloading";
            Log.Info($"PistolMechanicsTests: Running {testName}...");

            var pistol = CreateTestPistolWithOwner();
            pistol.PrimaryAction(); // Reduce ammo by one
            int ammoBeforeReload = pistol.CurrentAmmo;

            pistol.Reload(); // Start reloading
            // Log "Pistol reloading..." should appear.
            // isReloading is true internally.

            // Try to fire while reloading
            pistol.PrimaryAction(); 
            
            AssertEquals(ammoBeforeReload, pistol.CurrentAmmo, $"{testName} - Ammo did not change from firing while reloading");
            // The log "Pistol cannot fire: currently reloading." should appear if that log is active.

            // Let reload finish to ensure no other side effects
            float totalSimulatedTime = 0f;
            var gameTime = new GameTime(); // Dummy
            while(totalSimulatedTime < pistol.ReloadTime + mockDeltaTime)
            {
                pistol.Update(gameTime);
                totalSimulatedTime += mockDeltaTime;
            }
            AssertEquals(pistol.MaxAmmo, pistol.CurrentAmmo, $"{testName} - Ammo is full after letting reload complete");
        }
    }
}
