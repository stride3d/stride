// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// Shows the properties of an object in a <see cref="PropertyView"/>, with the theme and the templates of the property grid.
/// </summary>
internal static class PropertyViewWindow
{
    public static (Window Window, PropertyView PropertyView) Show(object instance)
    {
        DefaultProviders.Register();
        var root = new NodeViewModelContext().RootOf(instance);
        var propertyView = new PropertyView { ItemsSource = root.Children };
        var window = new Window { Width = 600, Height = 400 };
        // The theme of the property grid, before the content: a window is a root, its content takes its theme at once
        window.Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://Stride.Core.Assets.Editor.Avalonia.Tests/"))
        {
            Source = new Uri("avares://Stride.Core.Assets.Editor.Avalonia/Themes/PropertyViewTheme.axaml"),
        });
        window.Content = propertyView;
        window.Show();
        Refresh(window);
        return (window, propertyView);
    }

    /// <summary>
    /// Runs the layout, which creates the containers of the properties.
    /// </summary>
    public static void Refresh(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    public static PropertyViewItem Item(this PropertyView propertyView, string name)
    {
        return propertyView.Properties.Single(x => ((NodeViewModel)x.DataContext!).Name == name);
    }

    public static PropertyViewItem Item(this PropertyViewItem parent, string name)
    {
        return parent.Properties.Single(x => ((NodeViewModel)x.DataContext!).Name == name);
    }
}
