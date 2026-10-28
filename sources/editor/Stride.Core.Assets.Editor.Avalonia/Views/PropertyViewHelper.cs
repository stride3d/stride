// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Data;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Extensions;
using Stride.Core.Presentation.Avalonia.Views;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// This static class contains helper dependency properties that allows to override some properties of the parent <see cref="PropertyViewItem"/> of a control.
/// </summary>
public sealed class PropertyViewHelper
{
    public enum Category
    {
        PropertyHeader,
        PropertyFooter,
        PropertyEditor,
    }

    static PropertyViewHelper()
    {
        IncrementProperty.Changed.AddClassHandler<StyledElement, double>(OnIncrementChanged);
        IsExpandedProperty.Changed.AddClassHandler<StyledElement, bool>(OnIsExpandedChanged);
    }

    public static readonly AttachedProperty<double> IncrementProperty =
        AvaloniaProperty.RegisterAttached<PropertyViewHelper, TemplateProviderBase, double>("Increment", double.Epsilon);

    public static readonly AttachedProperty<bool> IsExpandedProperty =
        AvaloniaProperty.RegisterAttached<PropertyViewHelper, TemplateProviderBase, bool>("IsExpanded");

    public static readonly AttachedProperty<Category> TemplateCategoryProperty =
        AvaloniaProperty.RegisterAttached<PropertyViewHelper, TemplateProviderBase, Category>("TemplateCategory", Category.PropertyHeader, false, BindingMode.OneTime);

    public static readonly PropertyTemplateProviderSelector HeaderProviders = new();

    public static readonly PropertyTemplateProviderSelector EditorProviders = new();

    public static readonly PropertyTemplateProviderSelector FooterProviders = new();

    /// <summary>
    /// Get accessor for Attached property <see cref="IncrementProperty"/>.
    /// </summary>
    public static double GetIncrement(AvaloniaObject target)
    {
        return target.GetValue(IncrementProperty);
    }

    /// <summary>
    /// Set accessor for Attached property <see cref="IncrementProperty"/>.
    /// </summary>
    public static void SetIncrement(AvaloniaObject target, double value)
    {
        target.SetValue(IncrementProperty, value);
    }

    /// <summary>
    /// Get accessor for Attached property <see cref="IsExpandedProperty"/>.
    /// </summary>
    public static bool GetIsExpanded(AvaloniaObject target)
    {
        return target.GetValue(IsExpandedProperty);
    }

    /// <summary>
    /// Set accessor for Attached property <see cref="IsExpandedProperty"/>.
    /// </summary>
    [Obsolete("Use the DisplayAttribute on the properties")]
    public static void SetIsExpanded(AvaloniaObject target, bool value)
    {
        target.SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Get accessor for Attached property <see cref="TemplateCategoryProperty"/>.
    /// </summary>
    public static Category GetTemplateCategory(AvaloniaObject target)
    {
        return target.GetValue(TemplateCategoryProperty);
    }

    /// <summary>
    /// Set accessor for Attached property <see cref="TemplateCategoryProperty"/>.
    /// </summary>
    public static void SetTemplateCategory(AvaloniaObject target, Category category)
    {
        target.SetValue(TemplateCategoryProperty, category);
    }

    private static void OnIncrementChanged(StyledElement d, AvaloniaPropertyChangedEventArgs<double> e)
    {
        OnPropertyChanged(d, PropertyViewItem.IncrementProperty, e.NewValue.Value);
    }

    private static void OnIsExpandedChanged(StyledElement d, AvaloniaPropertyChangedEventArgs<bool> e)
    {
        OnPropertyChanged(d, ExpandableItemsControl.IsExpandedProperty, e.NewValue.Value);
    }

    private static void OnPropertyChanged<T>(StyledElement target, StyledProperty<T> property, T newValue)
    {
        if (newValue is null)
            return;

        if (target.IsInitialized)
        {
            SetCurrentValue(target, property, newValue);
            return;
        }

        target.Initialized += OnInitialized;
        return;

        void OnInitialized(object? sender, EventArgs e)
        {
            target.Initialized -= OnInitialized;
            SetCurrentValue(target, property, newValue);
        }

        static void SetCurrentValue(StyledElement target, StyledProperty<T> property, T newValue)
        {
            var item = target as PropertyViewItem ?? target.FindVisualParentOfType<PropertyViewItem>();
            item?.SetCurrentValue(property, newValue);
        }
    }
}
