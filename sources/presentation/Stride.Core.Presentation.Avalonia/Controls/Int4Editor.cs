// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class Int4Editor : VectorEditorBase<Int4?>
{
    static Int4Editor()
    {
        XProperty.Changed.AddClassHandler<Int4Editor>(OnComponentPropertyChanged);
        YProperty.Changed.AddClassHandler<Int4Editor>(OnComponentPropertyChanged);
        ZProperty.Changed.AddClassHandler<Int4Editor>(OnComponentPropertyChanged);
        WProperty.Changed.AddClassHandler<Int4Editor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<int?> XProperty =
        AvaloniaProperty.Register<Int4Editor, int?>(nameof(X), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> YProperty =
        AvaloniaProperty.Register<Int4Editor, int?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> ZProperty =
        AvaloniaProperty.Register<Int4Editor, int?>(nameof(Z), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> WProperty =
        AvaloniaProperty.Register<Int4Editor, int?>(nameof(W), defaultBindingMode: BindingMode.TwoWay);

    public int? X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public int? Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    public int? Z
    {
        get => GetValue(ZProperty);
        set => SetValue(ZProperty, value);
    }

    public int? W
    {
        get => GetValue(WProperty);
        set => SetValue(WProperty, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Int4? value)
    {
        if (value is not { } int4)
            return;

        SetCurrentValue(XProperty, int4.X);
        SetCurrentValue(YProperty, int4.Y);
        SetCurrentValue(ZProperty, int4.Z);
        SetCurrentValue(WProperty, int4.W);
    }

    /// <inheritdoc/>
    protected override Int4? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == XProperty)
            return X.HasValue && Value.HasValue ? (Int4?)new Int4(X.Value, Value.Value.Y, Value.Value.Z, Value.Value.W) : null;
        if (property == YProperty)
            return Y.HasValue && Value.HasValue ? (Int4?)new Int4(Value.Value.X, Y.Value, Value.Value.Z, Value.Value.W) : null;
        if (property == ZProperty)
            return Z.HasValue && Value.HasValue ? (Int4?)new Int4(Value.Value.X, Value.Value.Y, Z.Value, Value.Value.W) : null;
        if (property == WProperty)
            return W.HasValue && Value.HasValue ? (Int4?)new Int4(Value.Value.X, Value.Value.Y, Value.Value.Z, W.Value) : null;

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Int4? UpateValueFromFloat(float value)
    {
        return new Int4((int)MathF.Round(value, MidpointRounding.AwayFromZero));
    }
}
