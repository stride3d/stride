// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Point = Avalonia.Point;

namespace Stride.Core.Presentation.Avalonia.Controls;

[TemplatePart("PART_TextBox", typeof(TextBox), IsRequired = true)]
[TemplatePart("PART_IncreaseButton", typeof(RepeatButton))]
[TemplatePart("PART_DecreaseButton", typeof(RepeatButton))]
[PseudoClasses(DragHorizontalPseudoClass, DragVerticalPseudoClass)]
public sealed class NumericTextBox : TemplatedControl
{
    /// <summary>
    /// The distance in pixels the mouse moves to add or remove one <see cref="SmallChange"/> to the value.
    /// </summary>
    public const double DragSpeed = 3;

    // How far the mouse moves with the button down before the press becomes a drag
    private const double MinimumDragDistance = 4;
    private const string DragHorizontalPseudoClass = ":drag-horizontal";
    private const string DragVerticalPseudoClass = ":drag-vertical";

    private bool internalValueSet;
    // The value comes from text the user typed and is not written to the source yet
    private bool hasPendingEdit;
    private bool isSyncingTextAndValueProperties;
    private bool isTextChangedFromUI;
    private bool isUpdatingValueRatio;

    static NumericTextBox()
    {
        DecimalPlacesProperty.Changed.AddClassHandler<NumericTextBox>(OnDecimalPlacesPropertyChanged);
        MaximumProperty.Changed.AddClassHandler<NumericTextBox>(OnMaximumPropertyChanged);
        MinimumProperty.Changed.AddClassHandler<NumericTextBox>(OnMinimumPropertyChanged);
        TextProperty.Changed.AddClassHandler<NumericTextBox, string?>(OnTextPropertyChanged);
        ValueProperty.Changed.AddClassHandler<NumericTextBox, double?>(OnValuePropertyChanged);
        ValueRatioProperty.Changed.AddClassHandler<NumericTextBox, double>(OnValueRatioPropertyChanged);
    }

    public NumericTextBox()
    {
        // The inner text box moves its caret with the arrows: the steps take them first
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        // The inner text box takes the focus on a press: a drag starts before it does
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<NumericTextBox, int>(nameof(DecimalPlaces), -1);

    public static readonly StyledProperty<double> LargeChangeProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(LargeChange), 10.0);

    public static readonly StyledProperty<double> SmallChangeProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(SmallChange), 1.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(Maximum), double.MaxValue, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(Minimum), double.MinValue, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<NumericTextBox, string?>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        TextBox.TextAlignmentProperty.AddOwner<NumericTextBox>();

