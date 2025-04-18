// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Data;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Stride.Core.Presentation.Avalonia.Controls;

// FIXME xplat-editor make it work with Avalonia's TimePicker
public sealed class TimeSpanEditor : TemplatedControl
{
    public static readonly StyledProperty<TimeSpan?> ValueProperty =
        AvaloniaProperty.Register<TimeSpanEditor, TimeSpan?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> DaysProperty =
        AvaloniaProperty.Register<TimeSpanEditor, int?>(nameof(Days), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> HoursProperty =
        AvaloniaProperty.Register<TimeSpanEditor, int?>(nameof(Hours), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> MinutesProperty =
        AvaloniaProperty.Register<TimeSpanEditor, int?>(nameof(Minutes), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double?> SecondsProperty =
        AvaloniaProperty.Register<TimeSpanEditor, double?>(nameof(Seconds), defaultBindingMode: BindingMode.TwoWay);
    
    public TimeSpan? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int? Days
    {
        get => GetValue(DaysProperty);
        set => SetValue(DaysProperty, value);
    }

    public int? Hours
    {
        get => GetValue(HoursProperty);
        set => SetValue(HoursProperty, value);
    }

    public int? Minutes
    {
        get => GetValue(MinutesProperty);
        set => SetValue(MinutesProperty, value);
    }

    public double? Seconds
    {
        get => GetValue(SecondsProperty);
        set => SetValue(SecondsProperty, value);
    }

    /// <summary>
    /// Updates the properties corresponding to the components of the time span from the given time span value.
    /// </summary>
    /// <param name="value">The time span from which to update component properties.</param>
    private void UpdateComponentsFromValue(TimeSpan? value)
    {
        if (value is not { } timeSpan)
            return;

        SetCurrentValue(DaysProperty, timeSpan.Days);
        SetCurrentValue(HoursProperty, timeSpan.Hours);
        SetCurrentValue(MinutesProperty, timeSpan.Minutes);
        SetCurrentValue(SecondsProperty, (double)(timeSpan.Ticks % TimeSpan.TicksPerMinute) / TimeSpan.TicksPerSecond);
    }

    /// <summary>
    /// Updates the <see cref="Value"/> property according to a change in the given component property.
    /// </summary>
    /// <param name="property">The component property from which to update the <see cref="Value"/>.</param>
    private TimeSpan? UpdateValueFromComponent(AvaloniaProperty property)
    {
        // NOTE: Precision must be on OS tick level.

        if (property == DaysProperty)
            return Days.HasValue && Value.HasValue ? new TimeSpan(Days.Value * TimeSpan.TicksPerDay + Value.Value.Hours * TimeSpan.TicksPerHour + Value.Value.Minutes * TimeSpan.TicksPerMinute + Value.Value.Ticks % TimeSpan.TicksPerMinute) : null;
        if (property == HoursProperty)
            return Hours.HasValue && Value.HasValue ? new TimeSpan(Value.Value.Days * TimeSpan.TicksPerDay + Hours.Value * TimeSpan.TicksPerHour + Value.Value.Minutes * TimeSpan.TicksPerMinute + Value.Value.Ticks % TimeSpan.TicksPerMinute) : null;
        if (property == MinutesProperty)
            return Minutes.HasValue && Value.HasValue ? new TimeSpan(Value.Value.Days * TimeSpan.TicksPerDay + Value.Value.Hours * TimeSpan.TicksPerHour + Minutes.Value * TimeSpan.TicksPerMinute + Value.Value.Ticks % TimeSpan.TicksPerMinute) : null;
        if (property == SecondsProperty)
            return Seconds.HasValue && Value.HasValue ? new TimeSpan(Value.Value.Days * TimeSpan.TicksPerDay + Value.Value.Hours * TimeSpan.TicksPerHour + Value.Value.Minutes * TimeSpan.TicksPerMinute + (long)(Seconds.Value * TimeSpan.TicksPerSecond)) : null;
        
        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

}
