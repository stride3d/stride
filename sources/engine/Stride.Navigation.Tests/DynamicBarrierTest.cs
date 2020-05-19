// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Core.MicroThreading;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics.Regression;
using Stride.Physics;
using Stride.Rendering.Compositing;

namespace Stride.Navigation.Tests
{
    public class DynamicBarrierTest : GameTestBase
    {
        private Entity entityA;
        private Entity entityB;
        private PlayerController controllerA;
        private PlayerController controllerB;

        private Entity filterB;
        private Entity filterAB;

        private Vector3 targetPosition = new Vector3(1.4f, 0.0f, 0.0f);

        private DynamicNavigationMeshSystem dynamicNavigation;

        public DynamicBarrierTest()
        {
            AutoLoadDefaultSettings = true;
            IsDrawDesynchronized = false;
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            entityA = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Name == "A");
            entityB = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Name == "B");

            entityA.Add(controllerA = new PlayerController());
            entityB.Add(controllerB = new PlayerController());

            filterAB = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Name == "FilterAB");
            filterB = SceneSystem.SceneInstance.RootScene.Entities.FirstOrDefault(x => x.Name == "FilterB");

            dynamicNavigation = (DynamicNavigationMeshSystem)GameSystems.FirstOrDefault(x => x is DynamicNavigationMeshSystem);
            if (dynamicNavigation == null)
                throw new Exception("Failed to find dynamic navigation mesh system");

            dynamicNavigation.AutomaticRebuild = false;
            dynamicNavigation.Enabled = true;

            Script.AddTask(RunAsyncTests);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (gameTime.Total > TimeSpan.FromSeconds(6))
            {
                Assert.True(false, "Test timed out");
            }
        }

        private async Task RunAsyncTests()
        {
            // Wait for start method to be called
            while(controllerA.Character == null)
                await Script.NextFrame();

            // Wait for controllers to be on the ground
            while (!controllerA.Character.IsGrounded || !controllerB.Character.IsGrounded)
                await Script.NextFrame();

            controllerA.UpdateSpawnPosition();
            controllerB.UpdateSpawnPosition();

            // Enabled a wall that blocks A and B
            RecursiveToggle(filterAB, true);
            RecursiveToggle(filterB, false);
            var buildResult = await dynamicNavigation.Rebuild();
            Assert.True(buildResult.Success);
            Assert.Equal(2, buildResult.UpdatedLayers.Count);

            await Task.WhenAll(controllerA.TryMove(targetPosition).ContinueWith(x => { Assert.False(x.Result.Success); }),
                controllerB.TryMove(targetPosition).ContinueWith(x => { Assert.False(x.Result.Success); }));

            await Reset();

            // Enabled a wall that only blocks B
            RecursiveToggle(filterAB, false);
            RecursiveToggle(filterB, true);
            buildResult = await dynamicNavigation.Rebuild();
            Assert.True(buildResult.Success);

            await Task.WhenAll(controllerA.TryMove(targetPosition).ContinueWith(x => { Assert.True(x.Result.Success); }),
                controllerB.TryMove(targetPosition).ContinueWith(x => { Assert.False(x.Result.Success); }));

            await Reset();

            // Disable both walls
            RecursiveToggle(filterAB, false);
            RecursiveToggle(filterB, false);
            buildResult = await dynamicNavigation.Rebuild();
            Assert.True(buildResult.Success);

            await Task.WhenAll(controllerA.TryMove(targetPosition).ContinueWith(x => { Assert.True(x.Result.Success); }),
                controllerB.TryMove(targetPosition).ContinueWith(x => { Assert.True(x.Result.Success); }));

            // Walk back to spawn with only letting A pass
            RecursiveToggle(filterAB, false);
            RecursiveToggle(filterB, true);
            buildResult = await dynamicNavigation.Rebuild();
            Assert.True(buildResult.Success);

            await Task.WhenAll(controllerA.TryMove(controllerA.SpawnPosition).ContinueWith(x => { Assert.True(x.Result.Success); }),
                controllerB.TryMove(controllerB.SpawnPosition).ContinueWith(x => { Assert.False(x.Result.Success); }));

            Exit();
        }

        private async Task Reset()
        {
            controllerA.Reset();
            controllerB.Reset();
            await Script.NextFrame();
        }

        private void RecursiveToggle(Entity entity, bool enabled)
        {
            var model = entity.Get<ModelComponent>();
            if (model != null)
                model.Enabled = enabled;
            var collider = entity.Get<StaticColliderComponent>();
            if (collider != null)
                collider.Enabled = enabled;

            foreach (var c in entity.GetChildren())
                RecursiveToggle(c, enabled);
        }

        [Fact]
        public static void DynamicBarrierTest1()
        {
            RunGameTest(new DynamicBarrierTest());
        }
    }
}
