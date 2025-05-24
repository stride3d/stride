// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Weapons; // For BaseWeapon
using FirstPersonShooter.Player; // For PlayerEquipment
using FirstPersonShooter.Items;  // For IEquippable (though BaseWeapon implements it)

namespace FirstPersonShooter.Tests
{
    // --- Mock Weapon Implementations ---
    public class MockWeapon : BaseWeapon
    {
        public bool WasEquipped { get; private set; }
        public bool WasUnequipped { get; private set; }
        public Entity LastOwner { get; private set; }

        public string Name { get; set; } // For easy identification in logs

        public MockWeapon(string name = "MockWeapon")
        {
            Name = name;
        }

        public override void PrimaryAction()
        {
            // Log.Info($"{Name}: PrimaryAction called by {OwnerEntity?.Name}");
        }

        public override void OnEquip(Entity owner)
        {
            base.OnEquip(owner); // Sets OwnerEntity
            WasEquipped = true;
            LastOwner = owner;
            // Log.Info($"{Name}: OnEquip called by {owner?.Name}");
        }

        public override void OnUnequip(Entity owner)
        {
            base.OnUnequip(owner); // Clears OwnerEntity
            WasUnequipped = true;
            // Log.Info($"{Name}: OnUnequip called by {owner?.Name}");
        }

        public void ResetFlags()
        {
            WasEquipped = false;
            WasUnequipped = false;
            LastOwner = null;
        }
    }

    // --- PlayerEquipment Tests ---
    public class PlayerEquipmentTests : SyncScript
    {
        private int testsRun = 0;
        private int testsPassed = 0;

        public override void Start()
        {
            Log.Info("PlayerEquipmentTests: Starting tests...");

            TestEquipSingleWeapon();
            TestSwitchWeapon();

            Log.Info($"PlayerEquipmentTests: Finished. {testsPassed}/{testsRun} tests passed.");
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

        private void AssertEquals<T>(T expected, T actual, string testName, string message = "") where T : class
        {
            testsRun++;
            if (expected == actual)
            {
                testsPassed++;
                Log.Info($"[SUCCESS] {testName} {message}");
            }
            else
            {
                Log.Error($"[FAILURE] {testName} - Expected '{expected?.ToString() ?? "null"}', got '{actual?.ToString() ?? "null"}' {message}");
            }
        }
        
        private void AssertNotNull(object obj, string testName, string message = "")
        {
            AssertTrue(obj != null, testName, message);
        }


        private void TestEquipSingleWeapon()
        {
            var testName = "TestEquipSingleWeapon";
            Log.Info($"PlayerEquipmentTests: Running {testName}...");

            // Setup
            var playerEntity = new Entity("TestPlayer_SingleEquip");
            var equipment = new PlayerEquipment();
            playerEntity.Add(equipment);
            // Simulate entity being added to scene and Start being called (if PlayerEquipment had a Start method)
            
            var weaponEntityA = new Entity("TestWeaponA_SingleEquip");
            var mockWeaponA = new MockWeapon("WeaponA");
            weaponEntityA.Add(mockWeaponA);
            // Simulate Start for mockWeaponA if it had one.

            // Action
            equipment.EquipWeapon(mockWeaponA);

            // Assertions
            AssertEquals(mockWeaponA, equipment.CurrentWeapon, $"{testName} - CurrentWeapon is WeaponA");
            AssertTrue(mockWeaponA.WasEquipped, $"{testName} - WeaponA.WasEquipped is true");
            AssertFalse(mockWeaponA.WasUnequipped, $"{testName} - WeaponA.WasUnequipped is false");
            AssertEquals(playerEntity, mockWeaponA.OwnerEntity, $"{testName} - WeaponA.OwnerEntity is playerEntity");
            AssertEquals(playerEntity, mockWeaponA.LastOwner, $"{testName} - WeaponA.LastOwner is playerEntity (from OnEquip)");
        }

        private void TestSwitchWeapon()
        {
            var testName = "TestSwitchWeapon";
            Log.Info($"PlayerEquipmentTests: Running {testName}...");

            // Setup
            var playerEntity = new Entity("TestPlayer_Switch");
            var equipment = new PlayerEquipment();
            playerEntity.Add(equipment);

            var weaponEntityA = new Entity("TestWeaponA_Switch");
            var mockWeaponA = new MockWeapon("WeaponA_Switch");
            weaponEntityA.Add(mockWeaponA);
            
            var weaponEntityB = new Entity("TestWeaponB_Switch");
            var mockWeaponB = new MockWeapon("WeaponB_Switch");
            weaponEntityB.Add(mockWeaponB);

            // Action Phase 1: Equip Weapon A
            equipment.EquipWeapon(mockWeaponA);

            // Assertions for Phase 1 (sanity check)
            AssertTrue(mockWeaponA.WasEquipped, $"{testName} - Phase 1: WeaponA.WasEquipped");
            AssertEquals(playerEntity, mockWeaponA.OwnerEntity, $"{testName} - Phase 1: WeaponA.OwnerEntity");

            // Action Phase 2: Equip Weapon B (switching from A to B)
            equipment.EquipWeapon(mockWeaponB);

            // Assertions for Phase 2
            AssertTrue(mockWeaponA.WasUnequipped, $"{testName} - Phase 2: WeaponA.WasUnequipped is true after switching");
            AssertEquals(null, mockWeaponA.OwnerEntity, $"{testName} - Phase 2: WeaponA.OwnerEntity is null after unequip");

            AssertEquals(mockWeaponB, equipment.CurrentWeapon, $"{testName} - Phase 2: CurrentWeapon is WeaponB");
            AssertTrue(mockWeaponB.WasEquipped, $"{testName} - Phase 2: WeaponB.WasEquipped is true");
            AssertFalse(mockWeaponB.WasUnequipped, $"{testName} - Phase 2: WeaponB.WasUnequipped is false");
            AssertEquals(playerEntity, mockWeaponB.OwnerEntity, $"{testName} - Phase 2: WeaponB.OwnerEntity is playerEntity");
            AssertEquals(playerEntity, mockWeaponB.LastOwner, $"{testName} - Phase 2: WeaponB.LastOwner is playerEntity (from OnEquip)");
        }

        public override void Update()
        {
            // This script can be set to automatically remove itself after Start()
        }
    }
}
