// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class Vector2Editor : VectorEditor<Vector2?>
{
    static Vector2Editor()
    {
        XProperty.Changed.AddClassHandler<Vector2Editor>(OnComponentPropertyChanged);
        YProperty.Changed.AddClassHandler<Vector2Editor>(OnComponentPropertyChanged);
        LengthProperty.Changed.AddClassHandler<Vector2Editor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<float?> LengthProperty =
        AvaloniaProperty.Register<Vector2Editor, float?>(nameof(Length), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceLengthValue);

    public static readonly StyledProperty<float?> XProperty =
        AvaloniaProperty.Register<Vector2Editor, float?>(nameof(X), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> YProperty =
        AvaloniaProperty.Register<Vector2Editor, float?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

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

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Vector2? value)
    {
        if (value is not { } vector)
            return;

        switch (EditingMode)
        {
            case VectorEditingMode.Normal:
            case VectorEditingMode.AllComponents:
                SetCurrentValue(XProperty, vector.X);
                SetCurrentValue(YProperty, vector.Y);
                break;

            case VectorEditingMode.Length:
                SetCurrentValue(LengthProperty, vector.Length());
                break;
        }
    }

    /// <inheritdoc/>
    protected override Vector2? UpdateValueFromComponent(AvaloniaProperty property)
    {
        switch (EditingMode)
        {
            case VectorEditingMode.Normal:
                if (property == XProperty)
                    return X.HasValue && Value.HasValue ? new Vector2(X.Value, Value.Value.Y) : null;
                if (property == YProperty)
                    return Y.HasValue && Value.HasValue ? new Vector2(Value.Value.X, Y.Value) : null;
                break;

            case VectorEditingMode.AllComponents:
                if (property == XProperty)
                    return X.HasValue ? new Vector2(X.Value) : null;
                if (property == YProperty)
                    return Y.HasValue ? new Vector2(Y.Value) : null;
                break;

            case VectorEditingMode.Length:
                if (property == LengthProperty)
                    return Length.HasValue ? FromLength(Value ?? Vector2.One, Length.Value) : null;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(EditingMode));
        }

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)} in {EditingMode} mode.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Vector2? UpateValueFromFloat(float value)
    {
        return new Vector2(value);
    }

    private static float? CoerceLengthValue(AvaloniaObject sender, float? baseValue)
    {
        baseValue = CoerceComponentValue(sender, baseValue);
        return Math.Max(0.0f, baseValue ?? 0.0f);
    }

    private static Vector2 FromLength(Vector2 value, float length)
    {
        var newValue = value;
        newValue.Normalize();
        newValue *= length;
        return newValue;
    }
}
