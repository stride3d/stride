// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using Stride.Rendering.Images;

namespace Stride.Graphics.Tests;

/// <summary>
/// Push constants (<c>[PushConstant] cbuffer</c>, <see cref="CommandList.SetPushConstants(ReadOnlySpan{byte}, int)"/>) read by a
/// dispatch and by a draw, with real values: changed between calls, partly updated, kept across a pipeline switch and a flush.
/// </summary>
/// <remarks>
/// Each dispatch writes its own buffer, so no barrier is needed between them. Run with STRIDE_VULKAN_SYNC_VALIDATION=1
/// for Vulkan synchronization validation; the Direct3D 12 debug layer is on in the tests.
/// </remarks>
public class TestPushConstants : GraphicTestGameBase
{
    // The C# side of PushConstantsComputeTestShader.PushData: the same offsets as the shader (0, 16, 24, 28)
    [StructLayout(LayoutKind.Sequential)]
    private struct ComputePushData
    {
        public UInt4 Values;
        public Vector2 Scale;
        public uint Slot;
        public uint Factor;
    }

    // The C# side of PushConstantsDrawTestShader.PushData
    [StructLayout(LayoutKind.Sequential)]
    private struct DrawPushData
    {
        public Vector4 Color;
        public float Intensity;
    }

    private const int SlotCount = 4;
    private const int SlotSize = 8;
    private const int SlotOffset = 24;

