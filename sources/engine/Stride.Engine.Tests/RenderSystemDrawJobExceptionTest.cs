// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Rendering;
using Xunit;

namespace Stride.Engine.Tests;

/// <summary>
/// Covers a render feature that throws while <see cref="RenderSystem"/> draws a stage in parallel command lists.
/// </summary>
public class RenderSystemDrawJobExceptionTest : IDisposable
{
    private readonly GraphicsDevice graphicsDevice = GraphicsDevice.New(DeviceCreationFlags.Debug);

    public void Dispose() => graphicsDevice.Dispose();

    [SkippableFact]
    public void DrawJobExceptionReachesTheCallerAndTheFrameCanEnd()
    {
        Skip.IfNot(graphicsDevice.IsDeferred, "Only Direct3D 12 and Vulkan draw a stage in parallel command lists");

        var services = new ServiceRegistry();
        services.AddService<IGraphicsDeviceService>(new GraphicsDeviceServiceLocal(graphicsDevice));
        services.AddService(new EffectSystem(services));
        var graphicsContext = new GraphicsContext(graphicsDevice);
        services.AddService(graphicsContext);
        var context = RenderContext.GetShared(services);
        var renderSystem = new RenderSystem();
        context.RenderSystem = renderSystem;
        renderSystem.Initialize(context);

        // Write: no render effect to read the depth access from
        var stage = new RenderStage("Main", "Main") { DepthAccess = RenderStageDepthAccess.Write };
        renderSystem.RenderStages.Add(stage);

        // More nodes than one command list draws (64), so the stage is drawn in parallel
        var feature = new ThrowingRenderFeature();
        var renderNodes = new ConcurrentCollector<RenderNodeFeatureReference>();
        var sortedRenderNodes = new List<RenderNodeFeatureReference>();
        for (int i = 0; i < 200; i++)
        {
            var node = new RenderNodeFeatureReference(feature, new RenderNodeReference(i), null);
            renderNodes.Add(node);
            sortedRenderNodes.Add(node);
        }
        renderNodes.Close();
        var view = new RenderView();
        view.RenderStages.Add(new RenderViewStage(stage) { RenderNodes = renderNodes, SortedRenderNodes = sortedRenderNodes });

        // The frame's list knows the back buffer before the stage, as when a game draws its scene
        using var backBuffer = Texture.New2D(graphicsDevice, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
        var commandList = graphicsContext.CommandList;
        commandList.SetRenderTargetAndViewport(null, backBuffer);

        var drawContext = new RenderDrawContext(services, context, graphicsContext);
        var exception = Assert.Throws<InvalidOperationException>(() => renderSystem.Draw(drawContext, view, stage));
        Assert.Equal(ThrowingRenderFeature.Message, exception.Message);

        // What GameBase.EndDraw does after a failed Draw
        commandList.ResourceBarrierTransition(backBuffer, BarrierLayout.Present);
        commandList.Flush();
    }

    private sealed class ThrowingRenderFeature : RootRenderFeature
    {
        public const string Message = "The draw of a render node failed";

        public override Type SupportedRenderObjectType => typeof(RenderObject);

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            throw new InvalidOperationException(Message);
        }
    }
}
