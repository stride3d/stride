// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;

namespace Stride.Graphics.Tests;

/// <summary>
///   Mapping a Dynamic Buffer with <see cref="MapMode.WriteDiscard"/> leaves the draws recorded before it reading the old contents.
/// </summary>
public class TestDynamicBufferDiscard : GameTestBase
{
    private const int TargetSize = 4;

    // One triangle that covers the whole target
    private static readonly Vector2[] Positions = [new(-1, -1), new(-1, 3), new(3, -1)];

    [Fact]
    public void DiscardKeepsTheContentsOfEarlierDraws()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var targetA = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var targetB = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, Positions);
            using var colorBuffer = Buffer.Vertex.New(device, 4 * sizeof(uint), GraphicsResourceUsage.Dynamic);

            using var drawer = new Drawer(game, targetA);

            colorBuffer.SetData(commandList, new[] { Color.Red });
            drawer.Draw(targetA, positionBuffer, colorBuffer, instance: 0);

            colorBuffer.SetData(commandList, new[] { Color.Blue });
            drawer.Draw(targetB, positionBuffer, colorBuffer, instance: 0);

            Assert.Equal(Color.Red, ReadColor(commandList, targetA));
            Assert.Equal(Color.Blue, ReadColor(commandList, targetB));
        });
    }

    [Fact]
    public unsafe void NoOverwriteAfterDiscardWritesTheSameBuffer()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var targetA = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var targetB = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, Positions);
            using var colorBuffer = Buffer.Vertex.New(device, 4 * sizeof(uint), GraphicsResourceUsage.Dynamic);

            using var drawer = new Drawer(game, targetA);

            // Discard and write the first instance, then append the second one without discarding
            var discarded = commandList.MapSubResource(colorBuffer, 0, MapMode.WriteDiscard);
            ((Color*) discarded.DataBox.DataPointer)[0] = Color.Red;
            commandList.UnmapSubResource(discarded);

            var appended = commandList.MapSubResource(colorBuffer, 0, MapMode.WriteNoOverwrite);
            ((Color*) appended.DataBox.DataPointer)[1] = Color.Lime;
            commandList.UnmapSubResource(appended);

            drawer.Draw(targetA, positionBuffer, colorBuffer, instance: 0);
            drawer.Draw(targetB, positionBuffer, colorBuffer, instance: 1);

            Assert.Equal(Color.Red, ReadColor(commandList, targetA));
            Assert.Equal(Color.Lime, ReadColor(commandList, targetB));
        });
    }

    private static Color ReadColor(CommandList commandList, Texture target)
    {
        var pixels = target.GetData<Color>(commandList);
        foreach (var pixel in pixels)
        {
            Assert.Equal(pixels[0], pixel);
        }
        return pixels[0];
    }

    /// <summary>
    ///   Draws one triangle that fills the target with the color of an instance read from a per-instance vertex stream.
    /// </summary>
    private sealed class Drawer : IDisposable
    {
        private readonly GameTestBase game;
        private readonly MutablePipelineState pipelineState;
        private readonly EffectInstance effect;

        public Drawer(GameTestBase game, Texture target)
        {
            this.game = game;
            var device = game.GraphicsDevice;

            pipelineState = new MutablePipelineState(device);
            pipelineState.State.SetDefaults();
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

            effect = new EffectInstance(game.EffectSystem.LoadEffect("InstancedDrawTestShader").WaitForResult());
            effect.UpdateEffect(device);
            pipelineState.State.RootSignature = effect.RootSignature;
            pipelineState.State.EffectBytecode = effect.Effect.Bytecode;

            var commandList = game.GraphicsContext.CommandList;
            commandList.SetRenderTargetAndViewport(null, target);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();
        }

        public void Draw(Texture target, Buffer positionBuffer, Buffer colorBuffer, int instance)
        {
            var commandList = game.GraphicsContext.CommandList;

            commandList.Clear(target, Color.Black);
            commandList.SetRenderTargetAndViewport(null, target);
            commandList.SetPipelineState(pipelineState.CurrentState);
            commandList.SetVertexBuffer(0, positionBuffer, 0, 8);
            commandList.SetVertexBuffer(1, colorBuffer, 0, 4);
            effect.Apply(game.GraphicsContext);

            commandList.DrawInstanced(3, 1, startInstanceLocation: instance);
        }

        public void Dispose()
        {
            effect.Dispose();
        }
    }
}
