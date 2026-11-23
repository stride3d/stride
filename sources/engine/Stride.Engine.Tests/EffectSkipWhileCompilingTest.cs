// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.ProceduralModels;
using Stride.Shaders;
using Stride.Shaders.Compiler;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// With <see cref="RootEffectRenderFeature.SkipWhileCompiling"/>, an object whose effect is still compiling and that has no fallback
    /// is skipped and the frame goes on. Without it, the frame waits for the compilation.
    /// </summary>
    public class EffectSkipWhileCompilingTest : EngineTestBase
    {
        private enum Scenario
        {
            /// <summary>The effect compiles while the object is skipped, then the object is drawn.</summary>
            SkipThenDraw,
            /// <summary>The compilation fails while the object is skipped.</summary>
            SkipThenFail,
            /// <summary>The feature does not skip: the frame waits for the compilation.</summary>
            Wait,
        }

        [SkippableFact]
        public void ObjectIsSkippedWhileItsEffectCompilesThenDrawn()
        {
            SkipTestForPlatform(PlatformType.iOS);
            RunGameTest(new SkipGame(Scenario.SkipThenDraw));
        }

        [SkippableFact]
        public void FailedCompilationOfASkippedObjectEndsInTheErrorState()
        {
            SkipTestForPlatform(PlatformType.iOS);
            RunGameTest(new SkipGame(Scenario.SkipThenFail));
        }

        [SkippableFact]
        public void FrameWaitsForTheCompilationWithoutSkipWhileCompiling()
        {
            SkipTestForPlatform(PlatformType.iOS);
            RunGameTest(new SkipGame(Scenario.Wait));
        }

        private class SkipGame : EngineTestBase
        {
            private static readonly TimeSpan BlockedTimeout = TimeSpan.FromSeconds(30);
            private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(3);

            private readonly Scenario scenario;
            private GatedEffectCompiler gate;
            private MeshRenderFeature meshRenderFeature;
            private Material tessellatedMaterial;
            private Entity entity;
            private Timer watchdog;
            private volatile bool watchdogFired;
            private int lastFrame;

            public SkipGame(Scenario scenario)
            {
                this.scenario = scenario;
                GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
                GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
                GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
            }

            protected override async Task LoadContent()
            {
                await base.LoadContent();

                var plainMaterial = Content.Load<Material>("NoTessellation");
                tessellatedMaterial = Content.Load<Material>("PNTessellationAE");

                gate = new GatedEffectCompiler((EffectCompilerBase)EffectSystem.Compiler);
                EffectSystem.Compiler = gate;

                // No fallback effect: the feature either skips the object or waits
                RenderContext.GetShared(Services).RendererInitialized += renderer =>
                {
                    if (renderer is MeshRenderFeature feature)
                    {
                        meshRenderFeature = feature;
                        feature.SkipWhileCompiling = scenario != Scenario.Wait;
                    }
                };

                var sphere = new SphereProceduralModel { Radius = 50, Tessellation = 5, MaterialInstance = { Material = plainMaterial } };
                entity = new Entity("Sphere") { new ModelComponent(new ProceduralModelDescriptor(sphere).GenerateModel(Services)) };
                Scene.Entities.Add(entity);

                var camera = new TestCamera(SceneSystem.GraphicsCompositor);
                CameraComponent = camera.Camera;
                Script.Add(camera);
                camera.Position = new Vector3(50, 90, 160);
                camera.SetTarget(entity, true);
            }

            protected override void RegisterTests()
            {
                base.RegisterTests();

                // Plain material, real effect
                Frame(() => { });

                // The tessellated effect is held back. If the frame blocks on it, the watchdog lets it through so the test fails instead of hanging
                Frame(() =>
                {
                    gate.Hold = true;
                    watchdog = new Timer(_ =>
                    {
                        watchdogFired = true;
                        gate.Release();
                    }, null, scenario == Scenario.Wait ? WaitTimeout : BlockedTimeout, Timeout.InfiniteTimeSpan);
                    entity.Get<ModelComponent>().Materials[0] = tessellatedMaterial;
                });

                // The material change reaches the render features in the following frames
                for (int i = 0; i < 4; i++)
                {
                    Frame(() =>
                    {
                        if (scenario == Scenario.Wait)
                            return;

                        Assert.False(watchdogFired, "The frame blocked on the compilation of the effect");
                    });
                }

                if (scenario == Scenario.Wait)
                {
                    Frame(() =>
                    {
                        Assert.True(gate.HeldCount > 0, "The tessellated effect was not held back");
                        Assert.True(watchdogFired, "The frame did not wait for the compilation of the effect");
                    });
                    return;
                }

                Frame(() =>
                {
                    Assert.True(gate.HeldCount > 0, "The tessellated effect was not held back");
                    Assert.False(watchdogFired, "The frame blocked on the compilation of the effect");
                    var renderEffects = GetRenderEffects().ToList();
                    Assert.NotEmpty(renderEffects);
                    foreach (var renderEffect in renderEffects)
                    {
                        Assert.Equal(RenderEffectState.Compiling, renderEffect.State);
                        Assert.Null(renderEffect.Effect);
                    }

                    if (scenario == Scenario.SkipThenDraw)
                        gate.Release();
                    else
                        gate.Fail();

                    foreach (var renderEffect in renderEffects)
                        Task.WaitAny(new[] { renderEffect.PendingEffect }, BlockedTimeout);
                });

                // The finished compilation is picked up by the next frames
                for (int i = 0; i < 2; i++)
                    Frame(() => { });

                Frame(() =>
                {
                    watchdog.Dispose();
                    Assert.False(watchdogFired, "The frame blocked on the compilation of the effect");
                    var renderEffects = GetRenderEffects().ToList();
                    Assert.NotEmpty(renderEffects);
                    foreach (var renderEffect in renderEffects)
                    {
                        if (scenario == Scenario.SkipThenDraw)
                        {
                            Assert.Equal(RenderEffectState.Normal, renderEffect.State);
                            Assert.NotNull(renderEffect.Effect);
                        }
                        else
                        {
                            Assert.Equal(RenderEffectState.Error, renderEffect.State);
                            Assert.Null(renderEffect.Effect);
                        }
                    }
                });

                // The frames after the failure keep going with the object skipped
                Frame(() => { });
            }

            // Draw(Action) does not advance the frame by itself, so each step takes the next frame explicitly
            private void Frame(Action action) => FrameGameSystem.Draw(++lastFrame, action);

            private IEnumerable<RenderEffect> GetRenderEffects()
            {
                var renderEffects = meshRenderFeature.RenderData.GetData(meshRenderFeature.RenderEffectKey);
                var slotCount = meshRenderFeature.EffectPermutationSlotCount;
                foreach (var renderObject in meshRenderFeature.RenderObjects)
                {
                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        var renderEffect = renderEffects[renderObject.StaticObjectNode * slotCount + slot];
                        if (renderEffect != null)
                            yield return renderEffect;
                    }
                }
            }
        }
    }
}
