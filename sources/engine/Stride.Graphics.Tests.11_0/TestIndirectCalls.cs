// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

/// <summary>
///   Indirect draws and dispatches read their arguments from a Buffer, written by the CPU or by a compute shader.
/// </summary>
/// <remarks>
///   GPU-written arguments are transitioned to <see cref="BarrierLayout.IndirectArgument"/> before the indirect call.
///   Run with STRIDE_VULKAN_SYNC_VALIDATION=1 to have Vulkan report the hazard when that transition is missing.
/// </remarks>
public class TestIndirectCalls : GraphicTestGameBase
{
    private const int GroupCount = 3;
    private const int OutputSize = 8;
    private const int TargetSize = 4;

    // The first triangle is degenerate, the second covers the whole target
    private static readonly Vector2[] Positions =
    [
        Vector2.Zero, Vector2.Zero, Vector2.Zero,
        new(-1, -1), new(-1, 3), new(3, -1),
    ];

    // Each instance adds this color, so two instances give 0.5
    private static readonly Vector4 InstanceColor = new(0.25f, 0.25f, 0.25f, 1.0f);

    // The iOS simulator's Metal cannot draw with a base instance: MoltenVK fails the command buffer
    // with VK_ERROR_FEATURE_NOT_PRESENT. Real iOS devices (A9 and later) support it.
#if STRIDE_PLATFORM_IOS
    private static bool IsSimulator => ObjCRuntime.Runtime.Arch == ObjCRuntime.Arch.SIMULATOR;
#else
    private static bool IsSimulator => false;
#endif

    [SkippableFact]
    public void DispatchReadsCpuWrittenArguments()
    {
        PerformTest(game =>
        {
            // The arguments start after one unused value, to cover the offset
            using var arguments = Buffer.Argument.New(game.GraphicsDevice, new uint[] { 99, GroupCount, 1, 1 });

            var groups = Dispatch(game, arguments, sizeof(uint));

            Assert.Equal(ExpectedGroups(), groups);
        });
    }

    [SkippableFact]
    public void DispatchReadsGpuWrittenArguments()
    {
        PerformTest(game =>
        {
            using var arguments = Buffer.Argument.New(game.GraphicsDevice, 3 * sizeof(uint), unorderedAccess: true);
            WriteArguments(game, arguments, [GroupCount, 1, 1]);

            var groups = Dispatch(game, arguments, 0);

            Assert.Equal(ExpectedGroups(), groups);
        });
    }

    [SkippableFact]
    public void DrawInstancedReadsCpuWrittenArguments()
    {
        Skip.If(IsSimulator, "The iOS simulator cannot draw with a base instance.");

        PerformTest(game =>
        {
            // (vertex count, instance count, start vertex, start instance) after one unused value
            using var arguments = Buffer.Argument.New(game.GraphicsDevice, new uint[] { 99, 3, 2, 3, 1 });

            var color = Draw(game, commandList => commandList.DrawInstanced(arguments, sizeof(uint)));

            Assert.Equal(new Vector3(0.5f), color);
        });
    }

    [SkippableFact]
    public void DrawIndexedInstancedReadsGpuWrittenArguments()
    {
        Skip.If(IsSimulator, "The iOS simulator cannot draw with a base instance.");

        PerformTest(game =>
        {
            // (index count, instance count, start index, base vertex, start instance)
            using var arguments = Buffer.Argument.New(game.GraphicsDevice, 5 * sizeof(uint), unorderedAccess: true);
            WriteArguments(game, arguments, [3, 2, 3, 3, 1]);

            var color = Draw(game, commandList => commandList.DrawIndexedInstanced(arguments, 0));

            Assert.Equal(new Vector3(0.5f), color);
        });
    }

    private static uint[] ExpectedGroups()
    {
        var expected = new uint[OutputSize];
        for (uint i = 0; i < GroupCount; i++)
            expected[i] = i + 1;
        return expected;
    }

    /// <summary>
    ///   Writes <paramref name="values"/> into <paramref name="arguments"/> with a compute shader, then transitions it
    ///   for the indirect call.
    /// </summary>
    private static void WriteArguments(GameTestBase game, Buffer arguments, uint[] values)
    {
        var commandList = game.GraphicsContext.CommandList;
        var renderContext = RenderContext.GetShared(game.Services);

        using var source = Buffer.Structured.New(game.GraphicsDevice, values);
        using var writer = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "IndirectArgumentsWriterTestShader",
            ThreadNumbers = new Int3(values.Length, 1, 1),
        };

