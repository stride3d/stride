// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

/// <summary>
/// Covers a buffer written as unordered access by one call and read as unordered access by the next dispatch: the layout
/// doesn't change, so only <see cref="CommandList.UnorderedAccessBarrier(GraphicsResource)"/> (or the clear itself) can
/// order the two.
/// </summary>
/// <remarks>
/// The first call is slow (a busy loop on real GPUs) and the second one starts with the elements the first one writes
/// last, so a GPU that overlaps them gives wrong values. WARP overlaps them without the loop; Lavapipe runs them in order.
/// Vulkan synchronization validation doesn't report a missing barrier between two dispatches.
/// </remarks>
public class TestUnorderedAccessBarrier : GraphicTestGameBase
{
    private const int ThreadsPerGroup = 64;
    private const int ElementCount = ThreadsPerGroup * 4096;
    private const int Runs = 8;

    public enum Case
    {
        ResourceBarrier,
        GlobalBarrier,
        Clear,
    }

    private readonly Case testCase;

    private ComputeEffectShader slowWriteEffect;
    private ComputeEffectShader reverseIncrementEffect;
    private Buffer buffer;

    public TestUnorderedAccessBarrier() : this(Case.ResourceBarrier)
    {
    }

    private TestUnorderedAccessBarrier(Case testCase)
    {
        this.testCase = testCase;
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(WriteThenIncrement);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        buffer = Buffer.Structured.New<uint>(GraphicsDevice, ElementCount, unorderedAccess: true).DisposeBy(this);

        var renderContext = RenderContext.GetShared(Services);
        slowWriteEffect = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "UnorderedAccessSlowWriteTestShader",
            ThreadNumbers = new Int3(ThreadsPerGroup, 1, 1),
            ThreadGroupCounts = new Int3(ElementCount / ThreadsPerGroup, 1, 1),
        };
        slowWriteEffect.DisposeBy(this);

        reverseIncrementEffect = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "UnorderedAccessReverseIncrementTestShader",
            ThreadNumbers = new Int3(ThreadsPerGroup, 1, 1),
            ThreadGroupCounts = new Int3(ElementCount / ThreadsPerGroup, 1, 1),
        };
        reverseIncrementEffect.DisposeBy(this);
    }

    private void WriteThenIncrement()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        var iterations = GraphicsDevice.Adapter.IsSoftwareAdapter ? 0u : 20000u;
        for (int run = 0; run < Runs; run++)
        {
            // The read-back of the previous run copied the buffer
            commandList.ResourceBarrierTransition(buffer, BarrierLayout.UnorderedAccess);

            // A slow write of index * 3
            slowWriteEffect.Parameters.Set(UnorderedAccessSlowWriteTestShaderKeys.Output, buffer);
            slowWriteEffect.Parameters.Set(UnorderedAccessSlowWriteTestShaderKeys.Iterations, iterations);
            ((RendererBase) slowWriteEffect).Draw(renderDrawContext);

            Func<uint, uint> expected = index => index * 3 + 1;
            switch (testCase)
            {
                case Case.ResourceBarrier:
                    commandList.UnorderedAccessBarrier(buffer);
                    break;
                case Case.GlobalBarrier:
                    commandList.UnorderedAccessBarrier();
                    break;
                case Case.Clear:
                    // The clear is ordered after the write and before the increment, with no barrier call
                    commandList.ClearReadWrite(buffer, new UInt4(7));
                    expected = _ => 8;
                    break;
            }

            // An increment, last element first
            reverseIncrementEffect.Parameters.Set(UnorderedAccessReverseIncrementTestShaderKeys.Data, buffer);
            reverseIncrementEffect.Parameters.Set(UnorderedAccessReverseIncrementTestShaderKeys.Count, (uint) ElementCount);
            ((RendererBase) reverseIncrementEffect).Draw(renderDrawContext);

            var values = buffer.GetData<uint>(commandList);
            int wrong = 0;
            int firstWrong = -1;
            for (uint i = 0; i < ElementCount; i++)
            {
                if (values[i] != expected(i))
                {
                    wrong++;
                    if (firstWrong < 0)
                        firstWrong = (int) i;
                }
            }
            Assert.True(wrong == 0, $"Run {run}: {wrong} of {ElementCount} elements are wrong (first: [{firstWrong}] = {(firstWrong >= 0 ? values[firstWrong] : 0)}, expected {(firstWrong >= 0 ? expected((uint) firstWrong) : 0)})");
        }
    }

    [SkippableFact]
    public void ResourceBarrierOrdersTwoDispatches()
    {
        RunGameTest(new TestUnorderedAccessBarrier(Case.ResourceBarrier));
    }

    [SkippableFact]
    public void GlobalBarrierOrdersTwoDispatches()
    {
        RunGameTest(new TestUnorderedAccessBarrier(Case.GlobalBarrier));
    }

    [SkippableFact]
    public void ClearReadWriteIsOrderedWithDispatches()
    {
        Skip.If(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "ClearReadWrite(Buffer) is not implemented on Vulkan yet.");

        RunGameTest(new TestUnorderedAccessBarrier(Case.Clear));
    }
}
