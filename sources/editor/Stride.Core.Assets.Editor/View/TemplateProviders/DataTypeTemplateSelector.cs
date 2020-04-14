// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    [ContentProperty("TemplateDefinitions")]
    public class DataTypeTemplateSelector : DataTemplateSelector
    {
        public TemplateDefinitionCollection TemplateDefinitions { get; } = new TemplateDefinitionCollection();

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var uiElement = container as UIElement;
            if (uiElement == null)
            {
                return base.SelectTemplate(item, container);
            }

            var templates = TemplateDefinitions;
            if (templates == null || templates.Count == 0)
            {
                return base.SelectTemplate(item, container);
            }

            var template = templates.FirstOrDefault(t => t.DataType.IsInstanceOfType(item));
            return template?.DataTemplate ?? base.SelectTemplate(item, container);
        }
    }

    public class TemplateDefinitionCollection : Collection<TemplateDefinition> { }

    public class TemplateDefinition : DependencyObject
    {
        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register("DataType", typeof(Type), typeof(TemplateDefinition));
        
        public static readonly DependencyProperty DataTemplateProperty =
           DependencyProperty.Register("DataTemplate", typeof(DataTemplate), typeof(TemplateDefinition));

        public Type DataType
        {
            get { return (Type)GetValue(DataTypeProperty); }
            set { SetValue(DataTypeProperty, value); }
        }

        public DataTemplate DataTemplate
        {
            get { return (DataTemplate)GetValue(DataTemplateProperty); }
            set { SetValue(DataTemplateProperty, value); }
        }
    }
}
