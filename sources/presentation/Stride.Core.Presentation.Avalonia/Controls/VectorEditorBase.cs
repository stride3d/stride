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
    
    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
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

    [return: NotNullIfNotNull(nameof(basevalue))]
    protected static float? CoerceComponentValue(AvaloniaObject sender, float? basevalue)
    {
        if (!basevalue.HasValue)
            return null;

        var editor = (VectorEditorBase<T>)sender;
        var decimalPlaces = editor.DecimalPlaces;
        return decimalPlaces < 0 ? basevalue : MathF.Round(basevalue.Value, decimalPlaces);
    }
}
