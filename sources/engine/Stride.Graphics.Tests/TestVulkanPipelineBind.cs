// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Graphics.Tests;

public class TestVulkanPipelineBind : GraphicTestGameBase
{
    private struct Vertex
    {
        public Vector3 Position;
        public Vector2 TexCoords;
    }

    /// <summary>
    /// The Vulkan backend binds a pipeline once, not before every draw: again only when <c>SetPipelineState</c> changes it.
    /// A new render pass instance (render targets set again) keeps it.
    /// </summary>
    [SkippableFact]
    public void PipelineIsBoundOncePerChange()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "vkCmdBindPipeline is Vulkan-only.");

#if STRIDE_GRAPHICS_API_VULKAN
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            var backBuffer = device.Presenter.BackBuffer;
            var depth = device.Presenter.DepthStencilBuffer;

            var declaration = new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.TextureCoordinate<Vector2>());
            var vertexBuffer = Buffer.Vertex.New(device, new Vertex[3], GraphicsResourceUsage.Default);
            var sampledTexture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);

            var effect = new EffectInstance(new Effect(device, SpriteEffect.Bytecode));
            effect.Parameters.Set(TexturingKeys.Texture0, sampledTexture);
            effect.Parameters.Set(TexturingKeys.Sampler, device.SamplerStates.LinearClamp);
            effect.UpdateEffect(device);

            commandList.SetRenderTargetAndViewport(depth, backBuffer);

            // Two pipelines that differ only by their blend state
            var mutablePipelineState = new MutablePipelineState(device);
            mutablePipelineState.State.SetDefaults();
            mutablePipelineState.State.RootSignature = effect.RootSignature;
            mutablePipelineState.State.EffectBytecode = effect.Effect.Bytecode;
            mutablePipelineState.State.InputElements = declaration.CreateInputElements();
            mutablePipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
            mutablePipelineState.State.Output.CaptureState(commandList);
            mutablePipelineState.Update();
            var opaque = mutablePipelineState.CurrentState;
            mutablePipelineState.State.BlendState = BlendStates.Additive;
            mutablePipelineState.Update();
            var additive = mutablePipelineState.CurrentState;
            Assert.NotSame(opaque, additive);

            void Use(PipelineState pipelineState)
            {
                commandList.SetPipelineState(pipelineState);
                commandList.SetVertexBuffer(0, vertexBuffer, 0, declaration.VertexStride);
                effect.Apply(game.GraphicsContext);
            }

            var start = commandList.NativePipelineBindCount;

            // Five draws with one pipeline: one bind
            Use(opaque);
            for (int i = 0; i < 5; i++)
                commandList.Draw(3);
            Assert.Equal(1, commandList.NativePipelineBindCount - start);

            // A new render pass instance keeps the bound pipeline
            commandList.SetRenderTargetAndViewport(depth, backBuffer);
            commandList.Draw(3);
            Assert.Equal(1, commandList.NativePipelineBindCount - start);

            // Each change binds once; setting the same pipeline again doesn't
            Use(additive);
            commandList.Draw(3);
            commandList.Draw(3);
            Use(opaque);
            commandList.Draw(3);
            Use(opaque);
            commandList.Draw(3);
            Assert.Equal(3, commandList.NativePipelineBindCount - start);

            sampledTexture.Dispose();
            vertexBuffer.Dispose();
#endif
        });
    }
}