        commandList.ResourceBarrierTransition(source, BarrierLayout.ShaderResource);
        commandList.ResourceBarrierTransition(arguments, BarrierLayout.UnorderedAccess);
        writer.Parameters.Set(IndirectArgumentsWriterTestShaderKeys.Source, source);
        writer.Parameters.Set(IndirectArgumentsWriterTestShaderKeys.Arguments, arguments);
        ((RendererBase) writer).Draw(new RenderDrawContext(game.Services, renderContext, game.GraphicsContext));

        commandList.ResourceBarrierTransition(arguments, BarrierLayout.IndirectArgument);
    }

    /// <summary>
    ///   Runs an indirect dispatch and returns which thread groups ran.
    /// </summary>
    private static uint[] Dispatch(GameTestBase game, Buffer arguments, int offset)
    {
        var commandList = game.GraphicsContext.CommandList;
        var renderContext = RenderContext.GetShared(game.Services);

        using var output = Buffer.Structured.New(game.GraphicsDevice, new uint[OutputSize], unorderedAccess: true);
        using var effect = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "IndirectDispatchTestShader",
            IndirectArguments = arguments,
            IndirectArgumentsOffset = offset,
        };

        commandList.ResourceBarrierTransition(output, BarrierLayout.UnorderedAccess);
        effect.Parameters.Set(IndirectDispatchTestShaderKeys.Output, output);
        ((RendererBase) effect).Draw(new RenderDrawContext(game.Services, renderContext, game.GraphicsContext));

        return output.GetData<uint>(commandList);
    }

    /// <summary>
    ///   Runs an indirect draw into a small render target with additive blending, and returns the color it filled the
    ///   target with.
    /// </summary>
    private static Vector3 Draw(GameTestBase game, Action<CommandList> draw)
    {
        var device = game.GraphicsDevice;
        var commandList = game.GraphicsContext.CommandList;

        using var target = Texture.New2D(device, TargetSize, TargetSize, PixelFormat.R32G32B32A32_Float, TextureFlags.RenderTarget);
        using var vertexBuffer = Buffer.Vertex.New(device, Positions);
        // With start index 3 and base vertex 3, the draw reads vertices 3 to 5; from index 0 it reads a degenerate triangle
        using var indexBuffer = Buffer.Index.New(device, new ushort[] { 0, 0, 0, 0, 1, 2 });

        var effect = new EffectInstance(game.EffectSystem.LoadEffect("IndirectDrawTestShader").WaitForResult());
        effect.Parameters.Set(IndirectDrawTestShaderKeys.Color, InstanceColor);
        effect.UpdateEffect(device);

        var pipelineState = new MutablePipelineState(device);
        pipelineState.State.SetDefaults();
        pipelineState.State.RootSignature = effect.RootSignature;
        pipelineState.State.EffectBytecode = effect.Effect.Bytecode;
        pipelineState.State.BlendState = new BlendStateDescription(Blend.One, Blend.One);
        pipelineState.State.RasterizerState = RasterizerStates.CullNone;
        pipelineState.State.PrimitiveType = PrimitiveType.TriangleList;
        pipelineState.State.InputElements =
        [
            new InputElementDescription { SemanticName = "POSITION", Format = PixelFormat.R32G32_Float },
        ];

        commandList.Clear(target, Color4.Black);
        commandList.SetRenderTargetAndViewport(null, target);
        pipelineState.State.Output.CaptureState(commandList);
        pipelineState.Update();

        commandList.SetPipelineState(pipelineState.CurrentState);
        commandList.SetVertexBuffer(0, vertexBuffer, 0, 8);
        commandList.SetIndexBuffer(indexBuffer, 0, false);
        effect.Apply(game.GraphicsContext);

        draw(commandList);

        var pixels = target.GetData<Vector4>(commandList);
        foreach (var pixel in pixels)
        {
            Assert.Equal(pixels[0], pixel);
        }
        return new Vector3(pixels[0].X, pixels[0].Y, pixels[0].Z);
    }
}