    public static readonly StyledProperty<double?> ValueProperty =
        AvaloniaProperty.Register<NumericTextBox, double?>(nameof(Value), 0.0, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        TextBox.PlaceholderTextProperty.AddOwner<NumericTextBox>();

    public static readonly StyledProperty<double> ValueRatioProperty =
        AvaloniaProperty.Register<NumericTextBox, double>(nameof(ValueRatio));

    public static readonly StyledProperty<bool> AllowMouseDragProperty =
        AvaloniaProperty.Register<NumericTextBox, bool>(nameof(AllowMouseDrag), true);

    public static readonly StyledProperty<bool> DisplayUpDownButtonsProperty =
        AvaloniaProperty.Register<NumericTextBox, bool>(nameof(DisplayUpDownButtons), true);

    public static readonly StyledProperty<MouseValidationTrigger> MouseValidationTriggerProperty =
        AvaloniaProperty.Register<NumericTextBox, MouseValidationTrigger>(nameof(MouseValidationTrigger), MouseValidationTrigger.OnMouseUp);

    public static readonly RoutedEvent<NumericTextBoxValueChangedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<NumericTextBox, NumericTextBoxValueChangedEventArgs>(nameof(ValueChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RepeatButtonPressedEventArgs> RepeatButtonPressedEvent =
        RoutedEvent.Register<NumericTextBox, RepeatButtonPressedEventArgs>(nameof(RepeatButtonPressed), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RepeatButtonPressedEventArgs> RepeatButtonReleasedEvent =
        RoutedEvent.Register<NumericTextBox, RepeatButtonPressedEventArgs>(nameof(RepeatButtonReleased), RoutingStrategies.Bubble);

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    /// <summary>
    /// Gets or sets the amount added or removed by Page Up, Page Down, or Shift with the Up and Down arrows.
    /// </summary>
    public double LargeChange
    {
        get => GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the amount added or removed by the Up and Down arrows.
    /// </summary>
    public double SmallChange
    {
        get => GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public double? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets where the value is between <see cref="Minimum"/> (0) and <see cref="Maximum"/> (1).
    /// </summary>
    public double ValueRatio
    {
        get => GetValue(ValueRatioProperty);
        set => SetValue(ValueRatioProperty, value);
    }

    /// <summary>
    /// Gets or sets whether dragging the mouse over the box, when it doesn't have the focus, changes the value.
    /// </summary>
    public bool AllowMouseDrag
    {
        get => GetValue(AllowMouseDragProperty);
        set => SetValue(AllowMouseDragProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the box shows the up and down buttons that add or remove <see cref="SmallChange"/>.
    /// </summary>
    public bool DisplayUpDownButtons
    {
        get => GetValue(DisplayUpDownButtonsProperty);
        set => SetValue(DisplayUpDownButtonsProperty, value);
    }

    /// <summary>
    /// Gets or sets when a value changed with the mouse is written to the source.
    /// </summary>
    public MouseValidationTrigger MouseValidationTrigger
    {
        get => GetValue(MouseValidationTriggerProperty);
        set => SetValue(MouseValidationTriggerProperty, value);
    }

    public event EventHandler<NumericTextBoxValueChangedEventArgs>? ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>
    /// Raised when one of the spin buttons is pressed.
    /// </summary>
    public event EventHandler<RepeatButtonPressedEventArgs>? RepeatButtonPressed
    {
        add => AddHandler(RepeatButtonPressedEvent, value);
        remove => RemoveHandler(RepeatButtonPressedEvent, value);
    }

    /// <summary>
    /// Raised when one of the spin buttons is released.
    /// </summary>
    public event EventHandler<RepeatButtonPressedEventArgs>? RepeatButtonReleased
    {
        add => AddHandler(RepeatButtonReleasedEvent, value);
        remove => RemoveHandler(RepeatButtonReleasedEvent, value);
    }

    /// <summary>
    /// Gets the TextBox template part.
    /// </summary>
    private TextBox? TextBox { get; set; }

    /// <summary>
    /// Gets the up button template part.
    /// </summary>
    private RepeatButton? IncreaseButton { get; set; }

    /// <summary>
    /// Gets the down button template part.
    /// </summary>
    private RepeatButton? DecreaseButton { get; set; }

    /// <summary>
    /// Gets the mouse gesture in progress (a press or a drag over the text, or a held spin button), if any.
    /// </summary>
    private MouseGesture? Gesture { get; set; }

    /// <summary>
    /// Gets whether the value can be changed: a read-only box shows its value only.
    /// </summary>
    private bool CanChangeValue => TextBox is not { IsReadOnly: true };

    protected override void OnInitialized()
    {
        if (!internalValueSet)
        {
            SyncTextAndValueProperties(false, null, true);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        // The part's own changes only: a class handler would run for every text box of the application and, being
        // static, would keep every numeric box alive
        if (TextBox is not null)
        {
            TextBox.PropertyChanged -= OnTextBoxPropertyChanged;
        }
        TextBox = e.NameScope.Find<TextBox>("PART_TextBox");
        if (TextBox is not null)
        {
            TextBox.PropertyChanged += OnTextBoxPropertyChanged;
        }

        IncreaseButton = ApplySpinButton(IncreaseButton, e.NameScope.Find<RepeatButton>("PART_IncreaseButton"));
        DecreaseButton = ApplySpinButton(DecreaseButton, e.NameScope.Find<RepeatButton>("PART_DecreaseButton"));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                var commitSuccess = CommitInput();
                e.Handled = !commitSuccess;
                if (commitSuccess)
                    ForgetTyping();
                break;
        }
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        CommitInput(true);
        ForgetTyping();
        base.OnLostFocus(e);
    }

    /// <summary>
    /// Clears the undo history of the text box once its value is written or replaced: Undo then undoes the edit of the value,
    /// not the typing that made it. While the user types, Undo undoes the typing.
    /// </summary>
    private void ForgetTyping()
    {
        if (TextBox is not { IsUndoEnabled: true } textBox)
            return;

        // The text box clears its history when its undo is turned off
        textBox.SetCurrentValue(TextBox.IsUndoEnabledProperty, false);
        textBox.SetCurrentValue(TextBox.IsUndoEnabledProperty, true);
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        var large = e.Key is Key.PageUp or Key.PageDown || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        double? delta = e.Key switch
        {
            Key.Up or Key.PageUp => large ? LargeChange : SmallChange,
            Key.Down or Key.PageDown => -(large ? LargeChange : SmallChange),
            _ => null,
        };
        if (delta is not { } change || e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            return;

        // Increasing an empty value starts from the minimum, decreasing it from the maximum
        var value = (Value ?? (change > 0 ? Minimum : Maximum)) + change;
        SetCurrentValue(ValueProperty, MathUtil.Clamp(value, Minimum, Maximum));
        BindingOperations.GetBindingExpressionBase(this, ValueProperty)?.UpdateSource();
        TextBox?.SelectAll();
        e.Handled = true;
    }

    private bool CommitInput(bool forceTextUpdate = false)
    {
        if (SyncTextAndValueProperties(true, Text, forceTextUpdate))
        {
            // Only a value the user typed: writing back a value that was only shown would change one the source converts
            // (an angle shown in degrees comes back in radians with a float error)
            if (hasPendingEdit)
            {
                hasPendingEdit = false;
                BindingOperations.GetBindingExpressionBase(this, ValueProperty)?.UpdateSource();
            }
            return true;
        }

        return false;
    }

    private double? ConvertTextToValue(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var currentValueText = ConvertValueToText(Value);
        if (Equals(currentValueText, text))
            return Value;

        if (double.TryParse(text, NumberStyles.Any, NumberFormatInfo.CurrentInfo, out var result))
            return MathUtil.Clamp(result, Minimum, Maximum);

        throw new InvalidDataException("Input string was not in a correct format.");

    }

    private string ConvertValueToText(double? value)
    {
        if (!value.HasValue)
            return string.Empty;

        var decimalPlaces = DecimalPlaces;
        var coercedValue = decimalPlaces < 0 ? value.Value : Math.Round(value.Value, decimalPlaces);
        return coercedValue.ToString(CultureInfo.InvariantCulture);
    }

    private void OnTextPropertyChanged()
    {
        if (!IsInitialized)
            return;

        SyncTextAndValueProperties(true, Text);
    }

    private void OnValuePropertyChanged(double? oldValue, double? newValue)
    {
        if (!internalValueSet)
        {
            // A value from the source (or set in code) replaces what the user typed
            hasPendingEdit = false;
            if (IsInitialized)
                SyncTextAndValueProperties(false, null, true);
            ForgetTyping();
        }

        UpdateValueRatio();
        RaiseEvent(new NumericTextBoxValueChangedEventArgs(ValueChangedEvent, oldValue, newValue));
    }

    private bool SyncTextAndValueProperties(bool updateValueFromText, string? text, bool forceTextUpdate = false)
    {
        if (isSyncingTextAndValueProperties)
            return true;

        isSyncingTextAndValueProperties = true;
        var textIsValid = true;
        try
        {
            if (updateValueFromText)
            {
                try
                {
                    var newValue = ConvertTextToValue(text);
                    if (!Equals(newValue, Value))
                    {
                        SetValueInternal(newValue);
                    }
                }
                catch
                {
                    textIsValid = false;
                }
            }

            // Do not touch the ongoing text input from user.
            if (!isTextChangedFromUI)
            {
                if (forceTextUpdate)
                {
                    var newText = ConvertValueToText(Value);
                    if (!Equals(Text, newText))
                    {
                        SetCurrentValue(TextProperty, newText);
                    }
                }
            }
        }
        finally
        {
            isSyncingTextAndValueProperties = false;
        }
        return textIsValid;

        void SetValueInternal(double? value)
        {
            internalValueSet = true;
            try
            {
                SetCurrentValue(ValueProperty, value);
                hasPendingEdit = true;
            }
            finally
            {
                internalValueSet = false;
            }
        }
    }

    private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty)
            TextBox_OnTextPropertyChanged();
    }

    private void TextBox_OnTextPropertyChanged()
    {
        try
        {
            isTextChangedFromUI = true;
            if (TextBox is not null)
            {
                SetCurrentValue(TextProperty, TextBox.Text);
            }
        }
        finally
        {
            isTextChangedFromUI = false;
        }
    }

    private static void OnDecimalPlacesPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs _)
    {
        // The rounding of the displayed value changed
        if (sender.IsInitialized)
        {
            sender.SyncTextAndValueProperties(false, null, true);
        }
    }

    private static void OnMaximumPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs _)
    {
        var needValidation = false;
        if (sender.Minimum > sender.Maximum)
        {
            sender.SetCurrentValue(MinimumProperty, sender.Maximum);
            needValidation = true;
        }
        if (sender.Value > sender.Maximum)
        {
            sender.SetCurrentValue(ValueProperty, sender.Maximum);
            needValidation = true;
        }

        sender.UpdateValueRatio();
        if (needValidation)
        {
            sender.SyncTextAndValueProperties(false, null, true);
        }
    }

    private static void OnMinimumPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs _)
    {
        var needValidation = false;
        if (sender.Maximum < sender.Minimum)
        {
            sender.SetCurrentValue(MaximumProperty, sender.Minimum);
            needValidation = true;
        }
        if (sender.Value < sender.Minimum)
        {
            sender.SetCurrentValue(ValueProperty, sender.Minimum);
            needValidation = true;
        }

        sender.UpdateValueRatio();
        if (needValidation)
        {
            sender.SyncTextAndValueProperties(false, null, true);
        }
    }

    private static void OnTextPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs<string?> e)
    {
        sender.OnTextPropertyChanged();
    }

    private static void OnValuePropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs<double?> e)
    {
        sender.OnValuePropertyChanged(e.OldValue.GetValueOrDefault(), e.NewValue.GetValueOrDefault());
    }

    private static void OnValueRatioPropertyChanged(NumericTextBox sender, AvaloniaPropertyChangedEventArgs<double> e)
    {
        // A ratio set from outside moves the value; the value only shows its ratio
        if (!sender.isUpdatingValueRatio && sender.CanChangeValue)
        {
            sender.SetCurrentValue(ValueProperty, MathUtil.Lerp(sender.Minimum, sender.Maximum, e.NewValue.GetValueOrDefault()));
        }
    }

    private void UpdateValueRatio()
    {
        isUpdatingValueRatio = true;
        try
        {
            SetCurrentValue(ValueRatioProperty, Value is { } value ? MathUtil.InverseLerp(Minimum, Maximum, value) : 0.0);
        }
        finally
        {
            isUpdatingValueRatio = false;
        }
    }

    /// <summary>
    /// Writes the value to the source of the binding, for the changes made with the mouse.
    /// </summary>
    internal void UpdateValueSource()
    {
        BindingOperations.GetBindingExpressionBase(this, ValueProperty)?.UpdateSource();
    }

    /// <summary>
    /// Adds a step to the value, as the arrows and the spin buttons do.
    /// </summary>
    private void Step(double change)
    {
        // Increasing an empty value starts from the minimum, decreasing it from the maximum
        var value = (Value ?? (change > 0 ? Minimum : Maximum)) + change;
        SetCurrentValue(ValueProperty, MathUtil.Clamp(value, Minimum, Maximum));
    }

    private RepeatButton? ApplySpinButton(RepeatButton? oldButton, RepeatButton? newButton)
    {
        if (oldButton is not null)
        {
            oldButton.Click -= OnSpinButtonClick;
            oldButton.PropertyChanged -= OnSpinButtonPropertyChanged;
        }
        if (newButton is not null)
        {
            newButton.Click += OnSpinButtonClick;
            newButton.PropertyChanged += OnSpinButtonPropertyChanged;
        }
        return newButton;
    }

    private void OnSpinButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!CanChangeValue)
            return;

        Step(ReferenceEquals(sender, IncreaseButton) ? SmallChange : -SmallChange);
        TextBox?.SelectAll();
        // A held button writes once, on release, unless every step is written
        if (Gesture is not { Kind: MouseGestureKind.SpinButton } || MouseValidationTrigger == MouseValidationTrigger.OnMouseMove)
        {
            UpdateValueSource();
        }
        e.Handled = true;
    }

