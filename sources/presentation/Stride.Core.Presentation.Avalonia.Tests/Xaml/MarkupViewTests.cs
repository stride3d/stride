// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace Stride.Core.Presentation.Avalonia.Tests.Xaml;

public sealed class MarkupViewTests
{
    private static MarkupView ShowView()
    {
        var view = new MarkupView();
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return view;
    }

    [AvaloniaFact]
    public void MarkupExtensionsProvideTheirValues()
    {
        var view = ShowView();

        Assert.Equal(new Thickness(1, 2, 3, 4), view.ThicknessBorder.Margin);
        Assert.Equal(new Thickness(5), view.UniformBorder.Padding);
        Assert.Equal("Save (Ctrl+S)", view.ToolTipText.Text);
        Assert.Equal("Hello", view.LocalizedText.Text);
        Assert.Equal(int.MaxValue, view.Maximum.Tag);
        Assert.Same(view, view.Root.Tag);
    }

    [AvaloniaFact]
    public void ConvertersFollowTheBoundValues()
    {
        var view = ShowView();

        Assert.False(view.Inverted.IsVisible);
        Assert.Equal(0.5, view.Dimmed.Opacity);
        Assert.False(view.AndResult.IsVisible);
        Assert.True(view.OrResult.IsVisible);

        view.Source.IsChecked = false;
        view.Other.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(view.Inverted.IsVisible);
        Assert.Equal(1.0, view.Dimmed.Opacity);
        Assert.False(view.AndResult.IsVisible);
        Assert.True(view.OrResult.IsVisible);

        view.Source.IsChecked = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(view.AndResult.IsVisible);
    }
}
