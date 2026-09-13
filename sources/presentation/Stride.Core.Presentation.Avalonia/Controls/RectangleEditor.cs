// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class RectangleEditor : VectorEditorBase<Rectangle?>
{
    static RectangleEditor()
    {
        RectXProperty.Changed.AddClassHandler<RectangleEditor>(OnComponentPropertyChanged);
        RectYProperty.Changed.AddClassHandler<RectangleEditor>(OnComponentPropertyChanged);
        RectXProperty.Changed.AddClassHandler<RectangleEditor>(OnComponentPropertyChanged);
        RectHeightProperty.Changed.AddClassHandler<RectangleEditor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<int?> RectXProperty =
        AvaloniaProperty.Register<RectangleEditor, int?>(nameof(RectX), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> RectYProperty =
        AvaloniaProperty.Register<RectangleEditor, int?>(nameof(RectY), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> RectWidthProperty =
        AvaloniaProperty.Register<RectangleEditor, int?>(nameof(RectWidth), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> RectHeightProperty =
        AvaloniaProperty.Register<RectangleEditor, int?>(nameof(RectHeight), defaultBindingMode: BindingMode.TwoWay);

    public int? RectX
    {
        get => GetValue(RectXProperty);
        set => SetValue(RectXProperty, value);
    }

    public int? RectY
    {
        get => GetValue(RectYProperty);
        set => SetValue(RectYProperty, value);
    }

    public int? RectWidth
    {
        get => GetValue(RectWidthProperty);
        set => SetValue(RectWidthProperty, value);
    }

    public int? RectHeight
    {
        get => GetValue(RectHeightProperty);
        set => SetValue(RectHeightProperty, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Rectangle? value)
    {
        if (value is not { } rectangle)
            return;

        SetCurrentValue(RectXProperty, rectangle.X);
        SetCurrentValue(RectYProperty, rectangle.Y);
        SetCurrentValue(RectWidthProperty, rectangle.Width);
        SetCurrentValue(RectHeightProperty, rectangle.Height);
    }

    /// <inheritdoc/>
    protected override Rectangle? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == RectXProperty)
            return RectX.HasValue && Value.HasValue ? new Rectangle(RectX.Value, Value.Value.Y, Value.Value.Width, Value.Value.Height) : null;
        if (property == RectYProperty)
            return RectY.HasValue && Value.HasValue ? new Rectangle(Value.Value.X, RectY.Value, Value.Value.Width, Value.Value.Height) : null;
        if (property == RectWidthProperty)
            return RectWidth.HasValue && Value.HasValue ? new Rectangle(Value.Value.X, Value.Value.Y, RectWidth.Value, Value.Value.Height) : null;
        if (property == RectHeightProperty)
            return RectHeight.HasValue && Value.HasValue ? new Rectangle(Value.Value.X, Value.Value.Y, Value.Value.Width, RectHeight.Value) : null;

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Rectangle? UpateValueFromFloat(float value)
    {
        var intValue = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        return new Rectangle(0, 0, intValue, intValue);
    }
}
