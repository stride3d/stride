// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace Stride.Core.Assets.Editor.Avalonia;

public partial class ObjectBrowserUserControl : UserControl
{
    public static readonly StyledProperty<IEnumerable> HierarchyItemsSourceProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IEnumerable>(nameof(HierarchyItemsSource));

    public static readonly StyledProperty<object> SelectedHierarchyItemProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, object>(nameof(SelectedHierarchyItem));

    public static readonly StyledProperty<IDataTemplate> HierarchyItemTemplateProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IDataTemplate>(nameof(HierarchyItemTemplate));

    public static readonly StyledProperty<IStyle> HierarchyItemContainerStyleProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IStyle>(nameof(HierarchyItemContainerStyle));

    public static readonly StyledProperty<IEnumerable> ObjectItemsSourceProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IEnumerable>(nameof(ObjectItemsSource));

    public static readonly StyledProperty<object> SelectedObjectItemProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, object>(nameof(SelectedObjectItem));

    public static readonly StyledProperty<IDataTemplate> ObjectItemTemplateProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IDataTemplate>(nameof(ObjectItemTemplate));

    public static readonly StyledProperty<IDataTemplate> ObjectItemTemplateSelectorProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IDataTemplate>(nameof(ObjectItemTemplateSelector));

    public static readonly StyledProperty<IStyle> ObjectItemContainerStyleProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IStyle>(nameof(ObjectItemContainerStyle));

    public static readonly StyledProperty<IDataTemplate> ObjectDescriptionTemplateProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IDataTemplate>(nameof(ObjectDescriptionTemplate));

    public static readonly StyledProperty<IDataTemplate> ObjectDescriptionTemplateSelectorProperty =
        AvaloniaProperty.Register<ObjectBrowserUserControl, IDataTemplate>(nameof(ObjectDescriptionTemplateSelector));

    public ObjectBrowserUserControl()
    {
        InitializeComponent();
    }

    public IEnumerable HierarchyItemsSource
    {
        get => GetValue(HierarchyItemsSourceProperty);
        set => SetValue(HierarchyItemsSourceProperty, value);
    }

    public object SelectedHierarchyItem
    {
        get => GetValue(SelectedHierarchyItemProperty);
        set => SetValue(SelectedHierarchyItemProperty, value);
    }

    public IDataTemplate HierarchyItemTemplate
    {
        get => GetValue(HierarchyItemTemplateProperty);
        set => SetValue(HierarchyItemTemplateProperty, value);
    }

    public IStyle HierarchyItemContainerStyle
    {
        get => GetValue(HierarchyItemContainerStyleProperty);
        set => SetValue(HierarchyItemContainerStyleProperty, value);
    }

    public IEnumerable ObjectItemsSource
    {
        get => GetValue(ObjectItemsSourceProperty);
        set => SetValue(ObjectItemsSourceProperty, value);
    }

    public object SelectedObjectItem
    {
        get => GetValue(SelectedObjectItemProperty);
        set => SetValue(SelectedObjectItemProperty, value);
    }

    public IDataTemplate ObjectItemTemplate
    {
        get => GetValue(ObjectItemTemplateProperty);
        set => SetValue(ObjectItemTemplateProperty, value);
    }

    public IDataTemplate ObjectItemTemplateSelector
    {
        get => GetValue(ObjectItemTemplateSelectorProperty);
        set => SetValue(ObjectItemTemplateSelectorProperty, value);
    }

    public IStyle ObjectItemContainerStyle
    {
        get => GetValue(ObjectItemContainerStyleProperty);
        set => SetValue(ObjectItemContainerStyleProperty, value);
    }

    public IDataTemplate ObjectDescriptionTemplate
    {
        get => GetValue(ObjectDescriptionTemplateProperty);
        set => SetValue(ObjectDescriptionTemplateProperty, value);
    }

    public IDataTemplate ObjectDescriptionTemplateSelector
    {
        get => GetValue(ObjectDescriptionTemplateSelectorProperty);
        set => SetValue(ObjectDescriptionTemplateSelectorProperty, value);
    }

    // If you have a scroll viewer in your .axaml file:
    private void OnSelectedObjectItemChanged()
    {
        // ScrollViewer?.ScrollToTop(); // Placeholder: hook up actual ScrollViewer reference
    }
}
