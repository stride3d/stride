// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Events; // For EventReceiver
using FirstPersonShooter.Weapons; // For BaseWeapon
// using FirstPersonShooter.Weapons.Melee; // For Hatchet if testing directly

namespace FirstPersonShooter.Tests
{
    // --- Mock Damageable Weapon ---
    public class MockDamageableWeapon : BaseWeapon
    {
        public bool PrimaryActionPerformed { get; private set; }
        public bool SecondaryActionPerformed { get; private set; }

        public override void PrimaryAction()
        {
            base.PrimaryAction(); // Calls IsBroken check
            if (IsBroken) return;
            PrimaryActionPerformed = true;
            // Log.Info("MockDamageableWeapon: PrimaryAction executed.");
        }

        public override void SecondaryAction()
        {
            base.SecondaryAction(); // Calls IsBroken check
            if (IsBroken) return;
            SecondaryActionPerformed = true;
            // Log.Info("MockDamageableWeapon: SecondaryAction executed.");
        }

        public void ResetActionFlags()
        {
            PrimaryActionPerformed = false;
            SecondaryActionPerformed = false;
        }
    }

    // --- WeaponDurabilityTests ---
    public class WeaponDurabilityTests : SyncScript
    {
        private int testsRun = 0;
        private int testsPassed = 0;
        private bool weaponBrokeEventReceived = false;
        private EventReceiver<object> weaponBrokeListener; // Using object as EventKey is parameterless

        public override void Start()
        {
            Log.Info("WeaponDurabilityTests: Starting tests...");
            
            // Setup listener for WeaponBrokeEventKey for TestWeaponBrokeEvent
            // Note: EventKey is static, so this listener is global for its lifetime.
            // For more isolated tests, specific event instances or a different mechanism might be needed.
            weaponBrokeListener = new EventReceiver<object>(BaseWeapon.WeaponBrokeEventKey);
            BaseWeapon.WeaponBrokeEventKey.AddListener(OnWeaponBroke);


            TestReceiveDamage();
            TestWeaponBreaking();
            TestWeaponBrokeEvent(); // This will rely on the listener set up above
            TestActionOnBrokenWeapon();

            Log.Info($"WeaponDurabilityTests: Finished. {testsPassed}/{testsRun} tests passed.");

            // Clean up listener
            BaseWeapon.WeaponBrokeEventKey.RemoveListener(OnWeaponBroke);
            weaponBrokeListener.Dispose(); // Dispose the receiver
        }
        
        private void OnWeaponBroke(object data) // EventKey is parameterless, so data is null/default
        {
            weaponBrokeEventReceived = true;
            // Log.Info("WeaponDurabilityTests: WeaponBrokeEventKey received!");
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
        
        private void AssertEquals(float expected, float actual, string testName, string message = "")
        {
            testsRun++;
            // Using a small epsilon for float comparison
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

        private void TestReceiveDamage()
        {
            var testName = "TestReceiveDamage";
            Log.Info($"WeaponDurabilityTests: Running {testName}...");

            var weaponEntity = new Entity("DurabilityTestWeapon_ReceiveDmg");
            var weapon = new MockDamageableWeapon { Durability = 100f };
            weaponEntity.Add(weapon);

            weapon.ReceiveDamage(30f);
            AssertEquals(70f, weapon.Durability, $"{testName} - Durability reduced correctly");
            AssertFalse(weapon.IsBroken, $"{testName} - Weapon is not broken");

            weapon.ReceiveDamage(0f); // Test with zero damage
            AssertEquals(70f, weapon.Durability, $"{testName} - Durability unchanged with 0 damage");
        }

        private void TestWeaponBreaking()
        {
            var testName = "TestWeaponBreaking";
            Log.Info($"WeaponDurabilityTests: Running {testName}...");

            var weaponEntity = new Entity("DurabilityTestWeapon_Breaking");
            var weapon = new MockDamageableWeapon { Durability = 50f };
            weaponEntity.Add(weapon);

            weapon.ReceiveDamage(50f); // Exact break
            AssertEquals(0f, weapon.Durability, $"{testName} - Durability clamped at 0 on exact break");
            AssertTrue(weapon.IsBroken, $"{testName} - Weapon is broken on exact break");

            // Reset for overkill test
            weaponEntity = new Entity("DurabilityTestWeapon_BreakingOverkill");
            weapon = new MockDamageableWeapon { Durability = 50f };
            weaponEntity.Add(weapon);
            
            weapon.ReceiveDamage(100f); // Overkill
            AssertEquals(0f, weapon.Durability, $"{testName} - Durability clamped at 0 on overkill");
            AssertTrue(weapon.IsBroken, $"{testName} - Weapon is broken on overkill");

            // Test ReceiveDamage on already broken weapon
            weapon.ReceiveDamage(10f);
            AssertEquals(0f, weapon.Durability, $"{testName} - Durability remains 0 if damaged while broken");
            AssertTrue(weapon.IsBroken, $"{testName} - Weapon remains broken if damaged while broken");
        }

        private void TestWeaponBrokeEvent()
        {
            var testName = "TestWeaponBrokeEvent";
            Log.Info($"WeaponDurabilityTests: Running {testName}...");
            
            weaponBrokeEventReceived = false; // Reset flag before test

            var weaponEntity = new Entity("DurabilityTestWeapon_Event");
            var weapon = new MockDamageableWeapon { Durability = 10f };
            weaponEntity.Add(weapon);
            
            // Trigger the event by breaking the weapon
            weapon.ReceiveDamage(15f); 

            // Stride event processing is typically done between frames or at specific points.
            // For a SyncScript test, if the broadcast is immediate and the listener is synchronous,
            // the flag might be set. However, it's safer to check after a potential event processing delay.
            // In this test setup, the broadcast is synchronous within ReceiveDamage.
            
            AssertTrue(weapon.IsBroken, $"{testName} - Weapon is confirmed broken for event test");
            AssertTrue(weaponBrokeEventReceived, $"{testName} - WeaponBrokeEventKey was broadcast and received");

            // Test that event is not fired again if already broken
            weaponBrokeEventReceived = false; // Reset flag
            weapon.ReceiveDamage(5f); // Further damage to already broken weapon
            AssertFalse(weaponBrokeEventReceived, $"{testName} - WeaponBrokeEventKey not broadcast again for already broken weapon");
        }

        private void TestActionOnBrokenWeapon()
        {
            var testName = "TestActionOnBrokenWeapon";
            Log.Info($"WeaponDurabilityTests: Running {testName}...");

            var weaponEntity = new Entity("DurabilityTestWeapon_ActionBroken");
            var weapon = new MockDamageableWeapon { Durability = 20f };
            weaponEntity.Add(weapon);
            weapon.ResetActionFlags();

            // Break the weapon
            weapon.ReceiveDamage(25f);
            AssertTrue(weapon.IsBroken, $"{testName} - Weapon is broken before action test");

            // Attempt actions
            weapon.PrimaryAction();
            AssertFalse(weapon.PrimaryActionPerformed, $"{testName} - PrimaryAction did not execute on broken weapon");

            weapon.SecondaryAction();
            AssertFalse(weapon.SecondaryActionPerformed, $"{testName} - SecondaryAction did not execute on broken weapon");
        }
    }
}
