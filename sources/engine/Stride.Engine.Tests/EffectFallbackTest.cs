// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
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
    /// A mesh whose effect is still compiling draws with the fallback effect, the way the editor does.
    /// The fallback has no hull shader, so it must draw the original mesh and not the tessellated patch list
    /// the material switched to, or the draw is invalid and the GPU can hang.
    /// </summary>
    public class EffectFallbackTest : EngineTestBase
    {
        private Material plainMaterial;
        private Material tessellatedMaterial;
        private Entity entity;
        private GatedEffectCompiler gate;
        private MeshRenderFeature meshRenderFeature;
        private int fallbackCount;

        public EffectFallbackTest()
        {
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            plainMaterial = Content.Load<Material>("NoTessellation");
            // Adjacent-edge tessellation also swaps the index buffer, the strongest case for the fallback draw
            tessellatedMaterial = Content.Load<Material>("PNTessellationAE");

            gate = new GatedEffectCompiler((EffectCompilerBase)EffectSystem.Compiler);
            EffectSystem.Compiler = gate;

            var fallbackEffects = new MeshFallbackEffects(GraphicsDevice, EffectSystem);
            RenderContext.GetShared(Services).RendererInitialized += renderer =>
            {
                if (renderer is MeshRenderFeature feature)
                {
                    meshRenderFeature = feature;
                    feature.ComputeFallbackEffect = (renderObject, renderEffect, state) =>
                    {
                        Interlocked.Increment(ref fallbackCount);
                        return fallbackEffects.ComputeFallbackEffect(renderObject, renderEffect, state);
                    };
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
            FrameGameSystem.TakeScreenshot();

            // The tessellated effect is held back, so this frame draws the fallback: the plain sphere with a white light
            FrameGameSystem.Draw(() =>
            {
                gate.Hold = true;
                entity.Get<ModelComponent>().Materials[0] = tessellatedMaterial;
            }).TakeScreenshot();

            // Let the tessellated effect through, so this frame draws the real one
            FrameGameSystem.Draw(() =>
            {
                Assert.True(gate.HeldCount > 0, "The tessellated effect was not held back");
                Assert.True(fallbackCount > 0, "The fallback effect was not used");
                foreach (var renderEffect in GetRenderEffects())
                    Assert.Equal(RenderEffectState.Compiling, renderEffect.State);

                gate.Release();
                foreach (var renderEffect in GetRenderEffects())
                    renderEffect.PendingEffect?.Wait();
            }).TakeScreenshot();
        }

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

        [SkippableFact]
        public void RunTestGame()
        {
            SkipTestForPlatform(PlatformType.iOS);
            RunGameTest(new EffectFallbackTest());
        }

        /// <summary>
        /// Holds back the compilation of effects that use a tessellation shader until <see cref="Release"/>.
        /// </summary>
        private class GatedEffectCompiler : EffectCompilerChain
        {
            private readonly List<(TaskOrResult<EffectBytecodeCompilerResult> Inner, TaskCompletionSource<EffectBytecodeCompilerResult> Gate)> held = new();

            public volatile bool Hold;

            public int HeldCount { get { lock (held) return held.Count; } }

            public GatedEffectCompiler(EffectCompilerBase compiler) : base(compiler)
            {
            }

            public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters, ObjectId effectInputHash)
            {
                var result = base.Compile(mixinTree, effectParameters, compilerParameters, effectInputHash);
                if (!Hold || !compilerParameters.ContainsKey(MaterialKeys.TessellationShader))
                    return result;

                var gate = new TaskCompletionSource<EffectBytecodeCompilerResult>();
                lock (held)
                    held.Add((result, gate));
                return gate.Task;
            }

            public void Release()
            {
                Hold = false;
                lock (held)
                {
                    foreach (var (inner, gate) in held)
                    {
                        try
                        {
                            gate.SetResult(inner.WaitForResult());
                        }
                        catch (Exception e)
                        {
                            gate.SetException(e);
                        }
                    }
                }
            }
        }
    }
}
