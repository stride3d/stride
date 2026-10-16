// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Graphics.Tests
{
    public class TestCaptureState : GraphicTestGameBase
    {
        private struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoords;
        }

        /// <summary>
        /// A pipeline state captured with a color target, then captured again with only a depth buffer bound
        /// (a depth-only pass reusing the same <see cref="MutablePipelineState"/>).
        /// </summary>
        [SkippableFact]
        public void DepthOnlyCaptureAfterColorCaptureHasNoColorFormats()
        {
            PerformTest(game =>
            {
                var device = game.GraphicsDevice;
                var commandList = game.GraphicsContext.CommandList;

                using var color = Texture.New2D(device, 64, 64, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
                using var depth = Texture.New2D(device, 64, 64, PixelFormat.D32_Float, TextureFlags.DepthStencil);
                using var sampledTexture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);

                var declaration = new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.TextureCoordinate<Vector2>());
                var effect = new EffectInstance(new Effect(device, SpriteEffect.Bytecode));
                effect.Parameters.Set(TexturingKeys.Texture0, sampledTexture);
                effect.Parameters.Set(TexturingKeys.Sampler, device.SamplerStates.LinearClamp);
                effect.UpdateEffect(device);

                var pipelineState = new MutablePipelineState(device);
                pipelineState.State.SetDefaults();
                pipelineState.State.RootSignature = effect.RootSignature;
                pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
                pipelineState.State.InputElements = declaration.CreateInputElements();
                pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;

                commandList.SetRenderTargetAndViewport(depth, color);
                pipelineState.State.Output.CaptureState(commandList);
                pipelineState.Update();

                // Depth only: no render target, and no pixel shader to write one
                var bytecode = effect.Effect.Bytecode;
                pipelineState.State.EffectBytecode = new EffectBytecode
                {
                    Reflection = bytecode.Reflection,
                    HashSources = bytecode.HashSources,
                    Stages = bytecode.Stages.Where(stage => stage.Stage == ShaderStage.Vertex).ToArray(),
                };
                commandList.SetRenderTargetAndViewport(depth, null);
                pipelineState.State.Output.CaptureState(commandList);

                pipelineState.Update();
                Assert.NotNull(pipelineState.CurrentState);

                Assert.Equal(0, pipelineState.State.Output.RenderTargetCount);
                foreach (var format in pipelineState.State.Output.RenderTargetFormats)
                    Assert.Equal(PixelFormat.None, format);

                // Equal to a depth-only description built from scratch, so both share one cached pipeline state
                Assert.Equal(new RenderOutputDescription(PixelFormat.None, PixelFormat.D32_Float), pipelineState.State.Output);

                // The scene draw after the test needs a render target
                commandList.SetRenderTargetAndViewport(device.Presenter.DepthStencilBuffer, device.Presenter.BackBuffer);
            });
        }
    }
}
