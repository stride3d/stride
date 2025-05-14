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
using Avalonia.Media;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

[TemplatePart("PART_TextBox", typeof(TextBox), IsRequired = true)]
public sealed class NumericTextBox : TemplatedControl
{
    private bool internalValueSet;
    private bool isSyncingTextAndValueProperties;
    private bool isTextChangedFromUI;
    private IDisposable? textBoxTextChangedSubscription;

    static NumericTextBox()
    {
        MaximumProperty.Changed.AddClassHandler<NumericTextBox>(OnMaximumPropertyChanged);
        MinimumProperty.Changed.AddClassHandler<NumericTextBox>(OnMinimumPropertyChanged);
        TextProperty.Changed.AddClassHandler<NumericTextBox, string?>(OnTextPropertyChanged);
        ValueProperty.Changed.AddClassHandler<NumericTextBox, double?>(OnValuePropertyChanged);
    }

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<NumericTextBox, int>(nameof(DecimalPlaces), -1);

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

    public static readonly StyledProperty<string?> WatermarkProperty =
        TextBox.WatermarkProperty.AddOwner<NumericTextBox>();

    public static readonly RoutedEvent<NumericTextBoxValueChangedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<NumericTextBox, NumericTextBoxValueChangedEventArgs>(nameof(ValueChanged), RoutingStrategies.Bubble);

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
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

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public event EventHandler<NumericTextBoxValueChangedEventArgs>? ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>
    /// Gets the TextBox template part.
    /// </summary>
    private TextBox? TextBox { get; set; }

    protected override void OnInitialized()
    {
        if (!internalValueSet)
        {
            SyncTextAndValueProperties(false, null, true);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (TextBox is not null)
        {
            textBoxTextChangedSubscription?.Dispose();
        }
        TextBox = e.NameScope.Find<TextBox>("PART_TextBox");
        if (TextBox is not null)
        {
            textBoxTextChangedSubscription = TextBox.TextProperty.Changed.AddClassHandler<TextBox>((_, _) => TextBox_OnTextPropertyChanged());
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                var commitSuccess = CommitInput();
                e.Handled = !commitSuccess;
                break;
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        CommitInput(true);
        base.OnLostFocus(e);
    }

    private bool CommitInput(bool forceTextUpdate = false)
    {
        if (SyncTextAndValueProperties(true, Text, forceTextUpdate))
        {
            BindingOperations.GetBindingExpressionBase(this, ValueProperty)?.UpdateSource();
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
        if (!internalValueSet && IsInitialized)
        {
            SyncTextAndValueProperties(false, null, true);
        }

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
            }
            finally
            {
                internalValueSet = false;
            }
        }
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
}
