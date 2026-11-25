// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;

namespace Stride.Graphics.Tests;

public class TestFramebufferAttachmentGuard : GraphicTestGameBase
{
    private const int TargetSize = 4;

    private struct Vertex
    {
        public Vector3 Position;
        public Vector2 TexCoords;
    }

    /// <summary>
    /// The Vulkan backend's debug guard turns an attachment mismatch with the pipeline's declared output
    /// into a clear exception instead of a device loss. Verify it fires when more render targets are bound
    /// than the active pipeline's output declares.
    /// </summary>
    [SkippableFact]
    public void ThrowsWhenBoundTargetsExceedPipelineOutput()
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

            // Capture the pipeline Output with a single color + depth bound: it declares 2 attachments.
            commandList.SetRenderTargetAndViewport(depth, backBuffer);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();

            // Now bind two color targets + depth: 3 attachments, mismatching the pipeline output.
            commandList.SetRenderTargets(depth, backBuffer, extraTarget);
            commandList.SetPipelineState(pipelineState.CurrentState);
            commandList.SetVertexBuffer(0, vertexBuffer, 0, declaration.VertexStride);
            effect.Apply(game.GraphicsContext);

            var exception = Assert.Throws<InvalidOperationException>(() => commandList.Draw(3));
            Assert.Contains("do not match the active pipeline", exception.Message);

            sampledTexture.Dispose();
            extraTarget.Dispose();
        });
    }

    [SkippableFact]
    public void ColorFormatMismatchThrows()
    {
        Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Only Vulkan checks the attachment formats");

        PerformTest(game =>
        {
            using var target = Texture.New2D(game.GraphicsDevice, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.RenderTarget);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                Draw(game, PixelFormat.R8G8B8A8_UNorm, PixelFormat.None, target, null));

            Assert.Contains("R8G8B8A8_UNorm_SRgb", exception.Message);
            Assert.Contains("Expected: color [R8G8B8A8_UNorm]", exception.Message);
        });
    }

    [SkippableFact]
    public void DepthFormatMismatchThrows()
    {
        Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Only Vulkan checks the attachment formats");

        PerformTest(game =>
        {
            using var target = Texture.New2D(game.GraphicsDevice, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var depth = Texture.New2D(game.GraphicsDevice, TargetSize, TargetSize, PixelFormat.D32_Float, TextureFlags.DepthStencil);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                Draw(game, PixelFormat.R8G8B8A8_UNorm, PixelFormat.D16_UNorm, target, depth));

            Assert.Contains("D32_Float", exception.Message);
            Assert.Contains("D16_UNorm", exception.Message);
        });
    }

    [SkippableFact]
    public void PipelineSwitchInsideRenderPassThrows()
    {
        Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Only Vulkan checks the attachment formats");

        PerformTest(game =>
        {
            using var target = Texture.New2D(game.GraphicsDevice, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);

            Assert.Throws<InvalidOperationException>(() =>
                Draw(game, PixelFormat.R8G8B8A8_UNorm, PixelFormat.None, target, null, secondColorFormat: PixelFormat.R8G8B8A8_UNorm_SRgb));
        });
    }

    [Fact]
    public void MatchingFormatsDoNotThrow()
    {
        PerformTest(game =>
        {
            using var target = Texture.New2D(game.GraphicsDevice, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var depth = Texture.New2D(game.GraphicsDevice, TargetSize, TargetSize, PixelFormat.D32_Float, TextureFlags.DepthStencil);

            Draw(game, PixelFormat.R8G8B8A8_UNorm, PixelFormat.D32_Float, target, depth, secondColorFormat: PixelFormat.R8G8B8A8_UNorm);
        });
    }

    /// <summary>
    ///   Binds the attachments and draws with a pipeline built for the given formats; when <paramref name="secondColorFormat"/>
    ///   is set, draws again with a second pipeline built for that color format, without rebinding the attachments.
    /// </summary>
    private static void Draw(GameTestBase game, PixelFormat colorFormat, PixelFormat depthFormat, Texture target, Texture depth, PixelFormat? secondColorFormat = null)
    {
        var device = game.GraphicsDevice;
        var commandList = game.GraphicsContext.CommandList;

        using var positionBuffer = Buffer.Vertex.New(device, new[] { new Vector2(-1, -1), new Vector2(-1, 3), new Vector2(3, -1) });
        using var colorBuffer = Buffer.Vertex.New(device, new[] { Color.Lime });

        var effect = new EffectInstance(game.EffectSystem.LoadEffect("InstancedDrawTestShader").WaitForResult());
        effect.UpdateEffect(device);

        var firstPipeline = CreatePipelineState(device, effect, colorFormat, depthFormat);
        var secondPipeline = secondColorFormat is { } format ? CreatePipelineState(device, effect, format, depthFormat) : null;

        commandList.SetRenderTargetAndViewport(depth, target);
        commandList.SetVertexBuffer(0, positionBuffer, 0, 8);
        commandList.SetVertexBuffer(1, colorBuffer, 0, 4);

        commandList.SetPipelineState(firstPipeline.CurrentState);
        effect.Apply(game.GraphicsContext);
        commandList.DrawInstanced(3, 1);

        if (secondPipeline != null)
        {
            commandList.SetPipelineState(secondPipeline.CurrentState);
            effect.Apply(game.GraphicsContext);
            commandList.DrawInstanced(3, 1);
        }
    }

    private static MutablePipelineState CreatePipelineState(GraphicsDevice device, EffectInstance effect, PixelFormat colorFormat, PixelFormat depthFormat)
    {
        var pipelineState = new MutablePipelineState(device);
        pipelineState.State.SetDefaults();
        pipelineState.State.RootSignature = effect.RootSignature;
        pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
        pipelineState.State.RasterizerState = RasterizerStates.CullNone;
        pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
        pipelineState.State.InputElements =
        [
            new InputElementDescription { SemanticName = "POSITION", Format = PixelFormat.R32G32_Float, InputSlot = 0 },
            new InputElementDescription
            {
                SemanticName = "COLOR",
                Format = PixelFormat.R8G8B8A8_UNorm,
                InputSlot = 1,
                InputSlotClass = InputClassification.Instance,
                InstanceDataStepRate = 1,
            },
        ];
        pipelineState.State.Output.RenderTargetCount = 1;
        pipelineState.State.Output.RenderTargetFormat0 = colorFormat;
        pipelineState.State.Output.DepthStencilFormat = depthFormat;
        pipelineState.Update();
        return pipelineState;
    }
}
