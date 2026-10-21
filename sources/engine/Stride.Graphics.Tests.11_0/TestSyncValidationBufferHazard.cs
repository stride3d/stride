// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

/// <summary>
/// Checks that Vulkan synchronization validation reports a buffer hazard: a compute write read by a
/// second dispatch with no barrier between them. The read binds the buffer for unordered access, as
/// <see cref="ComputeEffectShader"/> transitions a buffer bound as shader resource, which orders the read.
/// </summary>
/// <remarks>
/// The barrier tests such as <see cref="TestBufferBarrier"/> pass when no hazard is reported, so they
/// cannot tell a correct barrier from a validation layer that does not look. Validation layers from
/// 1.3.290 ignore what shaders access through descriptors unless <c>syncval_shader_accesses_heuristic</c>
/// is on; this test fails if Stride stops turning it on.
/// </remarks>
[AllowGpuValidationError(GraphicsPlatform.Vulkan, "READ_AFTER_WRITE")]
[AllowGpuValidationError(GraphicsPlatform.Vulkan, "Scope tree:")]
public class TestSyncValidationBufferHazard : GraphicTestGameBase
{
    private const int ElementCount = 64;

    private ComputeEffectShader writeEffect;
    private ComputeEffectShader readEffect;
    private Buffer sharedBuffer;
    private Buffer resultBuffer;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(MissingBarrierIsReported);
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
            ShaderSourceName = "BufferHazardReadTestShader",
            ThreadNumbers = new Int3(ElementCount, 1, 1),
            ThreadGroupCounts = new Int3(1, 1, 1),
        };
        readEffect.DisposeBy(this);
    }

    private void MissingBarrierIsReported()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        // The validation layer reports while the command is recorded, on this thread
        var errors = new List<string>();
        void OnMessage(ILogMessage message)
        {
            if (message.Module == GraphicsDevice.DebugLogModule && message.Type == LogMessageType.Error)
                errors.Add(message.Text);
        }
        GlobalLogger.GlobalMessageLogged += OnMessage;
        try
        {
            commandList.ResourceBarrierTransition(sharedBuffer, BarrierLayout.UnorderedAccess);
            writeEffect.Parameters.Set(BufferBarrierTestShaderKeys.Output, sharedBuffer);
            ((RendererBase)writeEffect).Draw(renderDrawContext);

            // No UnorderedAccessBarrier on sharedBuffer: the read below races the write above
            commandList.ResourceBarrierTransition(resultBuffer, BarrierLayout.UnorderedAccess);
            readEffect.Parameters.Set(BufferHazardReadTestShaderKeys.Input, sharedBuffer);
            readEffect.Parameters.Set(BufferHazardReadTestShaderKeys.Result, resultBuffer);
            ((RendererBase)readEffect).Draw(renderDrawContext);
        }
        finally
        {
            GlobalLogger.GlobalMessageLogged -= OnMessage;
        }

        Assert.Contains(errors, error => error.Contains("READ_AFTER_WRITE"));
    }

    [SkippableFact]
    public void MissingBufferBarrierIsReported()
    {
        RequireShaderAccessSyncValidation();

        RunGameTest(new TestSyncValidationBufferHazard());
    }
}
