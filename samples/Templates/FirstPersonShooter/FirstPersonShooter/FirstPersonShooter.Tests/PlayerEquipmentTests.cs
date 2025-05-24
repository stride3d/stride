// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Weapons; // For BaseWeapon
using FirstPersonShooter.Player; // For PlayerEquipment

// Namespace for tests
namespace FirstPersonShooter.Tests
{
    // --- Mock Weapon Implementations ---
    // (Defined as a nested class or separate file, here nested for simplicity for the tool)
    public class MockTestWeapon : BaseWeapon 
    {
        public bool IsEquipped { get; private set; }
        public bool IsUnequipped { get; private set; }
        public Entity LastOwnerEntity { get; private set; }
        public string WeaponName { get; }

        public MockTestWeapon(string name)
        {
            WeaponName = name;
        }

        public override void PrimaryAction() 
        { 
            // Log.Info($"{WeaponName} - PrimaryAction called by {OwnerEntity?.Name}");
        }

        public override void OnEquip(Entity owner)
        {
            base.OnEquip(owner); // Important: This sets OwnerEntity in BaseWeapon
            IsEquipped = true;
            IsUnequipped = false; // Reset in case it was equipped again
            LastOwnerEntity = owner;
            // Log.Info($"{WeaponName} - OnEquip by {owner?.Name}");
        }

        public override void OnUnequip(Entity owner)
        {
            base.OnUnequip(owner); // Important: This clears OwnerEntity in BaseWeapon
            IsUnequipped = true;
            IsEquipped = false; // Should not be considered equipped after this
            // Log.Info($"{WeaponName} - OnUnequip by {owner?.Name}");
        }

