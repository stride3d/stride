// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Controls;

[TemplatePart(EditableTextBoxPartName, typeof(TextBox), IsRequired = true)]
[TemplatePart(ListBoxPartName, typeof(ListBox), IsRequired = true)]
public class SearchComboBox : SelectingItemsControl
{
    /// <summary>
    /// The name of the part for the <see cref="TextBox"/>.
    /// </summary>
    private const string EditableTextBoxPartName = "PART_EditableTextBox";

    /// <summary>
    /// The name of the part for the <see cref="ListBox"/>.
    /// </summary>
    private const string ListBoxPartName = "PART_ListBox";

    /// <summary>
    /// The input text box.
    /// </summary>
    private TextBox? editableTextBox;
    /// <summary>
    /// The suggestion list box.
    /// </summary>
    private ListBox? listBox;

    /// <summary>
    /// Indicates that the selection is being internally cleared and that the drop-down should not be opened nor refreshed.
    /// </summary>
    private bool clearing;
    /// <summary>
    /// Indicates that the user clicked on the listbox so the dropdown should not be force-closed.
    /// </summary>
    private bool listBoxClicking;

    /// <summary>
    /// Identifies the <see cref="Command"/> styled property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<SearchComboBox, ICommand?>(nameof(Command));

    /// <summary>
    /// Identifies the <see cref="AlternativeCommand"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> AlternativeCommandProperty =
        AvaloniaProperty.Register<SearchComboBox, ICommand?>(nameof(AlternativeCommand));

    /// <summary>
    /// Identifies the <see cref="AlternativeModifiers"/> styled property.
    /// </summary>
    public static readonly StyledProperty<KeyModifiers> AlternativeModifiersProperty =
        AvaloniaProperty.Register<SearchComboBox, KeyModifiers>(nameof(AlternativeModifiers), KeyModifiers.Shift);

    /// <summary>
    /// Identifies the <see cref="ClearTextAfterSelection"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> ClearTextAfterSelectionProperty =
        AvaloniaProperty.Register<SearchComboBox, bool>(nameof(ClearTextAfterSelection));

    /// <summary>
    /// Identifies the <see cref="IsAlternative"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> IsAlternativeProperty =
        AvaloniaProperty.Register<SearchComboBox, bool>(nameof(IsAlternative));

    /// <summary>
    /// Identifies the <see cref="IsDropDownOpen"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<SearchComboBox, bool>(nameof(IsDropDownOpen));

    /// <summary>
    /// Identifies the <see cref="OpenDropDownOnFocus"/> styled property.
    /// </summary>
    public static readonly StyledProperty<bool> OpenDropDownOnFocusProperty =
        AvaloniaProperty.Register<SearchComboBox, bool>(nameof(OpenDropDownOnFocus));

    /// <summary>
    /// Identifies the <see cref="SearchText"/> styled property.
    /// </summary>
    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<SearchComboBox, string?>(nameof(SearchText), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <see cref="Watermark"/> styled property.
    /// </summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        TextBox.WatermarkProperty.AddOwner<SearchComboBox>();

    /// <summary>
    /// Gets or sets the command invoked on selection when <see cref="AlternativeModifiers"/> are active.
    /// The command parameter is the current <see cref="SelectingItemsControl.SelectedItem"/>.
    /// </summary>
    public ICommand? AlternativeCommand
    {
        get => GetValue(AlternativeCommandProperty);
        set => SetValue(AlternativeCommandProperty, value);
    }

    /// <summary>Gets or sets the modifier keys that activate the alternative command.</summary>
    public KeyModifiers AlternativeModifiers
    {
        get => GetValue(AlternativeModifiersProperty);
        set => SetValue(AlternativeModifiersProperty, value);
    }

    /// <summary>Gets or sets whether to clear the search text after a selection is committed.</summary>
    public bool ClearTextAfterSelection
    {
        get => GetValue(ClearTextAfterSelectionProperty);
        set => SetValue(ClearTextAfterSelectionProperty, value);
    }

    /// <summary>
    /// Gets or sets the command invoked once a selection has been committed.
    /// The command parameter is the current <see cref="SelectingItemsControl.SelectedItem"/>.
    /// </summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Gets or sets whether the alternative modifier is currently active.</summary>
    public bool IsAlternative
    {
        get => GetValue(IsAlternativeProperty);
        set => SetValue(IsAlternativeProperty, value);
    }

    /// <summary>Gets or sets whether the suggestion dropdown is open.</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Gets or sets whether to open the dropdown when the control receives focus.</summary>
    public bool OpenDropDownOnFocus
    {
        get => GetValue(OpenDropDownOnFocusProperty);
        set => SetValue(OpenDropDownOnFocusProperty, value);
    }

    /// <summary>Gets or sets the current search text typed by the user.</summary>
    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <summary>Gets or sets the placeholder text displayed when the search box is empty.</summary>
    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (editableTextBox is not null)
        {
            editableTextBox.LostFocus -= EditableTextBoxLostFocus;
            editableTextBox.KeyDown -= EditableTextBoxKeyDown;
            editableTextBox.KeyUp -= EditableTextBoxKeyUp;
            editableTextBox.TextChanged -= EditableTextBoxTextChanged;
        }

        if (listBox is not null)
        {
            listBox.RemoveHandler(PointerPressedEvent, ListBoxPointerPressed);
            listBox.RemoveHandler(PointerReleasedEvent, ListBoxPointerReleased);
        }

        editableTextBox = e.NameScope.Find<TextBox>(EditableTextBoxPartName)
            ?? throw new InvalidOperationException($"A part named '{EditableTextBoxPartName}' must be present in the ControlTemplate.");

        listBox = e.NameScope.Find<ListBox>(ListBoxPartName)
            ?? throw new InvalidOperationException($"A part named '{ListBoxPartName}' must be present in the ControlTemplate.");

        editableTextBox.LostFocus += EditableTextBoxLostFocus;
        editableTextBox.KeyDown += EditableTextBoxKeyDown;
        editableTextBox.KeyUp += EditableTextBoxKeyUp;
        editableTextBox.TextChanged += EditableTextBoxTextChanged;
        listBox.AddHandler(PointerPressedEvent, ListBoxPointerPressed, RoutingStrategies.Tunnel);
        listBox.AddHandler(PointerReleasedEvent, ListBoxPointerReleased, RoutingStrategies.Tunnel);
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        if (OpenDropDownOnFocus && !listBoxClicking)
        {
            IsDropDownOpen = true;
        }
        listBoxClicking = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IsAlternativeModifier(e.Key))
        {
            IsAlternative = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (IsAlternativeModifier(e.Key))
        {
            IsAlternative = false;
        }
    }
    
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        // The user probably clicked (MouseDown) somewhere on our dropdown listbox, so we won't clear to be able to
        // get the pointer released event.
        if (listBox?.IsKeyboardFocusWithin == true)
            return;

