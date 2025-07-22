// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Stride.Core.Presentation.Avalonia.Controls;

public abstract class VectorEditorBase : TemplatedControl
{
    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<VectorEditorBase, int>(nameof(DecimalPlaces), -1);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<VectorEditorBase, string?>(nameof(Watermark));

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public abstract void ResetValue();

    /// <summary>
    /// Sets the vector value of this vector editor from a single float value.
    /// </summary>
    /// <param name="value">The value to use to generate a vector.</param>
    public abstract void SetVectorFromValue(float value);
}

public abstract class VectorEditorBase<T> : VectorEditorBase
{
    private bool interlock;
    private bool templateApplied;
    private AvaloniaProperty? initializingProperty;

    static VectorEditorBase()
    {
        ValueProperty.Changed.AddClassHandler<VectorEditorBase<T>>(OnValuePropertyChanged);
    }

    public static readonly StyledProperty<T> ValueProperty =
        AvaloniaProperty.Register<VectorEditorBase<T>, T>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<T> DefaultValueProperty =
        AvaloniaProperty.Register<VectorEditorBase<T>, T>(nameof(DefaultValue));

    public T Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public T DefaultValue
    {
        get => GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <inheritdoc/>
    public override void ResetValue()
    {
        Value = DefaultValue;
    }

    /// <inheritdoc/>
    public override void SetVectorFromValue(float value)
    {
        Value = UpateValueFromFloat(value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        templateApplied = false;
        base.OnApplyTemplate(e);
        templateApplied = true;
    }

    /// <summary>
    /// Updates the properties corresponding to the components of the vector from the given vector value.
    /// </summary>
    /// <param name="value">The vector from which to update component properties.</param>
    protected abstract void UpdateComponentsFromValue(T value);

    /// <summary>
    /// Updates the <see cref="Value"/> property according to a change in the given component property.
    /// </summary>
    /// <param name="property">The component property from which to update the <see cref="Value"/>.</param>
    protected abstract T UpdateValueFromComponent(AvaloniaProperty property);

    /// <summary>
    /// Updates the <see cref="Value"/> property from a single float.
    /// </summary>
    /// <param name="value">The value to use to generate a vector.</param>
    protected abstract T UpateValueFromFloat(float value);

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

    private void OnComponentPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var isInitializing = !templateApplied && initializingProperty == null;
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
    /// Updates the binding of the given dependency property.
    /// </summary>
    /// <param name="property">The dependency property.</param>
    private void UpdateBinding(AvaloniaProperty property)
    {
        if (property != initializingProperty)
        {
            BindingOperations.GetBindingExpressionBase(this, property)?.UpdateSource();
        }
    }

    protected static void OnComponentPropertyChanged(VectorEditorBase<T> sender, AvaloniaPropertyChangedEventArgs e)
    {
        sender.OnComponentPropertyChanged(e);
    }

    [return: NotNullIfNotNull(nameof(basevalue))]
    protected static float? CoerceComponentValue(AvaloniaObject sender, float? basevalue)
    {
        if (!basevalue.HasValue)
            return null;

        var editor = (VectorEditorBase<T>)sender;
        var decimalPlaces = editor.DecimalPlaces;
        return decimalPlaces < 0 ? basevalue : MathF.Round(basevalue.Value, decimalPlaces);
    }

    private static void OnValuePropertyChanged(VectorEditorBase<T> sender, AvaloniaPropertyChangedEventArgs e)
    {
        sender.OnValuePropertyChanged();
    }
}
