// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Stride.Core.Presentation.Avalonia.Controls;

// FIXME xplat-editor make it work with Avalonia's DatePicker and TimePicker
public sealed class DateTimeEditor : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> ValueProperty =
        AvaloniaProperty.Register<DateTimeEditor, DateTime?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<DateTimeEditor, string?>(nameof(Watermark));

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

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
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
}
