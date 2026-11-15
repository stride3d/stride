// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Stride.Core.Diagnostics;
using Stride.Editor.Avalonia.Engine;

namespace Stride.Editor.Avalonia.Tests.Engine;

public sealed class GpuFramePresenterTests
{
    [AvaloniaFact]
    public void WithoutGpuInteropThePresenterFallsBackToReadbackAndSaysWhy()
    {
        // The headless platform has no GPU interop
        var view = new OffscreenGameView();
        var window = new Window { Width = 16, Height = 16, Content = view };
        window.Show();
        var logger = new LoggerResult();

        var presenter = GpuFramePresenter.Create(view, logger);
        for (var i = 0; i < 10 && presenter.State == GpuFramePresenterState.Pending; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        Assert.Equal(GpuFramePresenterState.Fallback, presenter.State);
        Assert.False(presenter.IsActive);
        Assert.Contains("no GPU interop", presenter.FallbackReason);
        Assert.False(view.HasDrawingSurface);
        Assert.True(logger.HasErrors == false);
        Assert.Contains(logger.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("read back"));
        presenter.Dispose();
    }

    [AvaloniaFact]
    public void ThePresenterWaitsForTheViewToBeInAVisualTree()
    {
        var view = new OffscreenGameView();
        var logger = new LoggerResult();

        var presenter = GpuFramePresenter.Create(view, logger);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(GpuFramePresenterState.Pending, presenter.State);

        var window = new Window { Width = 16, Height = 16, Content = view };
        window.Show();
        for (var i = 0; i < 10 && presenter.State == GpuFramePresenterState.Pending; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        Assert.Equal(GpuFramePresenterState.Fallback, presenter.State);
        presenter.Dispose();
    }
}
