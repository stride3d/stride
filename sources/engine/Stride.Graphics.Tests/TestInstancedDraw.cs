// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;

namespace Stride.Graphics.Tests;

/// <summary>
///   Instanced draws read per-instance vertex data starting at the start instance location.
/// </summary>
public class TestInstancedDraw : GameTestBase
{
    private const int TargetSize = 4;

    // The first triangle is degenerate, the second covers the whole target, so only a draw that
    // starts at vertex 3 fills the target
    private static readonly Vector2[] Positions =
    [
        Vector2.Zero, Vector2.Zero, Vector2.Zero,
        new(-1, -1), new(-1, 3), new(3, -1),
    ];

    private static readonly Color[] InstanceColors = [Color.Red, Color.Lime, Color.Blue, Color.Yellow];

    [SkippableFact]
    public void DrawInstancedStartsAtTheStartInstance()
    {
        PerformTest(game =>
        {
            var color = Draw(game, commandList => commandList.DrawInstanced(3, 1, startVertexLocation: 3, startInstanceLocation: 1));

            Assert.Equal(Color.Lime, color);
        });
    }

    [SkippableFact]
    public void DrawIndexedInstancedStartsAtTheStartInstance()
    {
        PerformTest(game =>
        {
            var color = Draw(game, commandList => commandList.DrawIndexedInstanced(3, 1, startIndexLocation: 0, baseVertexLocation: 3, startInstanceLocation: 2));

            Assert.Equal(Color.Blue, color);
        });
    }

    /// <summary>
    ///   Runs the draw into a small render target and returns the color it filled the target with.
    /// </summary>
    private static Color Draw(GameTestBase game, Action<CommandList> draw)
    {
        var device = game.GraphicsDevice;
        var commandList = game.GraphicsContext.CommandList;

        using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
        using var positionBuffer = Buffer.Vertex.New(device, Positions);
        using var colorBuffer = Buffer.Vertex.New(device, InstanceColors);
        using var indexBuffer = Buffer.Index.New(device, new ushort[] { 0, 1, 2 });

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
                InputSlot = 1,
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
        commandList.SetVertexBuffer(1, colorBuffer, 0, 4);
        commandList.SetIndexBuffer(indexBuffer, 0, false);
        effect.Apply(game.GraphicsContext);

        draw(commandList);

        var pixels = target.GetData<Color>(commandList);
        foreach (var pixel in pixels)
        {
            Assert.Equal(pixels[0], pixel);
        }
        return pixels[0];
    }
}
