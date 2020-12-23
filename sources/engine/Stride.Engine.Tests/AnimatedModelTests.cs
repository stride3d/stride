// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Lights;
using Stride.Rendering.ProceduralModels;
using Stride.Rendering.Tessellation;

namespace Stride.Engine.Tests
{
    public class AnimatedModelTests : EngineTestBase
    {
        private Entity knight;
        private AnimationClip megalodonClip;
        private AnimationClip knightOptimizedClip;
        private TestCamera camera;

        public AnimatedModelTests()
        {
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_9_3 };

            // Use a fixed time step
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var knightModel = Content.Load<Model>("knight Model");
            knight = new Entity { new ModelComponent { Model = knightModel } };
            knight.Transform.Position = new Vector3(0, 0f, 0f);
            var animationComponent = knight.GetOrCreate<AnimationComponent>();
            animationComponent.Animations.Add("Run", Content.Load<AnimationClip>("knight Run"));
            animationComponent.Animations.Add("Idle", Content.Load<AnimationClip>("knight Idle"));

            // We will test both non-optimized and optimized clips
            megalodonClip = CreateModelChangeAnimation(new ProceduralModelDescriptor(new CubeProceduralModel { Size = Vector3.One, MaterialInstance = { Material = knightModel.Materials[0].Material } }).GenerateModel(Services));
            knightOptimizedClip = CreateModelChangeAnimation(Content.Load<Model>("knight Model"));
            knightOptimizedClip.Optimize();

            animationComponent.Animations.Add("ChangeModel1", megalodonClip);
            animationComponent.Animations.Add("ChangeModel2", knightOptimizedClip);

            Scene.Entities.Add(knight);

            camera = new TestCamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor);
            CameraComponent = camera.Camera;
            Script.Add(camera);

            camera.Position = new Vector3(6.0f, 2.5f, 1.5f);
            camera.SetTarget(knight, true);
        }

        private AnimationClip CreateModelChangeAnimation(Model model)
        {
            var changeMegalodonAnimClip = new AnimationClip();
            var modelCurve = new AnimationCurve<object>();
            modelCurve.KeyFrames.Add(new KeyFrameData<object>(CompressedTimeSpan.Zero, model));
            changeMegalodonAnimClip.AddCurve("[ModelComponent.Key].Model", modelCurve);

            return changeMegalodonAnimClip;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // Initial frame (no anim
            FrameGameSystem.Draw(() => { }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // T = 0
                var playingAnimation = knight.Get<AnimationComponent>().Play("Run");
                playingAnimation.Enabled = false;
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // T = 0.5sec
                var playingAnimation = knight.Get<AnimationComponent>().PlayingAnimations.First();
                playingAnimation.CurrentTime = TimeSpan.FromSeconds(0.5f);
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // Blend with Idle (both weighted 1.0f)
                var playingAnimation = knight.Get<AnimationComponent>().Blend("Idle", 1.0f, TimeSpan.Zero);
                playingAnimation.Enabled = false;
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // Update the model itself
                knight.Get<AnimationComponent>().Play("ChangeModel1");
            }).TakeScreenshot();

            FrameGameSystem.Draw(() =>
            {
                // Update the model itself (blend it at 2 vs 1 to force it to be active directly)
                var playingAnimation = knight.Get<AnimationComponent>().Blend("ChangeModel2", 2.0f, TimeSpan.Zero);
                playingAnimation.Enabled = false;
            }).TakeScreenshot();
        }

        [Fact]
        public void RunTestGame()
        {
            RunGameTest(new AnimatedModelTests());
        }
    }
}
