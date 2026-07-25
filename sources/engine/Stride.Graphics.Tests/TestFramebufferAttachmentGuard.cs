// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Graphics.Tests
{
    public class TestFramebufferAttachmentGuard : GraphicTestGameBase
    {
        private struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoords;
        }

        /// <summary>
        /// The Vulkan backend's debug guard turns a framebuffer/render-pass attachment-count mismatch into a clear
        /// exception instead of a device loss. Verify it fires when more render targets are bound than the active
        /// pipeline's render pass declares.
        /// </summary>
        [SkippableFact]
        public void ThrowsWhenBoundTargetsExceedPipelineRenderPass()
        {
            PerformTest(game =>
            {
                // The guard lives in the Vulkan backend (and needs the debug device, which GraphicTestGameBase sets).
                Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Attachment guard is Vulkan-only.");

                var device = game.GraphicsDevice;
                var commandList = game.GraphicsContext.CommandList;

                var backBuffer = device.Presenter.BackBuffer;
                var depth = device.Presenter.DepthStencilBuffer;
                var extraTarget = Texture.New2D(device, backBuffer.Width, backBuffer.Height, backBuffer.Format, TextureFlags.RenderTarget);

                var declaration = new VertexDeclaration(VertexElement.Position<Vector3>(), VertexElement.TextureCoordinate<Vector2>());
                var vertexBuffer = Buffer.Vertex.New(device, new Vertex[3], GraphicsResourceUsage.Default);
                var sampledTexture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);

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

                // Capture the pipeline Output with a single color + depth bound: its render pass declares 2 attachments.
                commandList.SetRenderTargetAndViewport(depth, backBuffer);
                pipelineState.State.Output.CaptureState(commandList);
                pipelineState.Update();

                // Now bind two color targets + depth: the framebuffer would have 3 attachments, mismatching the pass.
                commandList.SetRenderTargets(depth, backBuffer, extraTarget);
                commandList.SetPipelineState(pipelineState.CurrentState);
                commandList.SetVertexBuffer(0, vertexBuffer, 0, declaration.VertexStride);
                effect.Apply(game.GraphicsContext);

                var exception = Assert.Throws<InvalidOperationException>(() => commandList.Draw(3));
                Assert.Contains("render pass", exception.Message);

                sampledTexture.Dispose();
                extraTarget.Dispose();
            });
        }
    }
}
