// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine.Processors;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// A scene instance with two visibility groups, the compositor's and one added while the game runs, holds a model
    /// render processor and an instancing processor per group; each group's instancing map is keyed by the render
    /// models of its own group's model render processor, whichever of the two processors a group makes first and wherever
    /// its model render processor lands among the processors of the same order; and each group's render meshes take the
    /// mesh box widened by the instances once, not once per instancing processor.
    /// </summary>
    public class TestInstancingProcessor : GameTestBase
    {
        private readonly bool instancingFirst;
        private readonly int idleProcessors;
        private ModelComponent modelComponent;
        private Entity entity;

        public TestInstancingProcessor() : this(false, 0)
        {
        }

        private TestInstancingProcessor(bool instancingFirst, int idleProcessors)
        {
            this.instancingFirst = instancingFirst;
            this.idleProcessors = idleProcessors;
            GraphicsDeviceManager.PreferredBackBufferWidth = 64;
            GraphicsDeviceManager.PreferredBackBufferHeight = 64;
            // Instancing buffers are structured buffers
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var camera = new CameraComponent();
            var compositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: false, camera: camera);

            var scene = new Scene();
            var cameraEntity = new Entity { camera };
            cameraEntity.Transform.Position = new Vector3(0, 0, 3);
            scene.Entities.Add(cameraEntity);

            var model = new CubeProceduralModel().Generate(Services);
            model.Materials.Add(Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes = { Emissive = new MaterialEmissiveMapFeature(new ComputeColor(Color.White)) },
            }));
            modelComponent = new ModelComponent { Model = model };

            // Instances off the entity's origin, so a box widened twice is centered elsewhere than one widened once
            var instancingArray = new InstancingUserArray { ModelTransformUsage = ModelTransformUsage.PostMultiply };
            instancingArray.UpdateWorldMatrices([Matrix.Translation(1, 0, 0), Matrix.Translation(3, 0, 0)]);
            var instancing = new InstancingComponent { Type = instancingArray };

            // A scene instance makes a group's processors in the order it met their component types, so the component order decides which comes first
            entity = instancingFirst ? new Entity { instancing, modelComponent } : new Entity { modelComponent, instancing };
            entity.Transform.Position = new Vector3(0, 1, 0);
            scene.Entities.Add(entity);

            SceneSystem.SceneInstance = new SceneInstance(Services, scene);

            // Before the compositor's visibility group exists, so its model render processor lands among them
            for (int i = 0; i < idleProcessors; i++)
                SceneSystem.SceneInstance.Processors.Add(new IdleProcessor());
            SceneSystem.GraphicsCompositor = compositor;
        }

        /// <summary> For each visibility group, whether its instancing map holds exactly the render model its own group made. </summary>
        private List<string> CheckGroups()
        {
            var sceneInstance = SceneSystem.SceneInstance;
            var failures = new List<string>();
            foreach (var group in sceneInstance.VisibilityGroups)
            {
                var modelProcessors = sceneInstance.Processors.OfType<ModelRenderProcessor>().Where(processor => processor.VisibilityGroup == group).ToList();
                if (modelProcessors.Count != 1)
                {
                    failures.Add($"group {sceneInstance.VisibilityGroups.IndexOf(group)} has {modelProcessors.Count} model render processors");
                    continue;
                }

                var ownRenderModel = modelProcessors[0].RenderModels[modelComponent];
                var map = group.Tags.Get(InstancingRenderFeature.ModelToInstancingMap);
                if (map == null || map.Count != 1 || !map.ContainsKey(ownRenderModel))
                    failures.Add($"group {sceneInstance.VisibilityGroups.IndexOf(group)} maps {map?.Count ?? 0} render models, its own one {(map?.ContainsKey(ownRenderModel) == true ? "among them" : "not among them")}");

                // The unit cube at (1, 1, 0) and (3, 1, 0): widened once, the render mesh's box spans them; widened once more, it is centered at x = 4
                var expected = new BoundingBoxExt(new BoundingBox(new Vector3(0.5f, 0.5f, -0.5f), new Vector3(3.5f, 1.5f, 0.5f)));
                var boundingBox = ownRenderModel.Meshes[0].BoundingBox;
                if (Vector3.Distance(boundingBox.Center, expected.Center) > 1e-3f || Vector3.Distance(boundingBox.Extent, expected.Extent) > 1e-3f)
                    failures.Add($"group {sceneInstance.VisibilityGroups.IndexOf(group)} render mesh box: center {boundingBox.Center}, extent {boundingBox.Extent}; expected center {expected.Center}, extent {expected.Extent}");
            }
            return failures;
        }

        /// <summary> A processor of order 0 with no component to process, which moves where the next processor of order 0 lands. </summary>
        private class IdleProcessor : EntityProcessor<IdleComponent>
        {
        }

        private class IdleComponent : EntityComponent
        {
        }

        /// <summary>
        /// A model component replaced in place (the entity's collection set at its index, as a live assembly reload does) gets a
        /// render model of its own from the model render processor; the instancing map follows it.
        /// </summary>
        [Fact]
        public void AModelComponentReplacedInPlaceIsMappedAgain()
        {
            var game = new TestInstancingProcessor(false, 0);
            List<string> failures = null;
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;
                for (int i = 0; i < 3; i++)
                    await game.Script.NextFrame();

                var replacement = new ModelComponent { Model = game.modelComponent.Model };
                game.entity.Components[game.entity.Components.IndexOf(game.modelComponent)] = replacement;
                game.modelComponent = replacement;

                for (int i = 0; i < 3; i++)
                    await game.Script.NextFrame();

                failures = game.CheckGroups();
                game.Exit();
            });
            RunGameTest(game);

            Assert.Empty(failures);
        }

        /// <summary>
        /// A model set to null draws nothing and throws nothing: the render model keeps its meshes until a model is set again,
        /// and the processor widens no box meanwhile. Set again, the model draws its instances.
        /// </summary>
        [Fact]
        public void AModelSetToNullDrawsNothingAndItsInstancesAgainOnceSet()
        {
            var game = new TestInstancingProcessor(false, 0);
            List<string> failures = null;
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;
                for (int i = 0; i < 3; i++)
                    await game.Script.NextFrame();

                var model = game.modelComponent.Model;
                game.modelComponent.Model = null;
                for (int i = 0; i < 3; i++)
                    await game.Script.NextFrame();

                game.modelComponent.Model = model;
                for (int i = 0; i < 3; i++)
                    await game.Script.NextFrame();

                failures = game.CheckGroups();
                game.Exit();
            });
            RunGameTest(game);

            Assert.Empty(failures);
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(false, 2)]
        [InlineData(false, 3)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        public void EachGroupMapsTheRenderModelsOfItsOwnGroup(bool instancingFirst, int idleProcessors)
        {
            var game = new TestInstancingProcessor(instancingFirst, idleProcessors);
            List<string> failures = null;
            int groupCount = 0;
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                for (int i = 0; i < 3; i++)
                    await game.Script.NextFrame();

                var sceneInstance = game.SceneSystem.SceneInstance;

                // The way a second compositor adds a visibility group of its own while the scene runs
                sceneInstance.VisibilityGroups.Add(new VisibilityGroup(game.SceneSystem.GraphicsCompositor.RenderSystem));

                for (int i = 0; i < 3; i++)
                    await game.Script.NextFrame();

                groupCount = sceneInstance.VisibilityGroups.Count;
                failures = game.CheckGroups();
                game.Exit();
            });
            RunGameTest(game);

            Assert.Equal(2, groupCount);
            Assert.Empty(failures);
        }
    }
}
