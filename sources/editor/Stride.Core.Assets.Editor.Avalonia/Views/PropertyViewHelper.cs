// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Data;
using Stride.Core.Presentation.Avalonia.Controls;
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

    public static readonly AttachedProperty<Category> TemplateCategoryProperty =
        AvaloniaProperty.RegisterAttached<PropertyViewHelper, TemplateProviderBase, Category>("TemplateCategory", Category.PropertyHeader, false, BindingMode.OneTime);

    public static readonly TemplateProviderSelector HeaderProviders = new();

    public static readonly TemplateProviderSelector EditorProviders = new();

    public static readonly TemplateProviderSelector FooterProviders = new();

    /// <summary>
    /// Get accessor for Attached property <see cref="TemplateCategoryProperty"/>.
    /// </summary>
    public static Category GetTemplateCategory(AvaloniaObject element)
    {
        return element.GetValue(TemplateCategoryProperty);
    }
    
    /// <summary>
    /// Set accessor for Attached property <see cref="TemplateCategoryProperty"/>.
    /// </summary>
    public static void SetTemplateCategory(AvaloniaObject element, Category category)
    {
        element.SetValue(TemplateCategoryProperty, category);
    }
}
