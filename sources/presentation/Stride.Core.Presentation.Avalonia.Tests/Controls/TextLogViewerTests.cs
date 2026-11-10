// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

public sealed class TextLogViewerTests
{
    private static (Window Window, ScrollViewer Scroll, ObservableList<ILogMessage> Log) Show()
    {
        var log = new ObservableList<ILogMessage>();
        var window = new Window { Width = 400, Height = 200, Content = new TextLogViewer { LogMessages = log } };
        window.Styles.Add(new StyleInclude(new Uri("avares://Stride.Core.Presentation.Avalonia.Tests/"))
        {
            Source = new Uri("avares://Stride.Core.Presentation.Avalonia/Themes/TextLogViewerStyle.axaml"),
        });
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var scroll = window.GetVisualDescendants().OfType<ScrollViewer>().Single(x => x.Name == "PART_LogScrollViewer");
        return (window, scroll, log);
    }

    private static void AddLines(ObservableList<ILogMessage> log, string prefix, int count)
    {
        for (var i = 0; i < count; ++i)
            log.Add(new LogMessage("Test", LogMessageType.Info, $"{prefix} {i}"));
    }

    private static bool IsAtEnd(ScrollViewer scroll) => scroll.Extent.Height - scroll.Viewport.Height - scroll.Offset.Y < 1.0;

    [AvaloniaFact]
    public void TheLogFollowsItsEndWhileItGrows()
    {
        var (_, scroll, log) = Show();

        AddLines(log, "Line", 100);
        Dispatcher.UIThread.RunJobs();
        Assert.True(scroll.Extent.Height > 2 * scroll.Viewport.Height);
        Assert.True(IsAtEnd(scroll));

        AddLines(log, "More", 10);
        Dispatcher.UIThread.RunJobs();
        Assert.True(IsAtEnd(scroll));
    }

    [AvaloniaFact]
    public void ALogScrolledUpStaysWhereItIsWhenItGrows()
    {
        var (_, scroll, log) = Show();
        AddLines(log, "Line", 100);
        Dispatcher.UIThread.RunJobs();

        scroll.Offset = new Vector(0, 0);
        Dispatcher.UIThread.RunJobs();
        AddLines(log, "More", 10);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, scroll.Offset.Y);
    }

    [AvaloniaFact]
    public void AScrollUpBeforeTheLogCatchesUpWithItsEndKeepsItsPlace()
    {
        var (window, scroll, log) = Show();
        AddLines(log, "Line", 100);
        Dispatcher.UIThread.RunJobs();

        // New lines come while the end is shown: the layout sees them and the log will scroll to its new end...
        AddLines(log, "More", 10);
        window.UpdateLayout();
        // ...but the user scrolls up first
        scroll.Offset = new Vector(0, 0);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0, scroll.Offset.Y);
    }
}
