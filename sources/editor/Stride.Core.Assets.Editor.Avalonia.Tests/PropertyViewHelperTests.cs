// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Extensions;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

public sealed class PropertyViewHelperTests
{
    [AvaloniaFact]
    public void AttachedPropertiesForwardToThePropertyViewItem()
    {
        var propertyView = new PropertyView();
        var item = new PropertyViewItem(propertyView);
        PropertyViewHelper.SetIsExpanded(item, true);
        PropertyViewHelper.SetIncrement(item, 0.25);

        var window = new Window { Content = new StackPanel { Children = { propertyView, item } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(item.IsExpanded);
        Assert.Equal(0.25, item.Increment);
        Assert.True(PropertyViewHelper.GetIsExpanded(item));
        Assert.Equal(0.25, PropertyViewHelper.GetIncrement(item));
    }

    [AvaloniaFact]
    public void TemplateCategoryDefaultsToTheHeader()
    {
        var provider = new TypeMatchTemplateProvider { Type = typeof(int), Template = null! };

        Assert.Equal(PropertyViewHelper.Category.PropertyHeader, PropertyViewHelper.GetTemplateCategory(provider));

        PropertyViewHelper.SetTemplateCategory(provider, PropertyViewHelper.Category.PropertyFooter);

        Assert.Equal(PropertyViewHelper.Category.PropertyFooter, PropertyViewHelper.GetTemplateCategory(provider));
    }

    [Fact]
    public void TheThreeSelectorsAreDistinct()
    {
        Assert.NotSame(PropertyViewHelper.HeaderProviders, PropertyViewHelper.EditorProviders);
        Assert.NotSame(PropertyViewHelper.EditorProviders, PropertyViewHelper.FooterProviders);
        Assert.NotSame(PropertyViewHelper.HeaderProviders, PropertyViewHelper.FooterProviders);
    }

    [AvaloniaFact]
    public void TheToggleExpandsTheNestedPropertiesThenCollapsesThem()
    {
        var (window, propertyView) = PropertyViewWindow.Show(new NestedSample());
        var outer = propertyView.Item(nameof(NestedSample.Outer));
        outer.IsExpanded = true;
        PropertyViewWindow.Refresh(window);
        var (first, second) = (outer.Item(nameof(NestedOuter.First)), outer.Item(nameof(NestedOuter.Second)));
        first.IsExpanded = true;

        // One is collapsed: the toggle expands them all
        Assert.True(PropertyViewHelper.ToggleNestedPropertiesCommand.CanExecute(outer));
        PropertyViewHelper.ToggleNestedPropertiesCommand.Execute(outer);
        Assert.True(first.IsExpanded);
        Assert.True(second.IsExpanded);
        // The property itself stays as it is
        Assert.True(outer.IsExpanded);

        // All are expanded: the toggle collapses them, from a control inside the property too
        PropertyViewWindow.Refresh(window);
        var control = outer.GetVisualDescendants().OfType<Control>().First(x => x.FindVisualParentOfType<PropertyViewItem>() == outer);
        PropertyViewHelper.ToggleNestedPropertiesCommand.Execute(control);
        Assert.False(first.IsExpanded);
        Assert.False(second.IsExpanded);
        window.Close();
    }

    [Fact]
    public void TheToggleNeedsAProperty()
    {
        Assert.False(PropertyViewHelper.ToggleNestedPropertiesCommand.CanExecute(null));
        Assert.False(PropertyViewHelper.ToggleNestedPropertiesCommand.CanExecute("not a control"));
    }
}

public sealed class NestedSample
{
    public NestedOuter Outer { get; set; } = new();
}

public sealed class NestedOuter
{
    public FilterInner First { get; set; } = new();

    public FilterInner Second { get; set; } = new();
}
