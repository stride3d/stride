// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

/// <summary>
/// Covers a compute write handed to a second dispatch that reads it as a shader resource.
/// </summary>
/// <remarks>
/// The consumer is a dispatch rather than a readback: a readback copy emits its own barrier from
/// the buffer's access and stage masks, which would synchronise the write even with no transition
/// and leave the test unable to fail. Run with STRIDE_VULKAN_SYNC_VALIDATION=1 to have Vulkan
/// report the hazard when the transition is missing.
/// </remarks>
public class TestBufferBarrier : GraphicTestGameBase
{
    private const int ElementCount = 64;

    private ComputeEffectShader writeEffect;
    private ComputeEffectShader readEffect;
    private Buffer sharedBuffer;
    private Buffer resultBuffer;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(ComputeWriteIsVisibleToASubsequentDispatch);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        sharedBuffer = Buffer.Structured.New<uint>(GraphicsDevice, ElementCount, unorderedAccess: true).DisposeBy(this);
        resultBuffer = Buffer.Structured.New<uint>(GraphicsDevice, ElementCount, unorderedAccess: true).DisposeBy(this);

        var renderContext = RenderContext.GetShared(Services);
        writeEffect = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "BufferBarrierTestShader",
            ThreadNumbers = new Int3(ElementCount, 1, 1),
            ThreadGroupCounts = new Int3(1, 1, 1),
        };
        writeEffect.DisposeBy(this);

        readEffect = new ComputeEffectShader(renderContext)
        {
            ShaderSourceName = "BufferBarrierReadTestShader",
            ThreadNumbers = new Int3(ElementCount, 1, 1),
            ThreadGroupCounts = new Int3(1, 1, 1),
        };
        readEffect.DisposeBy(this);
    }

    private void ComputeWriteIsVisibleToASubsequentDispatch()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        commandList.ResourceBarrierTransition(sharedBuffer, BarrierLayout.UnorderedAccess);
        writeEffect.Parameters.Set(BufferBarrierTestShaderKeys.Output, sharedBuffer);
        ((RendererBase)writeEffect).Draw(renderDrawContext);

        commandList.ResourceBarrierTransition(sharedBuffer, BarrierLayout.ShaderResource);
        commandList.ResourceBarrierTransition(resultBuffer, BarrierLayout.UnorderedAccess);
        readEffect.Parameters.Set(BufferBarrierReadTestShaderKeys.Input, sharedBuffer);
        readEffect.Parameters.Set(BufferBarrierReadTestShaderKeys.Result, resultBuffer);
        ((RendererBase)readEffect).Draw(renderDrawContext);

        var values = resultBuffer.GetData<uint>(commandList);

        Assert.Equal(ElementCount, values.Length);
        for (uint i = 0; i < ElementCount; i++)
        {
            Assert.Equal((i * 3) + 1, values[i]);
        }
    }

    [SkippableFact]
    public void ComputeWriteIsVisibleToASubsequentRead()
    {
        RunGameTest(new TestBufferBarrier());
    }
}
