// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.View.Controls
{
    public class EditableContentListBox : ListBox
    {
        private ScrollViewer scrollViewer;

        public static readonly DependencyProperty CanEditProperty = DependencyProperty.Register("CanEdit", typeof(bool), typeof(EditableContentListBox), new PropertyMetadata(true));

        public static readonly DependencyProperty EditItemTemplateProperty = DependencyProperty.Register("EditItemTemplate", typeof(DataTemplate), typeof(EditableContentListBox));

        public static readonly DependencyProperty EditItemTemplateSelectorProperty = DependencyProperty.Register("EditItemTemplateSelector", typeof(DataTemplateSelector), typeof(EditableContentListBox));

        public static RoutedCommand BeginEditCommand { get; private set; }

        static EditableContentListBox()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(EditableListBox), new FrameworkPropertyMetadata(typeof(EditableListBox)));
            BeginEditCommand = new RoutedCommand("BeginEditCommand", typeof(EditableContentListBox), new InputGestureCollection(new[] { new KeyGesture(Key.F2) }));
            CommandManager.RegisterClassCommandBinding(typeof(EditableContentListBox), new CommandBinding(BeginEditCommand, OnBeginEditCommand));
        }

        public EditableContentListBox()
        {
            Loaded += OnEditableListBoxLoaded;
        }

        public bool CanEdit { get { return (bool)GetValue(CanEditProperty); } set { SetValue(CanEditProperty, value); } }

        public DataTemplate EditItemTemplate { get { return (DataTemplate)GetValue(EditItemTemplateProperty); } set { SetValue(EditItemTemplateProperty, value); } }

        public DataTemplateSelector EditItemTemplateSelector { get { return (DataTemplateSelector)GetValue(EditItemTemplateSelectorProperty); } set { SetValue(EditItemTemplateSelectorProperty, value); } }


        public void BeginEdit()
        {
            if (!CanEdit)
                return;

            var selectedItem = SelectedItems.Cast<object>().LastOrDefault();

            if (selectedItem == null)
                return;

            ScrollIntoView(selectedItem);

            var selectedContainer = (EditableContentListBoxItem)ItemContainerGenerator.ContainerFromItem(selectedItem);
            if (selectedContainer != null && selectedContainer.CanEdit)
            {
                selectedContainer.IsEditing = true;
            }
        }

        private void OnEditableListBoxLoaded(object sender, RoutedEventArgs e)
        {
            scrollViewer = this.FindVisualChildOfType<ScrollViewer>();

            if (scrollViewer != null)
                scrollViewer.ScrollChanged += ScrollViewerScrollChanged;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is EditableContentListBoxItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new EditableContentListBoxItem(ItemTemplate, ItemTemplateSelector, EditItemTemplate, EditItemTemplateSelector);
        }

        private void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Math.Abs(e.VerticalChange) < 1e-3 && Math.Abs(e.HorizontalChange) < 1e-3)
                return;

            if (!ReferenceEquals(e.OriginalSource, scrollViewer))
            {
                return;
            }

            var selectedItem = SelectedItems
                .Cast<object>()
                .LastOrDefault();

            if (selectedItem == null)
                return;

            var selectedContainer = (EditableContentListBoxItem)ItemContainerGenerator.ContainerFromItem(selectedItem);

            if (selectedContainer == null)
                return;

            selectedContainer.IsEditing = false;
        }

        private static void OnBeginEditCommand(object sender, ExecutedRoutedEventArgs e)
        {
            var listBox = (EditableContentListBox)sender;
            listBox.BeginEdit();
        }
    }

    public class EditableContentListBoxItem : ListBoxItem
    {
        private static readonly MethodInfo NotifyListItemClickedMethod;

        private readonly DataTemplate regularContentTemplate;
        private readonly DataTemplateSelector regularContentTemplateSelector;

        private readonly DataTemplate editContentTemplate;
        private readonly DataTemplateSelector editContentTemplateSelector;

        private bool mouseDown;

        public static readonly DependencyProperty CanEditProperty = DependencyProperty.Register("CanEdit", typeof(bool), typeof(EditableContentListBoxItem), new PropertyMetadata(true));

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(EditableContentListBoxItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsEditingPropertyChanged));

        static EditableContentListBoxItem()
        {
            NotifyListItemClickedMethod = typeof(ListBox).GetMethod("NotifyListItemClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            if (NotifyListItemClickedMethod == null)
                throw new InvalidOperationException("Unable to reach the NotifyListItemClicked internal method from ListBox class.");
        }

        internal EditableContentListBoxItem(
            DataTemplate regularContentTemplate,
            DataTemplateSelector regularContentTemplateSelector,
            DataTemplate editContentTemplate,
            DataTemplateSelector editContentTemplateSelector)
        {
            this.regularContentTemplate = regularContentTemplate;
            this.regularContentTemplateSelector = regularContentTemplateSelector;
            this.editContentTemplate = editContentTemplate;
            this.editContentTemplateSelector = editContentTemplateSelector;
        }

        public bool IsEditing { get { return (bool)GetValue(IsEditingProperty); } set { SetValue(IsEditingProperty, value); } }

        public bool CanEdit { get { return (bool)GetValue(CanEditProperty); } set { SetValue(CanEditProperty, value); } }

        private static void IsEditingPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = (EditableContentListBoxItem)sender;

            bool check;

            if (container.IsEditing)
                check = ApplyTemplate(container, container.editContentTemplate, container.editContentTemplateSelector);
            else
            {
                check = ApplyTemplate(container, container.regularContentTemplate, container.regularContentTemplateSelector);
                if ((bool)e.OldValue)
                {
                    container.Focus();
                }
            }
            Console.WriteLine(check);
        }

        private static bool ApplyTemplate(ContentControl container, DataTemplate dt, DataTemplateSelector dts)
        {
            container.ContentTemplate = dt;
            container.ContentTemplateSelector = dts;
            return container.ApplyTemplate();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Intentionally does nothing to prevent item selection to happen on MouseDown. It will be managed on MouseUp.
            // NOTE: This is ok since ListBoxItem.OnMouseLeftButtonDown only manages selection, and UIElement.OnMouseLeftButtonDown does nothing.
            // NOTE: We still have to keep track of the mouse down event for mouse up to prevent weird behavior
            mouseDown = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (mouseDown)
            {
                var listBox = ItemsControl.ItemsControlFromItemContainer(this) as ListBox;
                if (listBox != null && Focus())
                {
                    // Hackish way to reproduce what ListBoxItem does on MouseDown by invoking an internal method.
                    NotifyListItemClickedMethod.Invoke(listBox, new object[] { this, MouseButton.Left });
                }
            }
            mouseDown = false;
        }
    }
}
