// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class RectangleFEditor : VectorEditorBase<RectangleF?>
{
    static RectangleFEditor()
    {
        RectXProperty.Changed.AddClassHandler<RectangleFEditor>(OnComponentPropertyChanged);
        RectYProperty.Changed.AddClassHandler<RectangleFEditor>(OnComponentPropertyChanged);
        RectXProperty.Changed.AddClassHandler<RectangleFEditor>(OnComponentPropertyChanged);
        RectHeightProperty.Changed.AddClassHandler<RectangleFEditor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<float?> RectXProperty =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(RectX), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> RectYProperty =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(RectY), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> RectWidthProperty =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(RectWidth), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> RectHeightProperty =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(RectHeight), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public float? RectX
    {
        get => GetValue(RectXProperty);
        set => SetValue(RectXProperty, value);
    }

    public float? RectY
    {
        get => GetValue(RectYProperty);
        set => SetValue(RectYProperty, value);
    }

    public float? RectWidth
    {
        get => GetValue(RectWidthProperty);
        set => SetValue(RectWidthProperty, value);
    }

    public float? RectHeight
    {
        get => GetValue(RectHeightProperty);
        set => SetValue(RectHeightProperty, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(RectangleF? value)
    {
        if (value is not { } rectangleF)
            return;

        SetCurrentValue(RectXProperty, rectangleF.X);
        SetCurrentValue(RectYProperty, rectangleF.Y);
        SetCurrentValue(RectWidthProperty, rectangleF.Width);
        SetCurrentValue(RectHeightProperty, rectangleF.Height);
    }

    /// <inheritdoc/>
    protected override RectangleF? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == RectXProperty)
            return RectX.HasValue && Value.HasValue ? new RectangleF(RectX.Value, Value.Value.Y, Value.Value.Width, Value.Value.Height) : null;
        if (property == RectYProperty)
            return RectY.HasValue && Value.HasValue ? new RectangleF(Value.Value.X, RectY.Value, Value.Value.Width, Value.Value.Height) : null;
        if (property == RectWidthProperty)
            return RectWidth.HasValue && Value.HasValue ? new RectangleF(Value.Value.X, Value.Value.Y, RectWidth.Value, Value.Value.Height) : null;
        if (property == RectHeightProperty)
            return RectHeight.HasValue && Value.HasValue ? new RectangleF(Value.Value.X, Value.Value.Y, Value.Value.Width, RectHeight.Value) : null;

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override RectangleF? UpateValueFromFloat(float value)
    {
        return new RectangleF(0.0f, 0.0f, value, value);
    }
}