        Clear();
    }

    private void EditableTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        // This may happen somehow when the template is refreshed or if the list box is actively being clicked.
        if (!ReferenceEquals(sender, editableTextBox) || listBoxClicking)
            return;

        Clear();
    }

    private void EditableTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Clear();
            e.Handled = true;
            return;
        }

        if (listBox is null || listBox.ItemCount <= 0)
            return;

        switch (e.Key)
        {
            case Key.Up:
                listBox.SelectedIndex = Math.Max(listBox.SelectedIndex - 1, 0);
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.Down:
                listBox.SelectedIndex = Math.Min(listBox.SelectedIndex + 1, listBox.ItemCount - 1);
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.PageUp:
                listBox.SelectedIndex = Math.Max(listBox.SelectedIndex - 10, 0);
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.PageDown:
                listBox.SelectedIndex = Math.Min(listBox.SelectedIndex + 10, listBox.ItemCount - 1);
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.Home:
                listBox.SelectedIndex = 0;
                BringSelectedItemIntoView();
                e.Handled = true;
                break;

            case Key.End:
                listBox.SelectedIndex = listBox.ItemCount - 1;
                BringSelectedItemIntoView();
                e.Handled = true;
                break;
        }
    }

    private void EditableTextBoxKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (listBox is not null && listBox.SelectedItem is null && listBox.ItemCount > 0)
            {
                listBox.SelectedIndex = 0;
            }
            ValidateSelection();
            if (ClearTextAfterSelection)
            {
                Clear();
            }
        }
    }

    private void EditableTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        SetCurrentValue(SearchTextProperty, editableTextBox?.Text);

        if (clearing)
        {
            return;
        }

        IsDropDownOpen = editableTextBox?.IsFocused is true && ItemCount > 0;
    }

    private void ListBoxPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        listBoxClicking = true;
    }

    private void ListBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        ValidateSelection();
        if (ClearTextAfterSelection)
        {
            Clear();
        }
        listBoxClicking = false;
    }

    private void BringSelectedItemIntoView()
    {
        if (listBox?.SelectedItem is not null)
        {
            listBox.ScrollIntoView(listBox.SelectedIndex);
        }
    }

    private void Clear()
    {
        clearing = true;
        if (editableTextBox is not null)
            editableTextBox.Text = string.Empty;
        if (listBox is not null)
            listBox.SelectedItem = null;
        IsDropDownOpen = false;
        clearing = false;
    }

    private bool IsAlternativeModifier(Key key)
    {
        return AlternativeModifiers switch
        {
            KeyModifiers.None => false,
            KeyModifiers.Alt => key == Key.LeftAlt || key == Key.RightAlt,
            KeyModifiers.Control => key == Key.LeftCtrl || key == Key.RightCtrl,
            KeyModifiers.Shift => key == Key.LeftShift || key == Key.RightShift,
            KeyModifiers.Meta => key == Key.LWin || key == Key.RWin,
            _ => false,
        };
    }

    private void ValidateSelection()
    {
        if (listBox is null)
            return;

        // Commit the internal listbox selection to the outer control
        SetCurrentValue(SelectedItemProperty, listBox.SelectedItem);
        BindingOperations.GetBindingExpressionBase(this, SelectedItemProperty)?.UpdateSource();
        BindingOperations.GetBindingExpressionBase(this, SelectedIndexProperty)?.UpdateSource();

        var commandParameter = listBox.SelectedItem;
        if (IsAlternative && AlternativeCommand?.CanExecute(commandParameter) == true)
        {
            AlternativeCommand.Execute(commandParameter);
        }
        else if (Command?.CanExecute(commandParameter) == true)
        {
            Command.Execute(commandParameter);
        }
    }
}
