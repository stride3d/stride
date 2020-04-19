// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.Controls
{
    /// <summary>
    /// An enum describing when the related <see cref="NumericTextBox"/> should be validated, when the user uses the mouse to change its value.
    /// </summary>
    public enum MouseValidationTrigger
    {
        /// <summary>
        /// The validation occurs every time the mouse moves.
        /// </summary>
        OnMouseMove,
        /// <summary>
        /// The validation occurs when the mouse button is released.
        /// </summary>
        OnMouseUp,
    }

    public class RepeatButtonPressedRoutedEventArgs : RoutedEventArgs
    {
        public RepeatButtonPressedRoutedEventArgs(NumericTextBox.RepeatButtons button, RoutedEvent routedEvent)
            : base(routedEvent)
        {
            Button = button;
        }

        public NumericTextBox.RepeatButtons Button { get; private set; }
    }

    /// <summary>
    /// A specialization of the <see cref="TextBoxBase"/> control that can be used for numeric values.
    /// It contains a <see cref="Value"/> property that is updated on validation.
    /// </summary>
    /// PART_IncreaseButton") as RepeatButton;
    [TemplatePart(Name = "PART_IncreaseButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_DecreaseButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_ContentHost", Type = typeof(ScrollViewer))]
    public class NumericTextBox : TextBoxBase
    {
        public enum RepeatButtons
        {
            IncreaseButton,
            DecreaseButton,
        }

        private RepeatButton increaseButton;
        private RepeatButton decreaseButton;
        // FIXME: turn back private
        internal ScrollViewer contentHost;
        private bool updatingValue;

        /// <summary>
        /// The amount of pixel to move the mouse in order to add/remove a <see cref="SmallChange"/> to the current <see cref="Value"/>.
        /// </summary>
        public static readonly double DragSpeed = 3;

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double?), typeof(NumericTextBox), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged, null, true, UpdateSourceTrigger.Explicit));

        /// <summary>
        /// Identifies the <see cref="DecimalPlaces"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumericTextBox), new FrameworkPropertyMetadata(-1, OnDecimalPlacesPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericTextBox), new FrameworkPropertyMetadata(double.MinValue, OnMinimumPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericTextBox), new FrameworkPropertyMetadata(double.MaxValue, OnMaximumPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ValueRatio"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueRatioProperty = DependencyProperty.Register(nameof(ValueRatio), typeof(double), typeof(NumericTextBox), new PropertyMetadata(default(double), ValueRatioChanged));

        /// <summary>
        /// Identifies the <see cref="LargeChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LargeChangeProperty = DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(NumericTextBox), new PropertyMetadata(10.0));

        /// <summary>
        /// Identifies the <see cref="SmallChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(NumericTextBox), new PropertyMetadata(1.0));

        /// <summary>
        /// Identifies the <see cref="DisplayUpDownButtons"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayUpDownButtonsProperty = DependencyProperty.Register(nameof(DisplayUpDownButtons), typeof(bool), typeof(NumericTextBox), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="AllowMouseDrag"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowMouseDragProperty = DependencyProperty.Register(nameof(AllowMouseDrag), typeof(bool), typeof(NumericTextBox), new PropertyMetadata(BooleanBoxes.TrueBox));

        /// <summary>
        /// Identifies the <see cref="MouseValidationTrigger"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MouseValidationTriggerProperty = DependencyProperty.Register(nameof(MouseValidationTrigger), typeof(MouseValidationTrigger), typeof(NumericTextBox), new PropertyMetadata(MouseValidationTrigger.OnMouseUp));

        /// <summary>
        /// Raised when the <see cref="Value"/> property has changed.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(NumericTextBox));

        /// <summary>
        /// Raised when one of the repeat button is pressed.
        /// </summary>
        public static readonly RoutedEvent RepeatButtonPressedEvent = EventManager.RegisterRoutedEvent("RepeatButtonPressed", RoutingStrategy.Bubble, typeof(EventHandler<RepeatButtonPressedRoutedEventArgs>), typeof(NumericTextBox));

        /// <summary>
        /// Raised when one of the repeat button is released.
        /// </summary>
        public static readonly RoutedEvent RepeatButtonReleasedEvent = EventManager.RegisterRoutedEvent("RepeatButtonReleased", RoutingStrategy.Bubble, typeof(EventHandler<RepeatButtonPressedRoutedEventArgs>), typeof(NumericTextBox));

        /// <summary>
        /// Increases the current value with the value of the <see cref="LargeChange"/> property.
        /// </summary>
        public static RoutedCommand LargeIncreaseCommand { get; }

        /// <summary>
        /// Increases the current value with the value of the <see cref="SmallChange"/> property.
        /// </summary>
        public static RoutedCommand SmallIncreaseCommand { get; }

        /// <summary>
        /// Decreases the current value with the value of the <see cref="LargeChange"/> property.
        /// </summary>
        public static RoutedCommand LargeDecreaseCommand { get; }

        /// <summary>
        /// Decreases the current value with the value of the <see cref="SmallChange"/> property.
        /// </summary>
        public static RoutedCommand SmallDecreaseCommand { get; }

        /// <summary>
        /// Resets the current value to zero.
        /// </summary>
        public static RoutedCommand ResetValueCommand { get; }

        static NumericTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(typeof(NumericTextBox)));
            HorizontalScrollBarVisibilityProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(ScrollBarVisibility.Hidden, OnForbiddenPropertyChanged));
            VerticalScrollBarVisibilityProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(ScrollBarVisibility.Hidden, OnForbiddenPropertyChanged));
            AcceptsReturnProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, OnForbiddenPropertyChanged));
            AcceptsTabProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, OnForbiddenPropertyChanged));

            // Since the NumericTextBox is not focusable itself, we have to bind the commands to the inner text box of the control.
            // The handlers will then find the parent that is a NumericTextBox and process the command on this control if it is found.
            LargeIncreaseCommand = new RoutedCommand("LargeIncreaseCommand", typeof(System.Windows.Controls.TextBox));
            CommandManager.RegisterClassCommandBinding(typeof(System.Windows.Controls.TextBox), new CommandBinding(LargeIncreaseCommand, OnLargeIncreaseCommand));
            CommandManager.RegisterClassInputBinding(typeof(System.Windows.Controls.TextBox), new InputBinding(LargeIncreaseCommand, new KeyGesture(Key.PageUp)));
            CommandManager.RegisterClassInputBinding(typeof(System.Windows.Controls.TextBox), new InputBinding(LargeIncreaseCommand, new KeyGesture(Key.Up, ModifierKeys.Shift)));

            LargeDecreaseCommand = new RoutedCommand("LargeDecreaseCommand", typeof(System.Windows.Controls.TextBox));
            CommandManager.RegisterClassCommandBinding(typeof(System.Windows.Controls.TextBox), new CommandBinding(LargeDecreaseCommand, OnLargeDecreaseCommand));
            CommandManager.RegisterClassInputBinding(typeof(System.Windows.Controls.TextBox), new InputBinding(LargeDecreaseCommand, new KeyGesture(Key.PageDown)));
            CommandManager.RegisterClassInputBinding(typeof(System.Windows.Controls.TextBox), new InputBinding(LargeDecreaseCommand, new KeyGesture(Key.Down, ModifierKeys.Shift)));

            SmallIncreaseCommand = new RoutedCommand("SmallIncreaseCommand", typeof(System.Windows.Controls.TextBox));
            CommandManager.RegisterClassCommandBinding(typeof(System.Windows.Controls.TextBox), new CommandBinding(SmallIncreaseCommand, OnSmallIncreaseCommand));
            CommandManager.RegisterClassInputBinding(typeof(System.Windows.Controls.TextBox), new InputBinding(SmallIncreaseCommand, new KeyGesture(Key.Up)));

            SmallDecreaseCommand = new RoutedCommand("SmallDecreaseCommand", typeof(System.Windows.Controls.TextBox));
            CommandManager.RegisterClassCommandBinding(typeof(System.Windows.Controls.TextBox), new CommandBinding(SmallDecreaseCommand, OnSmallDecreaseCommand));
            CommandManager.RegisterClassInputBinding(typeof(System.Windows.Controls.TextBox), new InputBinding(SmallDecreaseCommand, new KeyGesture(Key.Down)));

            ResetValueCommand = new RoutedCommand("ResetValueCommand", typeof(System.Windows.Controls.TextBox));
            CommandManager.RegisterClassCommandBinding(typeof(System.Windows.Controls.TextBox), new CommandBinding(ResetValueCommand, OnResetValueCommand));
        }

        /// <summary>
        /// Gets or sets the current value of the <see cref="NumericTextBox"/>.
        /// </summary>
        public double? Value { get { return (double?)GetValue(ValueProperty); } set { SetValue(ValueProperty, value); } }

        /// <summary>
        /// Gets or sets the number of decimal places displayed in the <see cref="NumericTextBox"/>.
        /// </summary>
        public int DecimalPlaces { get { return (int)GetValue(DecimalPlacesProperty); } set { SetValue(DecimalPlacesProperty, value); } }

        /// <summary>
        /// Gets or sets the minimum value that can be set on the <see cref="Value"/> property.
        /// </summary>
        public double Minimum { get { return (double)GetValue(MinimumProperty); } set { SetValue(MinimumProperty, value); } }

        /// <summary>
        /// Gets or sets the maximum value that can be set on the <see cref="Value"/> property.
        /// </summary>
        public double Maximum { get { return (double)GetValue(MaximumProperty); } set { SetValue(MaximumProperty, value); } }

        /// <summary>
        /// Gets or sets the ratio of the <see cref="NumericTextBox.Value"/> between <see cref="NumericTextBox.Minimum"/> (0.0) and
        /// <see cref="NumericTextBox.Maximum"/> (1.0).
        /// </summary>
        public double ValueRatio { get { return (double)GetValue(ValueRatioProperty); } set { SetValue(ValueRatioProperty, value); } }

        /// <summary>
        /// Gets or sets the value to be added to or substracted from the <see cref="NumericTextBox.Value"/>.
        /// </summary>
        public double LargeChange { get { return (double)GetValue(LargeChangeProperty); } set { SetValue(LargeChangeProperty, value); } }

        /// <summary>
        /// Gets or sets the value to be added to or substracted from the <see cref="NumericTextBox.Value"/>.
        /// </summary>
        public double SmallChange { get { return (double)GetValue(SmallChangeProperty); } set { SetValue(SmallChangeProperty, value); } }

        /// <summary>
        /// Gets or sets whether to display Up and Down buttons on the side of the <see cref="NumericTextBox"/>.
        /// </summary>
        public bool DisplayUpDownButtons { get { return (bool)GetValue(DisplayUpDownButtonsProperty); } set { SetValue(DisplayUpDownButtonsProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets whether dragging the value of the <see cref="NumericTextBox"/> is enabled.
        /// </summary>
        public bool AllowMouseDrag { get { return (bool)GetValue(AllowMouseDragProperty); } set { SetValue(AllowMouseDragProperty, value.Box()); } }

        /// <summary>
        /// Gets or sets when the <see cref="NumericTextBox"/> should be validated when the user uses the mouse to change its value.
        /// </summary>
        public MouseValidationTrigger MouseValidationTrigger { get { return (MouseValidationTrigger)GetValue(MouseValidationTriggerProperty); } set { SetValue(MouseValidationTriggerProperty, value); } }

        /// <summary>
        /// Raised when the <see cref="Value"/> property has changed.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<double> ValueChanged { add { AddHandler(ValueChangedEvent, value); } remove { RemoveHandler(ValueChangedEvent, value); } }

        /// <summary>
        /// Raised when one of the repeat button is pressed.
        /// </summary>
        public event EventHandler<RepeatButtonPressedRoutedEventArgs> RepeatButtonPressed { add { AddHandler(RepeatButtonPressedEvent, value); } remove { RemoveHandler(RepeatButtonPressedEvent, value); } }

        /// <summary>
        /// Raised when one of the repeat button is released.
        /// </summary>
        public event EventHandler<RepeatButtonPressedRoutedEventArgs> RepeatButtonReleased { add { AddHandler(RepeatButtonReleasedEvent, value); } remove { RemoveHandler(RepeatButtonReleasedEvent, value); } }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            increaseButton = GetTemplateChild("PART_IncreaseButton") as RepeatButton;
            if (increaseButton == null)
                throw new InvalidOperationException("A part named 'PART_IncreaseButton' must be present in the ControlTemplate, and must be of type 'RepeatButton'.");

            decreaseButton = GetTemplateChild("PART_DecreaseButton") as RepeatButton;
            if (decreaseButton == null)
                throw new InvalidOperationException("A part named 'PART_DecreaseButton' must be present in the ControlTemplate, and must be of type 'RepeatButton'.");

            contentHost = GetTemplateChild("PART_ContentHost") as ScrollViewer;
            if (contentHost == null)
                throw new InvalidOperationException("A part named 'PART_ContentHost' must be present in the ControlTemplate, and must be of type 'ScrollViewer'.");

            var increasePressedWatcher = new DependencyPropertyWatcher(increaseButton);
            increasePressedWatcher.RegisterValueChangedHandler(ButtonBase.IsPressedProperty, RepeatButtonIsPressedChanged);
            var decreasePressedWatcher = new DependencyPropertyWatcher(decreaseButton);
            decreasePressedWatcher.RegisterValueChangedHandler(ButtonBase.IsPressedProperty, RepeatButtonIsPressedChanged);
            var textValue = FormatValue(Value);

            SetCurrentValue(TextProperty, textValue);
        }

        /// <summary>
        /// Raised when the <see cref="Value"/> property has changed.
        /// </summary>
        protected virtual void OnValueChanged(double? oldValue, double? newValue)
        {
        }

        /// <inheritdoc/>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            var textValue = FormatValue(Value);
            SetCurrentValue(TextProperty, textValue);
        }

        /// <inheritdoc/>
        protected sealed override void OnCancelled()
        {
            var expression = GetBindingExpression(ValueProperty);
            expression?.UpdateTarget();

            var textValue = FormatValue(Value);
            SetCurrentValue(TextProperty, textValue);
        }

        /// <inheritdoc/>
        protected sealed override void OnValidated()
        {
            double? value;
            if (double.TryParse(Text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue))
            {
                value = parsedValue;
            }
            else
            {
                value = Value;
            }
            SetCurrentValue(ValueProperty, value);

            var expression = GetBindingExpression(ValueProperty);
            expression?.UpdateSource();
        }

        protected override bool IsTextCompatibleWithValueBinding(string text)
        {
            return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
        }

        /// <inheritdoc/>
        [NotNull]
        protected override string CoerceTextForValidation(string baseValue)
        {
            baseValue = base.CoerceTextForValidation(baseValue);
            double? value;
            if (double.TryParse(baseValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedValue))
            {
                value = parsedValue;

                if (value > Maximum)
                {
                    value = Maximum;
                }
                if (value < Minimum)
                {
                    value = Minimum;
                }
            }
            else
            {
                value = Value;
            }

            return FormatValue(value);
        }

        [NotNull]
        protected string FormatValue(double? value)
        {
            if (!value.HasValue)
                return string.Empty;

            var decimalPlaces = DecimalPlaces;
            var coercedValue = decimalPlaces < 0 ? value.Value : Math.Round(value.Value, decimalPlaces);
            return coercedValue.ToString(CultureInfo.InvariantCulture);
        }

        private void RepeatButtonIsPressedChanged(object sender, EventArgs e)
        {
            var repeatButton = (RepeatButton)sender;
            if (ReferenceEquals(repeatButton, increaseButton))
            {
                RaiseEvent(new RepeatButtonPressedRoutedEventArgs(RepeatButtons.IncreaseButton, repeatButton.IsPressed ? RepeatButtonPressedEvent : RepeatButtonReleasedEvent));
            }
            if (ReferenceEquals(repeatButton, decreaseButton))
            {
                RaiseEvent(new RepeatButtonPressedRoutedEventArgs(RepeatButtons.DecreaseButton, repeatButton.IsPressed ? RepeatButtonPressedEvent : RepeatButtonReleasedEvent));
            }
        }

        private void OnValuePropertyChanged(double? oldValue, double? newValue)
        {
            if (newValue.HasValue && newValue.Value > Maximum)
            {
                SetCurrentValue(ValueProperty, Maximum);
                return;
            }
            if (newValue.HasValue && newValue.Value < Minimum)
            {
                SetCurrentValue(ValueProperty, Minimum);
                return;
            }

            var textValue = FormatValue(newValue);
            updatingValue = true;
            SetCurrentValue(TextProperty, textValue);
            SetCurrentValue(ValueRatioProperty, newValue.HasValue ? MathUtil.InverseLerp(Minimum, Maximum, newValue.Value) : 0.0);
            updatingValue = false;

            RaiseEvent(new RoutedPropertyChangedEventArgs<double?>(oldValue, newValue, ValueChangedEvent));
            OnValueChanged(oldValue, newValue);
        }

        private void UpdateValue(double value)
        {
            if (IsReadOnly == false)
            {
                SetCurrentValue(ValueProperty, value);
            }
        }

        private static void OnValuePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((NumericTextBox)sender).OnValuePropertyChanged((double?)e.OldValue, (double?)e.NewValue);
        }

        private static void OnDecimalPlacesPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var numericInput = (NumericTextBox)sender;
            numericInput.CoerceValue(ValueProperty);
        }

        private static void OnMinimumPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var numericInput = (NumericTextBox)sender;
            var needValidation = false;
            if (numericInput.Maximum < numericInput.Minimum)
            {
                numericInput.SetCurrentValue(MaximumProperty, numericInput.Minimum);
                needValidation = true;
            }
            if (numericInput.Value < numericInput.Minimum)
            {
                numericInput.SetCurrentValue(ValueProperty, numericInput.Minimum);
                needValidation = true;
            }

            // Do not overwrite the Value, it is already correct!
            numericInput.updatingValue = true;
            numericInput.SetCurrentValue(ValueRatioProperty, numericInput.Value.HasValue ? MathUtil.InverseLerp(numericInput.Minimum, numericInput.Maximum, numericInput.Value.Value) : 0.0);
            numericInput.updatingValue = false;

            if (needValidation)
            {
                numericInput.Validate();
            }
        }

        private static void OnMaximumPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var numericInput = (NumericTextBox)sender;
            var needValidation = false;
            if (numericInput.Minimum > numericInput.Maximum)
            {
                numericInput.SetCurrentValue(MinimumProperty, numericInput.Maximum);
                needValidation = true;
            }
            if (numericInput.Value > numericInput.Maximum)
            {
                numericInput.SetCurrentValue(ValueProperty, numericInput.Maximum);
                needValidation = true;
            }

            // Do not overwrite the Value, it is already correct!
            numericInput.updatingValue = true;
            numericInput.SetCurrentValue(ValueRatioProperty, numericInput.Value.HasValue ? MathUtil.InverseLerp(numericInput.Minimum, numericInput.Maximum, numericInput.Value.Value) : 0.0);
            numericInput.updatingValue = false;

            if (needValidation)
            {
                numericInput.Validate();
            }
        }

        private static void ValueRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (NumericTextBox)d;
            if (control != null && !control.updatingValue)
                control.UpdateValue(MathUtil.Lerp(control.Minimum, control.Maximum, (double)e.NewValue));
        }

        private static void UpdateValueCommand([NotNull] object sender, Func<NumericTextBox, double> getValue, bool validate = true)
        {
            var control = sender as NumericTextBox ?? ((System.Windows.Controls.TextBox)sender).FindVisualParentOfType<NumericTextBox>();
            if (control != null)
            {
                var value = getValue(control);
                control.UpdateValue(value);
                control.SelectAll();
                if (validate)
                    control.Validate();
            }
        }

        private static void OnLargeIncreaseCommand([NotNull] object sender, ExecutedRoutedEventArgs e)
        {
            UpdateValueCommand(sender, x => (x.Value ?? x.Minimum) + x.LargeChange);
        }

        private static void OnLargeDecreaseCommand([NotNull] object sender, ExecutedRoutedEventArgs e)
        {
            UpdateValueCommand(sender, x => (x.Value ?? x.Maximum) - x.LargeChange);
        }

        private static void OnSmallIncreaseCommand([NotNull] object sender, ExecutedRoutedEventArgs e)
        {
            UpdateValueCommand(sender, x => (x.Value ?? x.Minimum) + x.SmallChange);
        }

        private static void OnSmallDecreaseCommand([NotNull] object sender, ExecutedRoutedEventArgs e)
        {
            UpdateValueCommand(sender, x => (x.Value ?? x.Maximum) - x.SmallChange);
        }

        private static void OnResetValueCommand([NotNull] object sender, ExecutedRoutedEventArgs e)
        {
            UpdateValueCommand(sender, x => 0.0, false);
        }

        private static void OnForbiddenPropertyChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var metadata = e.Property.GetMetadata(d);
            if (!Equals(e.NewValue, metadata.DefaultValue))
            {
                var message = $"The value of the property '{e.Property.Name}' cannot be different from the value '{metadata.DefaultValue}'";
                throw new InvalidOperationException(message);
            }
        }
    }
}