    private void OnSpinButtonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Button.IsPressedProperty || sender is not RepeatButton button)
            return;

        var which = ReferenceEquals(button, IncreaseButton) ? NumericTextBoxRepeatButton.IncreaseButton : NumericTextBoxRepeatButton.DecreaseButton;
        if (button.IsPressed)
        {
            Gesture = new MouseGesture(MouseGestureKind.SpinButton, Value, default);
            RaiseEvent(new RepeatButtonPressedEventArgs(RepeatButtonPressedEvent, which));
        }
        else
        {
            var gesture = Gesture;
            Gesture = null;
            // One write for the whole press, so one change to undo
            if (gesture is { Kind: MouseGestureKind.SpinButton } && MouseValidationTrigger == MouseValidationTrigger.OnMouseUp && !Equals(gesture.StartValue, Value))
            {
                UpdateValueSource();
            }
            RaiseEvent(new RepeatButtonPressedEventArgs(RepeatButtonReleasedEvent, which));
        }
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Over the text of a box that doesn't have the focus: the press can become a drag. A box with the focus selects
        // its text with the mouse.
        if (!AllowMouseDrag || !CanChangeValue || TextBox is not { } textBox || textBox.IsKeyboardFocusWithin)
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source is not Visual source || !IsOverText(textBox, source))
            return;

        Gesture = new MouseGesture(MouseGestureKind.Press, Value, e.GetPosition(this));
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (Gesture is not { Kind: MouseGestureKind.Press or MouseGestureKind.Drag } gesture)
            return;

        var position = e.GetPosition(this);
        var offset = position - gesture.LastPosition;
        if (gesture.Kind == MouseGestureKind.Press)
        {
            if (Math.Abs(offset.X) <= MinimumDragDistance && Math.Abs(offset.Y) <= MinimumDragDistance)
                return;

            // The direction of the first move is the direction of the whole drag
            gesture.Kind = MouseGestureKind.Drag;
            gesture.Orientation = Math.Abs(offset.X) >= Math.Abs(offset.Y) ? Orientation.Horizontal : Orientation.Vertical;
            gesture.LastPosition = position;
            // The cursor shows the direction
            PseudoClasses.Set(gesture.Orientation == Orientation.Horizontal ? DragHorizontalPseudoClass : DragVerticalPseudoClass, true);
            TextBox?.SelectAll();
            e.Handled = true;
            return;
        }

        // Right and up increase the value
        gesture.Delta += gesture.Orientation == Orientation.Horizontal ? offset.X : -offset.Y;
        gesture.LastPosition = position;
        var steps = Math.Truncate(gesture.Delta / DragSpeed);
        if (steps != 0)
        {
            gesture.Delta -= steps * DragSpeed;
            SetCurrentValue(ValueProperty, MathUtil.Clamp((Value ?? 0.0) + steps * SmallChange, Minimum, Maximum));
            TextBox?.SelectAll();
            if (MouseValidationTrigger == MouseValidationTrigger.OnMouseMove)
            {
                UpdateValueSource();
            }
        }
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Gesture is not { Kind: MouseGestureKind.Press or MouseGestureKind.Drag } gesture)
            return;

        // Ends the gesture before the capture goes, so that losing the capture doesn't cancel it
        Gesture = null;
        ClearDragPseudoClasses();
        e.Pointer.Capture(null);
        e.Handled = true;
        if (gesture.Kind == MouseGestureKind.Press)
        {
            // A click without a drag edits the text
            TextBox?.Focus(NavigationMethod.Pointer);
            TextBox?.SelectAll();
        }
        else if (MouseValidationTrigger == MouseValidationTrigger.OnMouseUp && !Equals(gesture.StartValue, Value))
        {
            // One write for the whole drag, so one change to undo
            UpdateValueSource();
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (Gesture is not { Kind: MouseGestureKind.Press or MouseGestureKind.Drag } gesture)
            return;

        // The drag was interrupted (another window took the mouse): the value goes back to what the source has
        Gesture = null;
        ClearDragPseudoClasses();
        if (gesture.Kind == MouseGestureKind.Drag && MouseValidationTrigger == MouseValidationTrigger.OnMouseUp)
        {
            SetCurrentValue(ValueProperty, gesture.StartValue);
        }
    }

    private void ClearDragPseudoClasses()
    {
        PseudoClasses.Set(DragHorizontalPseudoClass, false);
        PseudoClasses.Set(DragVerticalPseudoClass, false);
    }

    private static bool IsOverText(TextBox textBox, Visual source)
    {
        // The spin buttons sit in the text box, beside the text
        for (var visual = source; visual is not null && !ReferenceEquals(visual, textBox); visual = visual.GetVisualParent())
        {
            if (visual is RepeatButton)
                return false;
        }
        return ReferenceEquals(source, textBox) || textBox.IsVisualAncestorOf(source);
    }

    private enum MouseGestureKind
    {
        /// <summary>
        /// The mouse button is down over the text, and the mouse hasn't moved enough to drag.
        /// </summary>
        Press,
        /// <summary>
        /// The mouse drags the value.
        /// </summary>
        Drag,
        /// <summary>
        /// A spin button is held.
        /// </summary>
        SpinButton,
    }

    private sealed class MouseGesture(MouseGestureKind kind, double? startValue, Point position)
    {
        public MouseGestureKind Kind { get; set; } = kind;

        /// <summary>
        /// The value when the gesture started: a gesture that ends on it writes nothing.
        /// </summary>
        public double? StartValue { get; } = startValue;

        public Point LastPosition { get; set; } = position;

        public Orientation Orientation { get; set; }

        /// <summary>
        /// The distance moved that didn't make a whole step yet.
        /// </summary>
        public double Delta { get; set; }
    }
}
