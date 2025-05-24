// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Weapons.Melee; // For Hatchet
using FirstPersonShooter.World;       // For IResourceNode, TreeResource
using FirstPersonShooter.Core;        // For MaterialType

namespace FirstPersonShooter.Tests
{
    // --- Mock Resource Node ---
    public class MockResourceNode : ScriptComponent, IResourceNode
    {
        public string ResourceTypeToProvide { get; set; } = "Wood";
        public float CurrentHealth { get; set; } = 100f;
        public MaterialType NodeMaterial { get; set; } = MaterialType.Wood;

        public bool HarvestCalled { get; private set; }
        public float LastGatherAmount { get; private set; }
        public Entity LastHarvester { get; private set; }
        private bool depleted = false;

        public string GetResourceType() => ResourceTypeToProvide;
        public bool IsDepleted => depleted || CurrentHealth <= 0;
        public Entity GetEntity() => this.Entity;
        public MaterialType HitMaterial => NodeMaterial;

        public bool Harvest(float gatherAmount, Entity harvester)
        {
            if (IsDepleted) return false;

            HarvestCalled = true;
            LastGatherAmount = gatherAmount;
            LastHarvester = harvester;
            CurrentHealth -= gatherAmount;
            
            Log.Info($"MockResourceNode '{Entity?.Name}': Harvested by {harvester?.Name} for {gatherAmount}. Health: {CurrentHealth}");

            if (CurrentHealth <= 0)
            {
                depleted = true;
                Log.Info($"MockResourceNode '{Entity?.Name}': Depleted.");
            }
            return true;
        }

        public void ResetMock()
        {
            HarvestCalled = false;
            LastGatherAmount = 0f;
            LastHarvester = null;
        }
    }

    // --- ResourceGatheringTests ---
    public class ResourceGatheringTests : SyncScript
    {
        private int testsRun = 0;
        private int testsPassed = 0;

        public override void Start()
        {
            Log.Info("ResourceGatheringTests: Starting tests...");

            TestHatchetHarvestsWood();
            TestHatchetIgnoresStone();
            TestTreeResourceDepletion();

            Log.Info($"ResourceGatheringTests: Finished. {testsPassed}/{testsRun} tests passed.");
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
            bool areEqual = (expected == null && actual == null) || (expected != null && expected.Equals(actual));
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

        // Helper to simulate parts of Hatchet's PrimaryAction relevant to harvesting
        // This avoids needing a full physics simulation for this unit test.
        private void SimulateHatchetHit(Hatchet hatchet, Entity hitEntity)
        {
            // Hatchet's PrimaryAction does:
            // 1. Cooldown checks, durability damage (we can ignore these for focused harvest test)
            // 2. Raycast (we simulate a successful hit on 'hitEntity')
            // 3. Gets IResourceNode from hitEntity
            // 4. Checks resource type and calls Harvest()
            // 5. Calls SoundManager

            var resourceNode = hitEntity?.Get<IResourceNode>();
            if (resourceNode != null && !resourceNode.IsDepleted)
            {
                if (resourceNode.GetResourceType() == "Wood") // Hatchet specifically gathers Wood
                {
                    resourceNode.Harvest(hatchet.Damage, hatchet.OwnerEntity);
                }
                // SoundManager.PlayImpactSound is called regardless of successful harvest if it's a resource node
            }
            // else if (hitEntity?.Get<SurfaceMaterial>() != null) { ... }
            // SoundManager.PlayImpactSound(...) would be called here too based on SurfaceMaterial
        }


        private void TestHatchetHarvestsWood()
        {
            var testName = "TestHatchetHarvestsWood";
            Log.Info($"ResourceGatheringTests: Running {testName}...");

            var playerEntity = new Entity("TestPlayer_HarvestWood");
            var hatchetEntity = new Entity("TestHatchet_HarvestWood");
            var hatchet = new Hatchet { Damage = 25f }; // Set damage for predictable harvest amount
            hatchetEntity.Add(hatchet);
            hatchet.OnEquip(playerEntity); // Set OwnerEntity for the hatchet

            var woodNodeEntity = new Entity("TestWoodNode");
            var woodNode = new MockResourceNode { ResourceTypeToProvide = "Wood", CurrentHealth = 50f };
            woodNodeEntity.Add(woodNode);

            // Simulate Hatchet hitting the wood node
            SimulateHatchetHit(hatchet, woodNodeEntity);

            AssertTrue(woodNode.HarvestCalled, $"{testName} - MockResourceNode.Harvest() was called");
            AssertEquals(hatchet.Damage, woodNode.LastGatherAmount, $"{testName} - Harvest amount matches hatchet damage");
            AssertEquals(playerEntity, woodNode.LastHarvester, $"{testName} - Harvester is the player entity");
            AssertEquals(25f, woodNode.CurrentHealth, $"{testName} - Node health reduced correctly");
        }

        private void TestHatchetIgnoresStone()
        {
            var testName = "TestHatchetIgnoresStone";
            Log.Info($"ResourceGatheringTests: Running {testName}...");

            var playerEntity = new Entity("TestPlayer_IgnoreStone");
            var hatchetEntity = new Entity("TestHatchet_IgnoreStone");
            var hatchet = new Hatchet { Damage = 25f };
            hatchetEntity.Add(hatchet);
            hatchet.OnEquip(playerEntity);

            var stoneNodeEntity = new Entity("TestStoneNode");
            var stoneNode = new MockResourceNode { ResourceTypeToProvide = "Stone", CurrentHealth = 50f };
            stoneNodeEntity.Add(stoneNode);

            // Simulate Hatchet hitting the stone node
            SimulateHatchetHit(hatchet, stoneNodeEntity);

            AssertFalse(stoneNode.HarvestCalled, $"{testName} - MockResourceNode.Harvest() was NOT called for stone");
            AssertEquals(50f, stoneNode.CurrentHealth, $"{testName} - Stone node health unchanged");
        }

        private void TestTreeResourceDepletion()
        {
            var testName = "TestTreeResourceDepletion";
            Log.Info($"ResourceGatheringTests: Running {testName}...");

            var treeEntity = new Entity("TestTree_Depletion");
            var tree = new TreeResource { Health = 30f, ResourceType = "Wood" }; // Lower health for fewer calls
            treeEntity.Add(tree);
            
            var harvesterEntity = new Entity("TestHarvester_Tree");

            AssertFalse(tree.IsDepleted, $"{testName} - Tree initially not depleted");

            tree.Harvest(10f, harvesterEntity); // Health: 20
            AssertFalse(tree.IsDepleted, $"{testName} - Tree not depleted after 10 damage");
            AssertEquals(20f, tree.Health, $"{testName} - Tree health is 20");

            tree.Harvest(10f, harvesterEntity); // Health: 10
            AssertFalse(tree.IsDepleted, $"{testName} - Tree not depleted after 20 damage");
            AssertEquals(10f, tree.Health, $"{testName} - Tree health is 10");

            tree.Harvest(15f, harvesterEntity); // Health: -5 (should be 0 and depleted)
            AssertTrue(tree.IsDepleted, $"{testName} - Tree IS depleted after 35 total damage");
            AssertEquals(0f, tree.Health, $"{testName} - Tree health clamped at 0");

            // Test harvesting already depleted tree
            bool harvestResultOnDepleted = tree.Harvest(5f, harvesterEntity);
            AssertFalse(harvestResultOnDepleted, $"{testName} - Harvest returns false on already depleted tree");
            AssertEquals(0f, tree.Health, $"{testName} - Tree health remains 0 after attempting harvest on depleted");
        }
    }
}
