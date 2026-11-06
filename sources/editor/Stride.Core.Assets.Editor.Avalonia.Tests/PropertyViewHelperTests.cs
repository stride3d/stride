// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Avalonia.Controls;

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
}