        public void ResetMockState()
        {
            IsEquipped = false;
            IsUnequipped = false;
            LastOwnerEntity = null;
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
            TestEquipNullWeapon(); // Additional test for robustness

            Log.Info($"PlayerEquipmentTests: Finished. {testsPassed}/{testsRun} tests passed.");
            // Entity.Scene = null; // Optional: remove test entity from scene
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

        private void AssertEquals<T>(T expected, T actual, string testName, string message = "")
        {
            testsRun++;
            bool areEqual;
            if (expected == null)
                areEqual = actual == null;
            else
                areEqual = expected.Equals(actual);

            if (areEqual)
            {
                testsPassed++;
                Log.Info($"[SUCCESS] {testName} {message}");
            }
            else
            {
                Log.Error($"[FAILURE] {testName} - Expected '{expected?.ToString() ?? "null"}', got '{actual?.ToString() ?? "null"}' {message}");
            }
        }
        
        private void AssertNull(object obj, string testName, string message = "")
        {
            AssertTrue(obj == null, testName, message);
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
            var playerEntity = new Entity("PE_TestPlayer_Single");
            var equipment = new PlayerEquipment();
            playerEntity.Add(equipment);
            // equipment.Start(); // If PlayerEquipment had a Start method with setup.
            
            var weaponEntityA = new Entity("PE_WeaponA_Single");
            var mockWeaponA = new MockTestWeapon("WeaponA");
            weaponEntityA.Add(mockWeaponA);
            // mockWeaponA.Start(); // If MockTestWeapon had a Start method.

            // Action
            equipment.EquipWeapon(mockWeaponA);

            // Assertions
            AssertEquals(mockWeaponA, equipment.CurrentWeapon, $"{testName} - CurrentWeapon is WeaponA");
            AssertTrue(mockWeaponA.IsEquipped, $"{testName} - WeaponA.IsEquipped is true");
            AssertFalse(mockWeaponA.IsUnequipped, $"{testName} - WeaponA.IsUnequipped is false");
            AssertEquals(playerEntity, mockWeaponA.OwnerEntity, $"{testName} - WeaponA.OwnerEntity is playerEntity");
            AssertEquals(playerEntity, mockWeaponA.LastOwnerEntity, $"{testName} - WeaponA.LastOwnerEntity is playerEntity");
        }

        private void TestSwitchWeapon()
        {
            var testName = "TestSwitchWeapon";
            Log.Info($"PlayerEquipmentTests: Running {testName}...");

            // Setup
            var playerEntity = new Entity("PE_TestPlayer_Switch");
            var equipment = new PlayerEquipment();
            playerEntity.Add(equipment);

            var weaponEntityA = new Entity("PE_WeaponA_Switch");
            var mockWeaponA = new MockTestWeapon("WeaponA_S");
            weaponEntityA.Add(mockWeaponA);
            
            var weaponEntityB = new Entity("PE_WeaponB_Switch");
            var mockWeaponB = new MockTestWeapon("WeaponB_S");
            weaponEntityB.Add(mockWeaponB);

            // Action Phase 1: Equip Weapon A
            equipment.EquipWeapon(mockWeaponA);
            AssertEquals(mockWeaponA, equipment.CurrentWeapon, $"{testName} - Sanity: WeaponA equipped");
            AssertTrue(mockWeaponA.IsEquipped, $"{testName} - Sanity: WeaponA.IsEquipped");
            AssertEquals(playerEntity, mockWeaponA.OwnerEntity, $"{testName} - Sanity: WeaponA owned by player");

            // Action Phase 2: Equip Weapon B (switching from A to B)
            equipment.EquipWeapon(mockWeaponB);

            // Assertions for Phase 2
            AssertTrue(mockWeaponA.IsUnequipped, $"{testName} - WeaponA.IsUnequipped is true after switching");
            AssertFalse(mockWeaponA.IsEquipped, $"{testName} - WeaponA.IsEquipped is false after switching / unequip");
            AssertNull(mockWeaponA.OwnerEntity, $"{testName} - WeaponA.OwnerEntity is null after unequip");

            AssertEquals(mockWeaponB, equipment.CurrentWeapon, $"{testName} - CurrentWeapon is WeaponB");
            AssertTrue(mockWeaponB.IsEquipped, $"{testName} - WeaponB.IsEquipped is true");
            AssertFalse(mockWeaponB.IsUnequipped, $"{testName} - WeaponB.IsUnequipped is false");
            AssertEquals(playerEntity, mockWeaponB.OwnerEntity, $"{testName} - WeaponB.OwnerEntity is playerEntity");
            AssertEquals(playerEntity, mockWeaponB.LastOwnerEntity, $"{testName} - WeaponB.LastOwnerEntity is playerEntity");
        }

        private void TestEquipNullWeapon()
        {
            var testName = "TestEquipNullWeapon";
            Log.Info($"PlayerEquipmentTests: Running {testName}...");

            // Setup
            var playerEntity = new Entity("PE_TestPlayer_Null");
            var equipment = new PlayerEquipment();
            playerEntity.Add(equipment);

            var weaponEntityA = new Entity("PE_WeaponA_Null");
            var mockWeaponA = new MockTestWeapon("WeaponA_N");
            weaponEntityA.Add(mockWeaponA);

            // Action Phase 1: Equip Weapon A
            equipment.EquipWeapon(mockWeaponA);
            AssertEquals(mockWeaponA, equipment.CurrentWeapon, $"{testName} - Sanity: WeaponA equipped");

            // Action Phase 2: Equip null (unequip current weapon)
            equipment.EquipWeapon(null);

            // Assertions for Phase 2
            AssertNull(equipment.CurrentWeapon, $"{testName} - CurrentWeapon is null after equipping null");
            AssertTrue(mockWeaponA.IsUnequipped, $"{testName} - WeaponA.IsUnequipped is true after equipping null");
            AssertFalse(mockWeaponA.IsEquipped, $"{testName} - WeaponA.IsEquipped is false after equipping null / unequip");
            AssertNull(mockWeaponA.OwnerEntity, $"{testName} - WeaponA.OwnerEntity is null after unequip");
        }
    }
}
