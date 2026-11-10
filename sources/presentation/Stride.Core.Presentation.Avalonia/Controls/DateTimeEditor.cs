// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Stride.Core.Presentation.Avalonia.Controls;

// FIXME xplat-editor make it work with Avalonia's DatePicker and TimePicker
public sealed class DateTimeEditor : TemplatedControl
{
    private bool interlock;
    private bool templateApplied;
    private AvaloniaProperty? initializingProperty;

    static DateTimeEditor()
    {
        ValueProperty.Changed.AddClassHandler<DateTimeEditor>((sender, _) => sender.OnValuePropertyChanged());
        YearProperty.Changed.AddClassHandler<DateTimeEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        MonthProperty.Changed.AddClassHandler<DateTimeEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        DayProperty.Changed.AddClassHandler<DateTimeEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        HourProperty.Changed.AddClassHandler<DateTimeEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        MinuteProperty.Changed.AddClassHandler<DateTimeEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        SecondProperty.Changed.AddClassHandler<DateTimeEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
    }

    public static readonly StyledProperty<DateTime?> ValueProperty =
        AvaloniaProperty.Register<DateTimeEditor, DateTime?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<DateTimeEditor, string?>(nameof(PlaceholderText));

    public static readonly StyledProperty<int?> YearProperty =
        AvaloniaProperty.Register<DateTimeEditor, int?>(nameof(Year), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> MonthProperty =
        AvaloniaProperty.Register<DateTimeEditor, int?>(nameof(Month), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> DayProperty =
        AvaloniaProperty.Register<DateTimeEditor, int?>(nameof(Day), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> HourProperty =
        AvaloniaProperty.Register<DateTimeEditor, int?>(nameof(Hour), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> MinuteProperty =
        AvaloniaProperty.Register<DateTimeEditor, int?>(nameof(Minute), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double?> SecondProperty =
        AvaloniaProperty.Register<DateTimeEditor, double?>(nameof(Second), defaultBindingMode: BindingMode.TwoWay);

    public DateTime? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public int? Year
    {
        get => GetValue(YearProperty);
        set => SetValue(YearProperty, value);
    }

    public int? Month
    {
        get => GetValue(MonthProperty);
        set => SetValue(MonthProperty, value);
    }

    public int? Day
    {
        get => GetValue(DayProperty);
        set => SetValue(DayProperty, value);
    }

    public int? Hour
    {
        get => GetValue(HourProperty);
        set => SetValue(HourProperty, value);
    }

    public int? Minute
    {
        get => GetValue(MinuteProperty);
        set => SetValue(MinuteProperty, value);
    }

    public double? Second
    {
        get => GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        templateApplied = false;
        base.OnApplyTemplate(e);
        templateApplied = true;
    }

    /// <summary>
    /// Updates the properties corresponding to the components of the date time from the given date time value.
    /// </summary>
    /// <param name="value">The date time from which to update component properties.</param>
    private void UpdateComponentsFromValue(DateTime? value)
    {
        if (value is not { } dateTime)
            return;

        SetCurrentValue(YearProperty, dateTime.Year);
        SetCurrentValue(MonthProperty, dateTime.Month);
        SetCurrentValue(DayProperty, dateTime.Day);
        SetCurrentValue(HourProperty, dateTime.Hour);
        SetCurrentValue(MinuteProperty, dateTime.Minute);
        SetCurrentValue(SecondProperty, (double)(dateTime.Ticks % TimeSpan.TicksPerMinute) / TimeSpan.TicksPerSecond);
    }

    /// <summary>
    /// Updates the <see cref="Value"/> property according to a change in the given component property.
    /// </summary>
    /// <param name="property">The component property from which to update the <see cref="Value"/>.</param>
    private DateTime? UpdateValueFromComponent(AvaloniaProperty property)
    {
        // NOTE: Precision must be on OS tick level.

        if (property == YearProperty)
        {
            if (!Year.HasValue || !Value.HasValue)
                return null;
            long ticks = new DateTime(Year.Value, Value.Value.Month, Math.Min(DateTime.DaysInMonth(Year.Value, Value.Value.Month), Value.Value.Day), Value.Value.Hour, Value.Value.Minute, 0).Ticks;
            return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
        }

        if (property == MonthProperty)
        {
            if (!Month.HasValue || !Value.HasValue)
                return null;
            long ticks = new DateTime(Value.Value.Year, Month.Value, Math.Min(DateTime.DaysInMonth(Value.Value.Year, Month.Value), Value.Value.Day), Value.Value.Hour, Value.Value.Minute, 0).Ticks;
            return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
        }

        if (property == DayProperty)
        {
            if (!Day.HasValue || !Value.HasValue)
                return null;
            long ticks = new DateTime(Value.Value.Year, Value.Value.Month, Math.Min(DateTime.DaysInMonth(Value.Value.Year, Value.Value.Month), Day.Value), Value.Value.Hour, Value.Value.Minute, 0).Ticks;
            return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
        }

        if (property == HourProperty)
        {
            if (!Hour.HasValue || !Value.HasValue)
                return null;
            long ticks = new DateTime(Value.Value.Year, Value.Value.Month, Value.Value.Day, Hour.Value, Value.Value.Minute, 0).Ticks;
            return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
        }

        if (property == MinuteProperty)
        {
            if (!Minute.HasValue || !Value.HasValue)
                return null;
            long ticks = new DateTime(Value.Value.Year, Value.Value.Month, Value.Value.Day, Value.Value.Hour, Minute.Value, 0).Ticks;
            return new DateTime(ticks + Value.Value.Ticks % TimeSpan.TicksPerMinute);
        }

        if (property == SecondProperty)
        {
            if (!Second.HasValue || !Value.HasValue)
                return null;
            long ticks = Value.Value.Ticks - (Value.Value.Ticks % TimeSpan.TicksPerMinute);
            return new DateTime(ticks + (long)(Second.Value * TimeSpan.TicksPerSecond));
        }

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <summary>
    /// Raised when the <see cref="Value"/> property is modified.
    /// </summary>
    private void OnValuePropertyChanged()
    {
        var isInitializing = !templateApplied && initializingProperty is null;
        if (isInitializing)
            initializingProperty = ValueProperty;

        if (!interlock)
        {
            interlock = true;
            UpdateComponentsFromValue(Value);
            interlock = false;
        }

        UpdateBinding(ValueProperty);
        if (isInitializing)
            initializingProperty = null;
    }

    /// <summary>
    /// Raised when either of the <see cref="Year"/>, <see cref="Month"/>, <see cref="Day"/>, <see cref="Hour"/>, <see cref="Minute"/> or <see cref="Second"/> properties are modified.
    /// </summary>
    /// <param name="e">The event data.</param>
    private void OnComponentPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var isInitializing = !templateApplied && initializingProperty is null;
        if (isInitializing)
            initializingProperty = e.Property;

        if (!interlock)
        {
            interlock = true;
            Value = UpdateValueFromComponent(e.Property);
            UpdateComponentsFromValue(Value);
            interlock = false;
        }

        UpdateBinding(e.Property);
        if (isInitializing)
            initializingProperty = null;
    }

    /// <summary>
    /// Updates the binding of the given property.
    /// </summary>
    /// <param name="property">The property.</param>
    private void UpdateBinding(AvaloniaProperty property)
    {
        if (property != initializingProperty)
        {
            BindingOperations.GetBindingExpressionBase(this, property)?.UpdateSource();
        }
    }
}
