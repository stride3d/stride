// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Input;

using Stride.Core.Presentation.Controls;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.View;

namespace Stride.Core.Assets.Editor.View
{
    /// <summary>
    /// This static class contains helper dependency properties that allows to override some properties of the parent <see cref="PropertyViewItem"/> of a control.
    /// </summary>
    public static class PropertyViewHelper
    {
        public enum Category
        {
            PropertyHeader,
            PropertyFooter,
            PropertyEditor,
        };

        static PropertyViewHelper()
        {
            ToggleNestedPropertiesCommand = new RoutedCommand("ToggleNestedPropertiesCommand", typeof(PropertyViewHelper));
            CommandManager.RegisterClassCommandBinding(typeof(PropertyViewItem), new CommandBinding(ToggleNestedPropertiesCommand, OnToggleNestedProperties));
        }

        public static readonly DependencyProperty IncrementProperty = DependencyProperty.RegisterAttached("Increment", typeof(double?), typeof(PropertyViewHelper), new PropertyMetadata(null, OnIncrementChanged));

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.RegisterAttached("IsExpanded", typeof(bool?), typeof(PropertyViewHelper), new PropertyMetadata(null, OnIsExpandedChanged));

        public static readonly DependencyProperty TemplateCategoryProperty = DependencyProperty.RegisterAttached("TemplateCategory", typeof(Category), typeof(PropertyViewHelper), new PropertyMetadata(Category.PropertyHeader));

        public static RoutedCommand ToggleNestedPropertiesCommand { get; }

        public static readonly TemplateProviderSelector HeaderProviders = new TemplateProviderSelector();

        public static readonly TemplateProviderSelector EditorProviders = new TemplateProviderSelector();

        public static readonly TemplateProviderSelector FooterProviders = new TemplateProviderSelector();

        public static double GetIncrement(DependencyObject target)
        {
            return (double)target.GetValue(IncrementProperty);
        }

        public static void SetIncrement(DependencyObject target, double value)
        {
            target.SetValue(IncrementProperty, value);
        }

        public static Category GetTemplateCategory(DependencyObject target)
        {
            return (Category)target.GetValue(TemplateCategoryProperty);
        }

        public static void SetTemplateCategory(DependencyObject target, Category value)
        {
            target.SetValue(TemplateCategoryProperty, value);
        }

        public static bool GetIsExpanded(DependencyObject target)
        {
            return (bool)target.GetValue(IsExpandedProperty);
        }

        [Obsolete("Use the DisplayAttribute on the properties")]
        public static void SetIsExpanded(DependencyObject target, bool value)
        {
            target.SetValue(IsExpandedProperty, value);
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyProperty property, object newValue)
        {
            if (newValue == null)
                return;

            var target = d as PropertyViewItem ?? d.FindVisualParentOfType<PropertyViewItem>();
            target?.SetCurrentValue(property, newValue);
        }

        private static void OnIncrementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(d, PropertyViewItem.IncrementProperty, e.NewValue);
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnPropertyChanged(d, PropertyViewItem.IsExpandedProperty, e.NewValue);
        }

        private static void OnToggleNestedProperties(object sender, ExecutedRoutedEventArgs e)
        {
            var d = sender as DependencyObject;
            if (d != null)
            {
                var target = sender as PropertyViewItem ?? d.FindVisualParentOfType<PropertyViewItem>();
                if (target != null)
                {
                    var currentValue = true;

                    for (var i = 0; i < target.Items.Count; i++)
                    {
                        var container = (PropertyViewItem)target.ItemContainerGenerator.ContainerFromIndex(i);
                        if (!container.IsExpanded)
                        {
                            currentValue = false;
                            break;
                        }
                    }
                    for (var i = 0; i < target.Items.Count; i++)
                    {
                        var container = (PropertyViewItem)target.ItemContainerGenerator.ContainerFromIndex(i);
                        container.SetCurrentValue(PropertyViewItem.IsExpandedProperty, !currentValue);
                    }
                }
            }
        }
    }
}

