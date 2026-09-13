// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class Vector3Editor : VectorEditor<Vector3?>
{
    static Vector3Editor()
    {
        XProperty.Changed.AddClassHandler<Vector3Editor>(OnComponentPropertyChanged);
        YProperty.Changed.AddClassHandler<Vector3Editor>(OnComponentPropertyChanged);
        ZProperty.Changed.AddClassHandler<Vector3Editor>(OnComponentPropertyChanged);
        LengthProperty.Changed.AddClassHandler<Vector3Editor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<float?> LengthProperty =
        AvaloniaProperty.Register<Vector3Editor, float?>(nameof(Length), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceLengthValue);

    public static readonly StyledProperty<float?> XProperty =
        AvaloniaProperty.Register<Vector3Editor, float?>(nameof(X), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> YProperty =
        AvaloniaProperty.Register<Vector3Editor, float?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> ZProperty =
        AvaloniaProperty.Register<Vector3Editor, float?>(nameof(Z), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public float? Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    public float? X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public float? Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    public float? Z
    {
        get => GetValue(ZProperty);
        set => SetValue(ZProperty, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Vector3? value)
    {
        if (value is not { } vector)
            return;

        switch (EditingMode)
        {
            case VectorEditingMode.Normal:
            case VectorEditingMode.AllComponents:
                SetCurrentValue(XProperty, vector.X);
                SetCurrentValue(YProperty, vector.Y);
                SetCurrentValue(ZProperty, value.Value.Z);
                break;

            case VectorEditingMode.Length:
                SetCurrentValue(LengthProperty, vector.Length());
                break;
        }
    }

    /// <inheritdoc/>
    protected override Vector3? UpdateValueFromComponent(AvaloniaProperty property)
    {
        switch (EditingMode)
        {
            case VectorEditingMode.Normal:
                if (property == XProperty)
                    return X.HasValue && Value.HasValue ? new Vector3(X.Value, Value.Value.Y, Value.Value.Z) : null;
                if (property == YProperty)
                    return Y.HasValue && Value.HasValue ? new Vector3(Value.Value.X, Y.Value, Value.Value.Z) : null;
                if (property == ZProperty)
                    return Z.HasValue && Value.HasValue ? new Vector3(Value.Value.X, Value.Value.Y, Z.Value) : null;
                break;

            case VectorEditingMode.AllComponents:
                if (property == XProperty)
                    return X.HasValue ? new Vector3(X.Value) : null;
                if (property == YProperty)
                    return Y.HasValue ? new Vector3(Y.Value) : null;
                if (property == ZProperty)
                    return Z.HasValue ? new Vector3(Z.Value) : null;
                break;

            case VectorEditingMode.Length:
                if (property == LengthProperty)
                    return Length.HasValue ? FromLength(Value ?? Vector3.One, Length.Value) : null;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(EditingMode));
        }

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)} in {EditingMode} mode.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Vector3? UpateValueFromFloat(float value)
    {
        return new Vector3(value);
    }

    private static float? CoerceLengthValue(AvaloniaObject sender, float? baseValue)
    {
        baseValue = CoerceComponentValue(sender, baseValue);
        return Math.Max(0.0f, baseValue ?? 0.0f);
    }

    private static Vector3 FromLength(Vector3 value, float length)
    {
        var newValue = value;
        newValue.Normalize();
        newValue *= length;
        return newValue;
    }
}
