// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Controls
{
    [TemplatePart(Name = "PART_EditableTextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_ListBox", Type = typeof(ListBox))]
    public class FilteringComboBox : Selector
    {
        /// <summary>
        /// A dependency property used to safely evaluate the value of an item given a path.
        /// </summary>
        private static readonly DependencyProperty InternalValuePathProperty = DependencyProperty.Register("InternalValuePath", typeof(object), typeof(FilteringComboBox));
        /// <summary>
        /// The input text box.
        /// </summary>
        private TextBox editableTextBox;
        /// <summary>
        /// The filtered list box.
        /// </summary>
        private ListBox listBox;
        /// <summary>
        /// Indicates that the selection is being internally cleared and that the drop down should not be opened nor refreshed.
        /// </summary>
        private bool clearing;
        /// <summary>
        /// Indicates that the user clicked in the listbox with the mouse and that the drop down should not be opened.
        /// </summary>
        private bool listBoxClicking;
        /// <summary>
        /// Indicates that the selection is being internally updated and that the text should not be cleared.
        /// </summary>
        private bool updatingSelection;
        /// <summary>
        /// Indicates that the text box is being validated and that the update of the text should not impact the selected item.
        /// </summary>
        private bool validating;

        /// <summary>
        /// Identifies the <see cref="RequireSelectedItemToValidate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RequireSelectedItemToValidateProperty = DependencyProperty.Register("RequireSelectedItemToValidate", typeof(bool), typeof(FilteringComboBox));

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(FilteringComboBox), new FrameworkPropertyMetadata { DefaultUpdateSourceTrigger = UpdateSourceTrigger.Explicit, BindsTwoWayByDefault = true });

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(FilteringComboBox), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, OnIsDropDownOpenChanged));

        /// <summary>
        /// Identifies the <see cref="OpenDropDownOnFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OpenDropDownOnFocusProperty = DependencyProperty.Register("OpenDropDownOnFocus", typeof(bool), typeof(FilteringComboBox));

        /// <summary>
        /// Identifies the <see cref="ClearTextAfterValidation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearTextAfterValidationProperty = DependencyProperty.Register("ClearTextAfterValidation", typeof(bool), typeof(FilteringComboBox));

        /// <summary>
        /// Identifies the <see cref="WatermarkContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkContentProperty = DependencyProperty.Register("WatermarkContent", typeof(object), typeof(FilteringComboBox), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="IsFiltering"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFilteringProperty = DependencyProperty.Register("IsFiltering", typeof(bool), typeof(FilteringComboBox), new FrameworkPropertyMetadata(true, OnIsFilteringChanged));

        /// <summary>
        /// Identifies the <see cref="ItemsToExclude"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsToExcludeProperty = DependencyProperty.Register("ItemsToExclude", typeof(IEnumerable), typeof(FilteringComboBox));

        /// <summary>
        /// Identifies the <see cref="Sort"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SortProperty = DependencyProperty.Register("Sort", typeof(FilteringComboBoxSort), typeof(FilteringComboBox), new FrameworkPropertyMetadata(OnItemsSourceRefresh));

        /// <summary>
        /// Identifies the <see cref="SortMemberPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SortMemberPathProperty = DependencyProperty.Register("SortMemberPath", typeof(string), typeof(FilteringComboBox));

        /// <summary>
        /// Identifies the <see cref="ValidatedValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidatedValueProperty = DependencyProperty.Register("ValidatedValue", typeof(object), typeof(FilteringComboBox));

        /// <summary>
        /// Identifies the <see cref="ValidatedItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidatedItemProperty = DependencyProperty.Register("ValidatedItem", typeof(object), typeof(FilteringComboBox));

        /// <summary>
        /// Identifies the <see cref="ValidateOnLostFocus"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValidateOnLostFocusProperty =
            DependencyProperty.Register(nameof(ValidateOnLostFocus), typeof(bool), typeof(FilteringComboBox), new PropertyMetadata(BooleanBoxes.TrueBox));


        /// <summary>
        /// Raised just before the TextBox changes are validated. This event is cancellable
        /// </summary>
        public static readonly RoutedEvent ValidatingEvent = EventManager.RegisterRoutedEvent("Validating", RoutingStrategy.Bubble, typeof(CancelRoutedEventHandler), typeof(FilteringComboBox));

        /// <summary>
        /// Raised when TextBox changes have been validated.
        /// </summary>
        public static readonly RoutedEvent ValidatedEvent = EventManager.RegisterRoutedEvent("Validated", RoutingStrategy.Bubble, typeof(ValidationRoutedEventHandler<string>), typeof(FilteringComboBox));

        static FilteringComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FilteringComboBox), new FrameworkPropertyMetadata(typeof(FilteringComboBox)));
        }

        public FilteringComboBox()
        {
            IsTextSearchEnabled = false;
        }

        /// <summary>
        /// Gets or sets whether the drop down is open.
        /// </summary>
        public bool IsDropDownOpen { get { return (bool)GetValue(IsDropDownOpenProperty); } set { SetValue(IsDropDownOpenProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether to open the dropdown when the control got the focus.
        /// </summary>
        public bool OpenDropDownOnFocus { get { return (bool)GetValue(OpenDropDownOnFocusProperty); } set { SetValue(OpenDropDownOnFocusProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether the validation will be cancelled if <see cref="Selector.SelectedItem"/> is null.
        /// </summary>
        public bool RequireSelectedItemToValidate { get { return (bool)GetValue(RequireSelectedItemToValidateProperty); } set { SetValue(RequireSelectedItemToValidateProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets the text of this <see cref="FilteringComboBox"/>
        /// </summary>
        public string Text { get { return (string)GetValue(TextProperty); } set { SetValue(TextProperty, value); } }

        /// <summary>
        /// Gets or sets whether to clear the text after the validation.
        /// </summary>
        public bool ClearTextAfterValidation { get { return (bool)GetValue(ClearTextAfterValidationProperty); } set { SetValue(ClearTextAfterValidationProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets the content to display when the TextBox is empty.
        /// </summary>
        public object WatermarkContent { get { return GetValue(WatermarkContentProperty); } set { SetValue(WatermarkContentProperty, value); } }

        /// <summary>
        /// Gets or sets the content to display when the TextBox is empty.
        /// </summary>
        public bool IsFiltering { get { return (bool)GetValue(IsFilteringProperty); } set { SetValue(IsFilteringProperty, value); } }

        [Obsolete]
        public IEnumerable ItemsToExclude { get { return (IEnumerable)GetValue(ItemsToExcludeProperty); } set { SetValue(ItemsToExcludeProperty, value); } }

        /// <summary>
        /// Gets or sets the comparer used to sort items.
        /// </summary>
        public FilteringComboBoxSort Sort { get { return (FilteringComboBoxSort)GetValue(SortProperty); } set { SetValue(SortProperty, value); } }

        /// <summary>
        /// Gets or sets the name of the member to use to sort items.
        /// </summary>
        public string SortMemberPath { get { return (string)GetValue(SortMemberPathProperty); } set { SetValue(SortMemberPathProperty, value); } }

        public object ValidatedValue { get { return GetValue(ValidatedValueProperty); } set { SetValue(ValidatedValueProperty, value); } }

        public object ValidatedItem { get { return GetValue(ValidatedItemProperty); } set { SetValue(ValidatedItemProperty, value); } }

        /// <summary>
        /// Gets or sets whether the validation should happen when the control losts focus.
        /// </summary>
        public bool ValidateOnLostFocus { get { return (bool)GetValue(ValidateOnLostFocusProperty); } set { SetValue(ValidateOnLostFocusProperty, value); } }

        /// <summary>
        /// Raised just before the TextBox changes are validated. This event is cancellable
        /// </summary>
        public event CancelRoutedEventHandler Validating { add { AddHandler(ValidatingEvent, value); } remove { RemoveHandler(ValidatingEvent, value); } }

        /// <summary>
        /// Raised when TextBox changes have been validated.
        /// </summary>
        public event ValidationRoutedEventHandler<string> Validated { add { AddHandler(ValidatedEvent, value); } remove { RemoveHandler(ValidatedEvent, value); } }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            if (newValue != null)
            {
                UpdateCollectionView();
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var filteringComboBox = (FilteringComboBox)d;
            if ((bool)e.NewValue && filteringComboBox.ItemsSource != null)
            {
                filteringComboBox.UpdateCollectionView();
            }
        }

        private static void OnIsFilteringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var filteringComboBox = (FilteringComboBox)d;
            if (filteringComboBox.ItemsSource != null)
            {
                filteringComboBox.UpdateCollectionView();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            editableTextBox = GetTemplateChild("PART_EditableTextBox") as TextBox;
            if (editableTextBox == null)
                throw new InvalidOperationException("A part named 'PART_EditableTextBox' must be present in the ControlTemplate, and must be of type 'Stride.Core.Presentation.Controls.Input.TextBox'.");

            listBox = GetTemplateChild("PART_ListBox") as ListBox;
            if (listBox == null)
                throw new InvalidOperationException("A part named 'PART_ListBox' must be present in the ControlTemplate, and must be of type 'ListBox'.");

            editableTextBox.TextChanged += EditableTextBoxTextChanged;
            editableTextBox.PreviewKeyDown += EditableTextBoxPreviewKeyDown;
            editableTextBox.Validating += EditableTextBoxValidating;
            editableTextBox.Validated += EditableTextBoxValidated;
            editableTextBox.Cancelled += EditableTextBoxCancelled;
            editableTextBox.LostFocus += EditableTextBoxLostFocus;
            listBox.MouseUp += ListBoxMouseUp;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (OpenDropDownOnFocus && !listBoxClicking)
            {
                IsDropDownOpen = true;
            }
            listBoxClicking = false;
        }

        private static void OnItemsSourceRefresh(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var filteringComboBox = (FilteringComboBox)d;
            filteringComboBox.OnItemsSourceChanged(filteringComboBox.ItemsSource, filteringComboBox.ItemsSource);
        }

        private void EditableTextBoxValidating(object sender, CancelRoutedEventArgs e)
        {
            // This may happens somehow when the template is refreshed.
            if (!ReferenceEquals(sender, editableTextBox))
                return;

            // If we require a selected item but there is none, cancel the validation
            BindingExpression expression;
            if (RequireSelectedItemToValidate && SelectedItem == null)
            {
                e.Cancel = true;
                expression = GetBindingExpression(TextProperty);
                expression?.UpdateTarget();
                editableTextBox.Cancel();
                return;
            }

            validating = true;

            // Update the validated properties
            ValidatedValue = SelectedValue;
            ValidatedItem = SelectedItem;

            // If the dropdown is still open and something is selected, use the string from the selected item
            if (SelectedItem != null && IsDropDownOpen)
            {
                var displayValue = ResolveSortMemberValue(SelectedItem);
                editableTextBox.Text = displayValue?.ToString();
                if (editableTextBox.Text != null)
                {
                    editableTextBox.CaretIndex = editableTextBox.Text.Length;
                }
            }

            // Update the source of the text property binding
            expression = GetBindingExpression(TextProperty);
            expression?.UpdateSource();

            // Close the dropdown
            if (IsDropDownOpen)
            {
                IsDropDownOpen = false;
            }

            validating = false;

            var cancelRoutedEventArgs = new CancelRoutedEventArgs(ValidatingEvent);
            RaiseEvent(cancelRoutedEventArgs);
            if (cancelRoutedEventArgs.Cancel)
            {
                e.Cancel = true;
            }
        }

        private void EditableTextBoxValidated(object sender, ValidationRoutedEventArgs<string> e)
        {
            // This may happens somehow when the template is refreshed.
            if (!ReferenceEquals(sender, editableTextBox))
                return;

            var validatedArgs = new RoutedEventArgs(ValidatedEvent);
            RaiseEvent(validatedArgs);

            if (ClearTextAfterValidation)
            {
                clearing = true;
                editableTextBox.Text = string.Empty;
                clearing = false;
            }
        }

        private void EditableTextBoxCancelled(object sender, RoutedEventArgs e)
        {
            // This may happens somehow when the template is refreshed.
            if (!ReferenceEquals(sender, editableTextBox))
                return;

            var expression = GetBindingExpression(TextProperty);
            expression?.UpdateTarget();

            clearing = true;
            IsDropDownOpen = false;
            clearing = false;
        }

        private void EditableTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // This may happens somehow when the template is refreshed.
            if (!ReferenceEquals(sender, editableTextBox))
                return;
            
            clearing = true;
            if (!RequireSelectedItemToValidate)
            {
                updatingSelection = true;
                SelectedItem = null;
                updatingSelection = false;
            }
            if (ValidateOnLostFocus)
            {
                editableTextBox.Validate();
            }
            // Make sure the drop down is closed
            IsDropDownOpen = false;
            clearing = false;
        }

        private void EditableTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ItemsSource == null)
                return;

            updatingSelection = true;
            if (!IsDropDownOpen && !clearing && IsKeyboardFocusWithin)
            {
                // Setting IsDropDownOpen to true will select all the text. We don't want this behavior, so let's save and restore the caret index.
                var index = editableTextBox.CaretIndex;
                IsDropDownOpen = true;
                editableTextBox.CaretIndex = index;
            }
            if (Sort != null)
                Sort.Token = editableTextBox.Text;

            // TODO: this will update the selected index because the collection view is shared. If UpdateSelectionOnValidation is true, this will still modify the SelectedIndex
            UpdateCollectionView();

            var collectionView = CollectionViewSource.GetDefaultView(ItemsSource);
            var listCollectionView = collectionView as ListCollectionView;

            collectionView.Refresh();
            if (!validating)
            {
                if (listCollectionView?.Count > 0 || collectionView.Cast<object>().Any())
                {
                    listBox.SelectedIndex = 0;
                }
            }
            updatingSelection = false;
        }

        private void UpdateCollectionView()
        {
            var collectionView = CollectionViewSource.GetDefaultView(ItemsSource);
            collectionView.Filter = IsFiltering ? (Predicate<object>)InternalFilter : null;
            var listCollectionView = collectionView as ListCollectionView;
            if (listCollectionView != null)
            {
                listCollectionView.CustomSort = Sort;
            }
        }

        private void EditableTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            updatingSelection = true;

            if (e.Key == Key.Escape)
            {
                if (IsDropDownOpen)
                {
                    IsDropDownOpen = false;
                    if (RequireSelectedItemToValidate)
                        editableTextBox.Cancel();
                }
                else
                {
                    editableTextBox.Cancel();
                }
            }

            if (listBox.Items.Count <= 0)
            {
                updatingSelection = false;
                return;
            }

            var stackPanel = listBox.FindVisualChildOfType<VirtualizingStackPanel>();
            switch (e.Key)
            {
                case Key.Escape:
                    if (IsDropDownOpen)
                    {
                        IsDropDownOpen = false;
                        if (RequireSelectedItemToValidate)
                            editableTextBox.Cancel();
                    }
                    else
                    {
                        editableTextBox.Cancel();
                    }
                    break;

                case Key.Up:
                    listBox.SelectedIndex = Math.Max(listBox.SelectedIndex - 1, 0);
                    BringSelectedItemIntoView();
                    break;

                case Key.Down:
                    listBox.SelectedIndex = Math.Min(listBox.SelectedIndex + 1, listBox.Items.Count - 1);
                    BringSelectedItemIntoView();
                    break;

                case Key.PageUp:
                    if (stackPanel != null)
                    {
                        var count = stackPanel.Children.Count;
                        listBox.SelectedIndex = Math.Max(listBox.SelectedIndex - count, 0);
                    }
                    else
                    {
                        listBox.SelectedIndex = 0;
                    }
                    BringSelectedItemIntoView();
                    break;

                case Key.PageDown:
                    if (stackPanel != null)
                    {
                        var count = stackPanel.Children.Count;
                        listBox.SelectedIndex = Math.Min(listBox.SelectedIndex + count, listBox.Items.Count - 1);
                    }
                    else
                    {
                        listBox.SelectedIndex = listBox.Items.Count - 1;
                    }
                    BringSelectedItemIntoView();
                    break;

                case Key.Home:
                    listBox.SelectedIndex = 0;
                    BringSelectedItemIntoView();
                    break;

                case Key.End:
                    listBox.SelectedIndex = listBox.Items.Count - 1;
                    BringSelectedItemIntoView();
                    break;
            }
            updatingSelection = false;
        }

        private void ListBoxMouseUp(object sender, [NotNull] MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && listBox.SelectedIndex > -1)
            {
                // We need to force the validation here
                // The user might have clicked on the list after the drop down was automatically open (see OpenDropDownOnFocus).
                editableTextBox.ForceValidate();
            }
            listBoxClicking = true;
        }

        private void BringSelectedItemIntoView()
        {
            var selectedItem = listBox.SelectedItem;
            if (selectedItem != null)
            {
                listBox.ScrollIntoView(selectedItem);
            }
        }

        private bool InternalFilter(object obj)
        {
            var filter = editableTextBox?.Text.Trim();
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            if (obj == null)
                return false;

            if (ItemsToExclude != null && ItemsToExclude.Cast<object>().Contains(obj))
                return false;

            var value = ResolveSortMemberValue(obj);
            var text = value?.ToString();
            return MatchText(filter, text);
        }

        private static bool MatchText([NotNull] string inputText, string text)
        {
            var tokens = inputText.Split(" \t\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (text.IndexOf(token, StringComparison.InvariantCultureIgnoreCase) < 0 && !token.MatchCamelCase(text))
                    return false;
            }
            return true;
        }

        private object ResolveSortMemberValue(object obj)
        {
            var value = obj;
            try
            {
                SetBinding(InternalValuePathProperty, new Binding(SortMemberPath) { Source = obj });
                value = GetValue(InternalValuePathProperty);
            }
            catch (Exception e)
            {
                e.Ignore();
            }
            finally
            {
                BindingOperations.ClearBinding(this, InternalValuePathProperty);
            }
            return value;
        }
    }
}
