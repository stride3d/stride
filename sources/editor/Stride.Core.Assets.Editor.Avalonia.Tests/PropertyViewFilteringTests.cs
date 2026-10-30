// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.Avalonia.Behaviors;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The search of the property grid: <see cref="PropertyFilter"/> and <see cref="PropertyViewFilteringBehavior"/>.
/// </summary>
public sealed class PropertyViewFilteringTests
{
    [AvaloniaFact]
    public void APropertyMatchesByItsNameOrTheNameOfANestedProperty()
    {
        var root = new NodeViewModelContext().RootOf(new FilterSample());
        var inner = root.GetChild(nameof(FilterSample.Inner))!;

        Assert.True(PropertyFilter.Matches(inner, "inn"));
        // Ignoring case
        Assert.True(PropertyFilter.Matches(inner, "INNER"));
        // A nested property matches
        Assert.True(PropertyFilter.Matches(inner, "rad"));
        Assert.False(PropertyFilter.Matches(inner, "title"));
        Assert.False(PropertyFilter.Matches(root.GetChild(nameof(FilterSample.Title))!, "rad"));
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AnEmptySearchShowsEveryProperty(string? token)
    {
        var root = new NodeViewModelContext().RootOf(new FilterSample());

        Assert.All(root.Children, x => Assert.True(PropertyFilter.Matches(x, token)));
    }

    [AvaloniaFact]
    public void TheSearchHidesTheOtherPropertiesAtEveryLevel()
    {
        var (window, propertyView, behavior) = ShowProperties(new FilterSample());

        behavior.FilterToken = "rad";
        Refresh(window);

        Assert.False(Item(propertyView, nameof(FilterSample.Title)).IsVisible);
        Assert.False(Item(propertyView, nameof(FilterSample.Count)).IsVisible);
        var inner = Item(propertyView, nameof(FilterSample.Inner));
        Assert.True(inner.IsVisible);
        // The nested properties are created when their property is expanded: the search hides them then
        inner.IsExpanded = true;
        Refresh(window);
        // Inside the matching property, only the nested properties that match stay, as in WPF
        Assert.True(Item(inner, nameof(FilterInner.Radius)).IsVisible);
        Assert.False(Item(inner, nameof(FilterInner.Label)).IsVisible);

        behavior.FilterToken = "";
        Refresh(window);

        Assert.All(propertyView.Properties, x => Assert.True(x.IsVisible));
        Assert.All(inner.Properties, x => Assert.True(x.IsVisible));
        window.Close();
    }

    [AvaloniaFact]
    public void PropertiesShownAfterASearchFollowIt()
    {
        var (window, propertyView, behavior) = ShowProperties(new FilterSample());
        behavior.FilterToken = "count";
        Refresh(window);

        // Another selection gives the grid new properties
        propertyView.ItemsSource = new NodeViewModelContext().RootOf(new FilterSample()).Children;
        Refresh(window);

        Assert.True(Item(propertyView, nameof(FilterSample.Count)).IsVisible);
        Assert.False(Item(propertyView, nameof(FilterSample.Title)).IsVisible);
        Assert.False(Item(propertyView, nameof(FilterSample.Inner)).IsVisible);
        window.Close();
    }

    private static (Window Window, PropertyView PropertyView, PropertyViewFilteringBehavior Behavior) ShowProperties(object instance)
    {
        DefaultProviders.Register();
        var root = new NodeViewModelContext().RootOf(instance);
        var propertyView = new PropertyView { ItemsSource = root.Children };
        var behavior = new PropertyViewFilteringBehavior();
        Interaction.GetBehaviors(propertyView).Add(behavior);
        var window = new Window { Width = 600, Height = 400 };
        // The theme of the property grid, before the content: a window is a root, its content takes its theme at once
        window.Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Stride.Core.Assets.Editor.Avalonia.Tests/"))
        {
            Source = new Uri("avares://Stride.Core.Assets.Editor.Avalonia/Themes/PropertyViewTheme.axaml"),
        });
        window.Content = propertyView;
        window.Show();
        Refresh(window);
        return (window, propertyView, behavior);
    }

    // The containers of the properties are created by the layout
    private static void Refresh(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static PropertyViewItem Item(PropertyView propertyView, string name)
    {
        return propertyView.Properties.Single(x => ((NodeViewModel)x.DataContext!).Name == name);
    }

    private static PropertyViewItem Item(PropertyViewItem parent, string name)
    {
        return parent.Properties.Single(x => ((NodeViewModel)x.DataContext!).Name == name);
    }
}

public sealed class FilterSample
{
    public string Title { get; set; } = "title";

    public int Count { get; set; }

    public FilterInner Inner { get; set; } = new();
}

public sealed class FilterInner
{
    public float Radius { get; set; } = 0.5f;

    public string Label { get; set; } = "label";
}
