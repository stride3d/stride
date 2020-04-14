// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.Views
{
    /// <summary>
    /// Interaction logic for ObjectBrowserUserControl.xaml
    /// </summary>
    public partial class ObjectBrowserUserControl
    {
        public static readonly DependencyProperty HierarchyItemsSourceProperty = DependencyProperty.Register("HierarchyItemsSource", typeof(IEnumerable), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty SelectedHierarchyItemProperty = DependencyProperty.Register("SelectedHierarchyItem", typeof(object), typeof(ObjectBrowserUserControl), new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        public static readonly DependencyProperty HierarchyItemTemplateProperty = DependencyProperty.Register("HierarchyItemTemplate", typeof(DataTemplate), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty HierarchyItemContainerStyleProperty = DependencyProperty.Register("HierarchyItemContainerStyle", typeof(Style), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty ObjectItemsSourceProperty = DependencyProperty.Register("ObjectItemsSource", typeof(IEnumerable), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty SelectedObjectItemProperty = DependencyProperty.Register("SelectedObjectItem", typeof(object), typeof(ObjectBrowserUserControl), new FrameworkPropertyMetadata { BindsTwoWayByDefault = true, DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

        public static readonly DependencyProperty ObjectItemTemplateProperty = DependencyProperty.Register("ObjectItemTemplate", typeof(DataTemplate), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty ObjectItemTemplateSelectorProperty = DependencyProperty.Register("ObjectItemTemplateSelector", typeof(DataTemplateSelector), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty ObjectItemContainerStyleProperty = DependencyProperty.Register("ObjectItemContainerStyle", typeof(Style), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty ObjectDescriptionTemplateProperty = DependencyProperty.Register("ObjectDescriptionTemplate", typeof(DataTemplate), typeof(ObjectBrowserUserControl));

        public static readonly DependencyProperty ObjectDescriptionTemplateSelectorProperty = DependencyProperty.Register("ObjectDescriptionTemplateSelector", typeof(DataTemplateSelector), typeof(ObjectBrowserUserControl));

        public ObjectBrowserUserControl()
        {
            InitializeComponent();
        }

        public IEnumerable HierarchyItemsSource { get { return (IEnumerable)GetValue(HierarchyItemsSourceProperty); } set { SetValue(HierarchyItemsSourceProperty, value); } }

        public object SelectedHierarchyItem { get { return GetValue(SelectedHierarchyItemProperty); } set { SetValue(SelectedHierarchyItemProperty, value); } }

        public DataTemplate HierarchyItemTemplate { get { return (DataTemplate)GetValue(HierarchyItemTemplateProperty); } set { SetValue(HierarchyItemTemplateProperty, value); } }

        public Style HierarchyItemContainerStyle { get { return (Style)GetValue(HierarchyItemContainerStyleProperty); } set { SetValue(HierarchyItemContainerStyleProperty, value); } }

        public IEnumerable ObjectItemsSource { get { return (IEnumerable)GetValue(ObjectItemsSourceProperty); } set { SetValue(ObjectItemsSourceProperty, value); } }

        public object SelectedObjectItem { get { return GetValue(SelectedObjectItemProperty); } set { SetValue(SelectedObjectItemProperty, value); } }

        public DataTemplate ObjectItemTemplate { get { return (DataTemplate)GetValue(ObjectItemTemplateProperty); } set { SetValue(ObjectItemTemplateProperty, value); } }

        public DataTemplate ObjectItemTemplateSelector { get { return (DataTemplate)GetValue(ObjectItemTemplateSelectorProperty); } set { SetValue(ObjectItemTemplateSelectorProperty, value); } }

        public Style ObjectItemContainerStyle { get { return (Style)GetValue(ObjectItemContainerStyleProperty); } set { SetValue(ObjectItemContainerStyleProperty, value); } }

        public DataTemplate ObjectDescriptionTemplate { get { return (DataTemplate)GetValue(ObjectDescriptionTemplateProperty); } set { SetValue(ObjectDescriptionTemplateProperty, value); } }
        
        public DataTemplateSelector ObjectDescriptionTemplateSelector { get { return (DataTemplateSelector)GetValue(ObjectDescriptionTemplateSelectorProperty); } set { SetValue(ObjectDescriptionTemplateSelectorProperty, value); } }

        private void SelectedObjectUpdated(object sender, DataTransferEventArgs e)
        {
            DescriptionScrollViewer.ScrollToTop();
        }
    }
}