    private ComputeEffectShader pushEffect;
    private ComputeEffectShader otherEffect;
    private ImageEffectShader drawEffect;
    private ImageEffectShader plainDrawEffect;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(DispatchReadsThePushConstants).Draw(DrawReadsThePushConstants);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var renderContext = RenderContext.GetShared(Services);
        pushEffect = new ComputeEffectShader(renderContext) { ShaderSourceName = "PushConstantsComputeTestShader", ThreadNumbers = new Int3(1, 1, 1), ThreadGroupCounts = new Int3(1, 1, 1) };
        pushEffect.DisposeBy(this);
        otherEffect = new ComputeEffectShader(renderContext) { ShaderSourceName = "PushConstantsOtherComputeTestShader", ThreadNumbers = new Int3(1, 1, 1), ThreadGroupCounts = new Int3(1, 1, 1) };
        otherEffect.DisposeBy(this);
        drawEffect = new ImageEffectShader("PushConstantsDrawTestShader").DisposeBy(this);
        plainDrawEffect = new ImageEffectShader("PushConstantsPlainDrawTestShader").DisposeBy(this);
    }

    private void DispatchReadsThePushConstants()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        var outputs = new Buffer[4];
        for (int i = 0; i < outputs.Length; i++)
        {
            outputs[i] = Buffer.Structured.New<uint>(GraphicsDevice, SlotCount * SlotSize, unorderedAccess: true).DisposeBy(this);
            commandList.ResourceBarrierTransition(outputs[i], BarrierLayout.UnorderedAccess);
        }
        var otherOutput = Buffer.Structured.New<uint>(GraphicsDevice, 1, unorderedAccess: true).DisposeBy(this);
        commandList.ResourceBarrierTransition(otherOutput, BarrierLayout.UnorderedAccess);

        void Dispatch(Buffer output)
        {
            pushEffect.Parameters.Set(PushConstantsComputeTestShaderKeys.Output, output);
            ((RendererBase)pushEffect).Draw(renderDrawContext);
        }

        // 1. The whole block
        commandList.SetPushConstants(new ComputePushData { Values = new UInt4(1, 2, 3, 4), Scale = new Vector2(0.5f, 2), Slot = 0, Factor = 1 });
        Dispatch(outputs[0]);

        // 2. Other values, next dispatch
        commandList.SetPushConstants(new ComputePushData { Values = new UInt4(10, 20, 30, 40), Scale = new Vector2(3, 4), Slot = 1, Factor = 2 });
        Dispatch(outputs[1]);

        // 3. Another pipeline (without push constants) in between, then only Slot changes: the rest is kept
        otherEffect.Parameters.Set(PushConstantsOtherComputeTestShaderKeys.Output, otherOutput);
        ((RendererBase)otherEffect).Draw(renderDrawContext);
        SetSlot(commandList, 2);
        Dispatch(outputs[2]);

        // A readback flushes the command list
        AssertSlot(outputs[0].GetData<uint>(commandList), 0, [1, 2, 3, 4], new Vector2(0.5f, 2), 1);
        AssertSlot(outputs[1].GetData<uint>(commandList), 1, [20, 40, 60, 80], new Vector2(3, 4), 2);
        AssertSlot(outputs[2].GetData<uint>(commandList), 2, [20, 40, 60, 80], new Vector2(3, 4), 2);
        Assert.Equal(0xDEADu, otherOutput.GetData<uint>(commandList)[0]);

        // 4. After the flush, the values are still there
        commandList.ResourceBarrierTransition(outputs[3], BarrierLayout.UnorderedAccess);
        SetSlot(commandList, 3);
        Dispatch(outputs[3]);
        AssertSlot(outputs[3].GetData<uint>(commandList), 3, [20, 40, 60, 80], new Vector2(3, 4), 2);
    }

    private static void SetSlot(CommandList commandList, uint slot)
        => commandList.SetPushConstants(MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(in slot)), SlotOffset);

    private static void AssertSlot(uint[] values, int slot, uint[] expectedValues, Vector2 expectedScale, uint expectedFactor)
    {
        var start = slot * SlotSize;
        Assert.Equal(expectedValues, values.AsSpan(start, 4).ToArray());
        Assert.Equal(expectedScale.X, BitConverter.UInt32BitsToSingle(values[start + 4]));
        Assert.Equal(expectedScale.Y, BitConverter.UInt32BitsToSingle(values[start + 5]));
        Assert.Equal((uint)slot, values[start + 6]);
        Assert.Equal(expectedFactor, values[start + 7]);
    }

    private void DrawReadsThePushConstants()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        var targets = new Texture[4];
        for (int i = 0; i < targets.Length; i++)
            targets[i] = Texture.New2D(GraphicsDevice, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource).DisposeBy(this);

        void Draw(ImageEffectShader effect, Texture target)
        {
            effect.SetOutput(target);
            effect.Draw(renderDrawContext);
        }

        // Values changed between two draws
        commandList.SetPushConstants(new DrawPushData { Color = new Vector4(1, 0, 0, 1), Intensity = 1 });
        Draw(drawEffect, targets[0]);
        commandList.SetPushConstants(new DrawPushData { Color = new Vector4(0, 1, 0, 1), Intensity = 0.5f });
        Draw(drawEffect, targets[1]);

        // Another pipeline in between, then the same values again without SetPushConstants
        Draw(plainDrawEffect, targets[2]);
        Draw(drawEffect, targets[3]);

        AssertColor(new Color(255, 0, 0, 255), targets[0].GetData<Color>(commandList));
        AssertColor(new Color(0, 128, 0, 255), targets[1].GetData<Color>(commandList));
        AssertColor(new Color(0, 0, 255, 255), targets[2].GetData<Color>(commandList));
        AssertColor(new Color(0, 128, 0, 255), targets[3].GetData<Color>(commandList));
    }

    private static void AssertColor(Color expected, Color[] pixels)
    {
        foreach (var pixel in pixels)
        {
            // 0.5 may round to 127 or 128
            Assert.InRange(pixel.R, Math.Max(expected.R - 1, 0), expected.R);
            Assert.InRange(pixel.G, Math.Max(expected.G - 1, 0), expected.G);
            Assert.InRange(pixel.B, Math.Max(expected.B - 1, 0), expected.B);
            Assert.Equal(expected.A, pixel.A);
        }
    }

    [SkippableFact]
    public void PushConstantsReachDispatchesAndDraws()
    {
        RunGameTest(new TestPushConstants());
    }
}
