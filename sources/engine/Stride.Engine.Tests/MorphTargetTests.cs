// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Xunit;
using Stride.Animations;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Shadows;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Renders a skinned model with morph targets: the bind pose, morph weights alone, morphs with skinning, and a model
    /// scaled to zero. Then renders the same frame with each deformation path and checks that they match. Last, checks
    /// that a model outside every view is not deformed, is deformed in the first frame it is visible again, and is
    /// deformed when only a shadow view collects it.
    /// </summary>
    public class MorphTargetTests : EngineTestBase
    {
        private const int DonutCount = 5;
        private const float DonutSpacing = 3.0f;
        // Largest difference of a color channel between two deformation paths, and how many pixels may reach it.
        private const int AllowedChannelDifference = 8;
        private const double AllowedDifferentPixelRatio = 0.001;

        private static readonly MeshDeformationOverride VertexShaderPath = new(MeshDeformationMode.VertexShader);
        private static readonly MeshDeformationOverride ComputeSingleInstancePath = new(MeshDeformationMode.Compute, DeformationThreadGroup.X32Y1);
        private static readonly MeshDeformationOverride ComputeBatchedPath = new(MeshDeformationMode.Compute, DeformationThreadGroup.X8Y16);

        private readonly List<Entity> donuts = new();
        private readonly Dictionary<MeshDeformationOverride, Image> capturedFrames = new();
        private MeshDeformationOverride captureNextFrame;
        // Frames at which to count the models no view collected, and the counts.
        private readonly List<int> countFrames = new();
        private readonly Dictionary<int, int> uncollectedModelCounts = new();

        public MorphTargetTests()
        {
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            // The test package compiles effects for Level_9_3, which has no compute shaders.
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;

            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            EnableDirectionalShadows();

            var model = Content.Load<Model>("MorphDonut Model");
            var clip = Content.Load<AnimationClip>("MorphDonut Animation");
            for (int index = 0; index < DonutCount; index++)
            {
                var donut = new Entity { new ModelComponent { Model = model } };
                donut.Transform.Position = new Vector3((index - (DonutCount - 1) / 2.0f) * DonutSpacing, 0, 0);
                donut.GetOrCreate<AnimationComponent>().Animations.Add("Animation", clip);
                Scene.Entities.Add(donut);
                donuts.Add(donut);
            }

            var camera = new TestCamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor);
            CameraComponent = camera.Camera;
            Script.Add(camera);
            camera.Position = new Vector3(0, 8.0f, 18.0f);
            camera.SetTarget(donuts[DonutCount / 2], true);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            int frame = 0;

            // Bind pose: every weight is 0 and no animation plays.
            FrameGameSystem.Draw(frame, () => { }).TakeScreenshot(frame);

            // Morph weights alone, a different combination on each donut, including a negative weight.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                SetWeights(0, ("Bigger", 1.0f));
                SetWeights(1, ("Thicker", 1.0f));
                SetWeights(2, ("Wider", 0.5f), ("Flatter", 0.5f));
                SetWeights(3, ("Flatter", -0.5f));
                SetWeights(4, ("Bigger", 0.25f), ("Thicker", 0.5f), ("Wider", 0.5f));
            }).TakeScreenshot(frame);

            // Morphs and skinning: the clip animates the bones and the weights, paused at a different time on each donut.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                for (int index = 0; index < DonutCount; index++)
                {
                    var playing = donuts[index].Get<AnimationComponent>().Play("Animation");
                    playing.Enabled = false;
                    playing.CurrentTime = TimeSpan.FromSeconds(0.3 + 0.35 * index);
                }
            }).TakeScreenshot(frame);

            // The same frame on each deformation path: the vertex shader path has a gold image, the compute paths must match it.
            foreach (var path in new[] { VertexShaderPath, ComputeSingleInstancePath, ComputeBatchedPath })
            {
                frame += 2;
                FrameGameSystem.Draw(frame - 1, () => ModelRenderProcessor.DeformationOverride = path);
                FrameGameSystem.Draw(frame, () => captureNextFrame = path);
                if (path == VertexShaderPath)
                    FrameGameSystem.TakeScreenshot(frame);
            }

            // A donut scaled to 0 has no inverse world matrix: it is skipped, the others still render.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                ModelRenderProcessor.DeformationOverride = null;
                donuts[DonutCount / 2].Transform.Scale = Vector3.Zero;
            }).TakeScreenshot(frame);

            // A donut behind the camera (at z = 18, looking at the origin) is not collected by any view, so it is not
            // deformed. The other four, the middle one at its normal scale again, are.
            frame += 2;
            var firstDonutPosition = donuts[0].Transform.Position;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                donuts[DonutCount / 2].Transform.Scale = Vector3.One;
                donuts[0].Transform.Position = new Vector3(0, 0, 60.0f);
            });
            countFrames.Add(frame);

            // Back in view: the donut is deformed in the first frame it is visible again, not one frame later.
            frame += 1;
            FrameGameSystem.Update(frame, () => donuts[0].Transform.Position = firstDonutPosition).TakeScreenshot(frame);

            // Above the camera's view, under a light pointing down: only a shadow view collects the donut, which must
            // deform it, so its shadow on the ground is deformed too.
            frame += 2;
            FrameGameSystem.Draw(frame - 1, () =>
            {
                AddGroundAndShadowLight();
                donuts[0].Transform.Position = new Vector3(0, 12.0f, -6.0f);
            }).TakeScreenshot(frame);
            countFrames.Add(frame);
        }

        // The shared test compositor renders no shadows; the shadow step needs directional light shadow maps.
        private void EnableDirectionalShadows()
        {
            var compositor = SceneSystem.GraphicsCompositor;
            var shadowCasterStage = compositor.RenderStages.First(stage => stage.Name == "ShadowMapCaster");
            var meshFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            var lightingFeature = meshFeature.RenderFeatures.OfType<ForwardLightingRenderFeature>().First();
            lightingFeature.ShadowMapRenderer = new ShadowMapRenderer
            {
                Renderers = { new LightDirectionalShadowMapRenderer { ShadowCasterRenderStage = shadowCasterStage } },
            };
        }

        private void AddGroundAndShadowLight()
        {
            // A white ground, so the donuts' shadows stand out from the pink donuts.
            var material = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.DarkGray)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });
            GeometricPrimitive<VertexPositionNormalTexture> plane = GeometricPrimitive.Plane.New(GraphicsDevice, 60.0f, 60.0f, normalDirection: NormalDirection.UpY);
            var ground = new Entity { new ModelComponent { Model = new Model { new Mesh { Draw = plane.ToMeshDraw() }, new MaterialInstance(material) } } };
            ground.Transform.Position = new Vector3(0, -1.5f, 0);
            Scene.Entities.Add(ground);

            var light = new Entity { new LightComponent { Type = new LightDirectional { Shadow = { Enabled = true } } } };
            // A directional light points along its forward axis (-Z): turn it to point down (-Y).
            light.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);
            Scene.Entities.Add(light);
        }

        protected override void Draw(Stride.Games.GameTime gameTime)
        {
            base.Draw(gameTime);
            if (countFrames.Contains(FrameGameSystem.CurrentFrame))
            {
                var modelProcessor = SceneSystem.SceneInstance.GetProcessor<ModelRenderProcessor>();
                uncollectedModelCounts[FrameGameSystem.CurrentFrame] = modelProcessor.DeformationRenderer.UncollectedModelCount;
            }
            if (captureNextFrame == null)
                return;
            capturedFrames[captureNextFrame] = GraphicsDevice.Presenter.BackBuffer.GetDataAsImage(GraphicsContext.CommandList);
            captureNextFrame = null;
        }

        private void SetWeights(int donutIndex, params (string Name, float Weight)[] weights)
        {
            var model = donuts[donutIndex].Get<ModelComponent>();
            foreach (var (name, weight) in weights)
                model.SetMorphWeight(name, weight);
        }

        private void AssertDeformationPathsMatch()
        {
            var reference = capturedFrames[VertexShaderPath];
            foreach (var path in new[] { ComputeSingleInstancePath, ComputeBatchedPath })
                AssertImagesMatch(reference, capturedFrames[path], $"{path.Mode} {path.ThreadGroup}");
        }

        // The donut behind the camera is the only one not deformed; the donut seen only by a shadow view is deformed.
        private void AssertOnlyHiddenDonutUncollected()
        {
            var counts = new List<int>();
            foreach (var countFrame in countFrames)
                counts.Add(uncollectedModelCounts.GetValueOrDefault(countFrame, -1));
            Assert.Equal(new[] { 1, 0 }, counts);
        }

        private static unsafe void AssertImagesMatch(Image expected, Image actual, string pathName)
        {
            var expectedPixels = expected.PixelBuffer[0];
            var actualPixels = actual.PixelBuffer[0];
            Assert.Equal(expectedPixels.Width, actualPixels.Width);
            Assert.Equal(expectedPixels.Height, actualPixels.Height);
            Assert.Equal(expectedPixels.Format, actualPixels.Format);

            int differentPixelCount = 0;
            int largestDifference = 0;
            for (int row = 0; row < expectedPixels.Height; row++)
            {
                var expectedRow = (Color*)(expectedPixels.DataPointer + row * expectedPixels.RowStride);
                var actualRow = (Color*)(actualPixels.DataPointer + row * actualPixels.RowStride);
                for (int column = 0; column < expectedPixels.Width; column++)
                {
                    var expectedColor = expectedRow[column];
                    var actualColor = actualRow[column];
                    int difference = Math.Max(Math.Max(Math.Abs(expectedColor.R - actualColor.R), Math.Abs(expectedColor.G - actualColor.G)), Math.Abs(expectedColor.B - actualColor.B));
                    if (difference > 2)
                        differentPixelCount++;
                    largestDifference = Math.Max(largestDifference, difference);
                }
            }

            double differentPixelRatio = (double)differentPixelCount / (expectedPixels.Width * expectedPixels.Height);
            Assert.True(largestDifference <= AllowedChannelDifference || differentPixelRatio <= AllowedDifferentPixelRatio,
                $"{pathName} differs from the vertex shader path: {differentPixelCount} pixels differ, by up to {largestDifference}.");
        }

        [Fact]
        public void RunTestGame()
        {
            var game = new MorphTargetTests();
            try
            {
                // Compare the deformation paths even when a gold image is missing or differs: their result doesn't depend on it.
                ExceptionDispatchInfo goldFailure = null;
                try
                {
                    RunGameTest(game);
                }
                catch (Exception exception) when (game.capturedFrames.Count == 3)
                {
                    goldFailure = ExceptionDispatchInfo.Capture(exception);
                }
                game.AssertDeformationPathsMatch();
                game.AssertOnlyHiddenDonutUncollected();
                goldFailure?.Throw();
            }
            finally
            {
                ModelRenderProcessor.DeformationOverride = null;
                foreach (var image in game.capturedFrames.Values)
                    image.Dispose();
            }
        }
    }
}
