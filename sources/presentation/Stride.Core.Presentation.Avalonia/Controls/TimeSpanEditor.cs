// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Data;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace Stride.Core.Presentation.Avalonia.Controls;

// FIXME xplat-editor make it work with Avalonia's TimePicker
public sealed class TimeSpanEditor : TemplatedControl
{
    private bool interlock;
    private bool templateApplied;
    private AvaloniaProperty? initializingProperty;

    static TimeSpanEditor()
    {
        ValueProperty.Changed.AddClassHandler<TimeSpanEditor>((sender, _) => sender.OnValuePropertyChanged());
        DaysProperty.Changed.AddClassHandler<TimeSpanEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        HoursProperty.Changed.AddClassHandler<TimeSpanEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        MinutesProperty.Changed.AddClassHandler<TimeSpanEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
        SecondsProperty.Changed.AddClassHandler<TimeSpanEditor>((sender, e) => sender.OnComponentPropertyChanged(e));
    }

    public static readonly StyledProperty<TimeSpan?> ValueProperty =
        AvaloniaProperty.Register<TimeSpanEditor, TimeSpan?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<TimeSpanEditor, string?>(nameof(PlaceholderText));

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

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
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

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        templateApplied = false;
        base.OnApplyTemplate(e);
        templateApplied = true;
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
    /// Raised when either of the <see cref="Days"/>, <see cref="Hours"/>, <see cref="Minutes"/> or <see cref="Seconds"/> properties are modified.
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
