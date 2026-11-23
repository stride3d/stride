// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Graphics.Tests;

/// <summary>
///   A pipeline whose vertex streams are in slots that are not consecutive, such as 0 and 5, builds and draws.
/// </summary>
public class TestSparseVertexSlots : GraphicTestGameBase
{
    private const int TargetSize = 4;
    private const int ColorSlot = 5;

    [Fact]
    public void DrawWithAGapBetweenVertexSlots()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var positionBuffer = Buffer.Vertex.New(device, new[] { new Vector2(-1, -1), new Vector2(-1, 3), new Vector2(3, -1) });
            using var colorBuffer = Buffer.Vertex.New(device, new[] { Color.Lime });

            var effect = new EffectInstance(game.EffectSystem.LoadEffect("InstancedDrawTestShader").WaitForResult());
            effect.UpdateEffect(device);

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
                    InputSlot = ColorSlot,
                    InputSlotClass = InputClassification.Instance,
                    InstanceDataStepRate = 1,
                },
            ];

            commandList.Clear(target, Color.Black);
            commandList.SetRenderTargetAndViewport(null, target);
            pipelineState.State.Output.CaptureState(commandList);
            pipelineState.Update();

            commandList.SetPipelineState(pipelineState.CurrentState);
            commandList.SetVertexBuffer(0, positionBuffer, 0, 8);
            commandList.SetVertexBuffer(ColorSlot, colorBuffer, 0, 4);
            effect.Apply(game.GraphicsContext);
            commandList.DrawInstanced(3, 1);

            foreach (var pixel in target.GetData<Color>(commandList))
            {
                Assert.Equal(Color.Lime, pixel);
            }

            effect.Dispose();
        });
    }
}
