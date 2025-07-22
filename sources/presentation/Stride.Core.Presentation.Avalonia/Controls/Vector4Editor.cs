// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class Vector4Editor : VectorEditor<Vector4?>
{
    static Vector4Editor()
    {
        XProperty.Changed.AddClassHandler<Vector4Editor>(OnComponentPropertyChanged);
        YProperty.Changed.AddClassHandler<Vector4Editor>(OnComponentPropertyChanged);
        ZProperty.Changed.AddClassHandler<Vector4Editor>(OnComponentPropertyChanged);
        WProperty.Changed.AddClassHandler<Vector4Editor>(OnComponentPropertyChanged);
        LengthProperty.Changed.AddClassHandler<Vector4Editor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<float?> LengthProperty =
        AvaloniaProperty.Register<Vector4Editor, float?>(nameof(Length), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceLengthValue);

    public static readonly StyledProperty<float?> XProperty =
        AvaloniaProperty.Register<Vector4Editor, float?>(nameof(X), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> YProperty =
        AvaloniaProperty.Register<Vector4Editor, float?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> ZProperty =
        AvaloniaProperty.Register<Vector3Editor, float?>(nameof(Z), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> WProperty =
        AvaloniaProperty.Register<Vector3Editor, float?>(nameof(W), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

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

    public float? W
    {
        get => GetValue(WProperty);
        set => SetValue(WProperty, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Vector4? value)
    {
        if (value is not { } vector)
        {
            return;
        }

        switch (EditingMode)
        {
            case VectorEditingMode.Normal:
            case VectorEditingMode.AllComponents:
                SetCurrentValue(XProperty, vector.X);
                SetCurrentValue(YProperty, vector.Y);
                SetCurrentValue(ZProperty, value.Value.Z);
                SetCurrentValue(WProperty, value.Value.W);
                break;

            case VectorEditingMode.Length:
                SetCurrentValue(LengthProperty, vector.Length());
                break;
        }
    }

    /// <inheritdoc/>
    protected override Vector4? UpdateValueFromComponent(AvaloniaProperty property)
    {
        switch (EditingMode)
        {
            case VectorEditingMode.Normal:
                if (property == XProperty)
                    return X.HasValue && Value.HasValue ? new Vector4(X.Value, Value.Value.Y, Value.Value.Z, Value.Value.W) : null;
                if (property == YProperty)
                    return Y.HasValue && Value.HasValue ? new Vector4(Value.Value.X, Y.Value, Value.Value.Z, Value.Value.W) : null;
                if (property == ZProperty)
                    return Z.HasValue && Value.HasValue ? new Vector4(Value.Value.X, Value.Value.Y, Z.Value, Value.Value.W) : null;
                if (property == WProperty)
                    return W.HasValue && Value.HasValue ? new Vector4(Value.Value.X, Value.Value.Y, Value.Value.Z, W.Value) : null;
                break;

            case VectorEditingMode.AllComponents:
                if (property == XProperty)
                    return X.HasValue ? new Vector4(X.Value) : null;
                if (property == YProperty)
                    return Y.HasValue ? new Vector4(Y.Value) : null;
                if (property == ZProperty)
                    return Z.HasValue ? new Vector4(Z.Value) : null;
                if (property == WProperty)
                    return W.HasValue ? new Vector4(W.Value) : null;
                break;

            case VectorEditingMode.Length:
                if (property == LengthProperty)
                    return Length.HasValue ? FromLength(Value ?? Vector4.One, Length.Value) : null;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(EditingMode));
        }

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)} in {EditingMode} mode.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Vector4? UpateValueFromFloat(float value)
    {
        return new Vector4(value);
    }

    private static float? CoerceLengthValue(AvaloniaObject sender, float? baseValue)
    {
        baseValue = CoerceComponentValue(sender, baseValue);
        return Math.Max(0.0f, baseValue ?? 0.0f);
    }

    private static Vector4 FromLength(Vector4 value, float length)
    {
        var newValue = value;
        newValue.Normalize();
        newValue *= length;
        return newValue;
    }
}
