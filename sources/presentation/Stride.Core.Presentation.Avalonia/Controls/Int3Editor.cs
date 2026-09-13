// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class Int3Editor : VectorEditorBase<Int3?>
{
    static Int3Editor()
    {
        XProperty.Changed.AddClassHandler<Int3Editor>(OnComponentPropertyChanged);
        YProperty.Changed.AddClassHandler<Int3Editor>(OnComponentPropertyChanged);
        ZProperty.Changed.AddClassHandler<Int3Editor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<int?> XProperty =
        AvaloniaProperty.Register<Int3Editor, int?>(nameof(X), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> YProperty =
        AvaloniaProperty.Register<Int3Editor, int?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> ZProperty =
        AvaloniaProperty.Register<Int4Editor, int?>(nameof(Z), defaultBindingMode: BindingMode.TwoWay);

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

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Int3? value)
    {
        if (value is not { } int3)
            return;

        SetCurrentValue(XProperty, int3.X);
        SetCurrentValue(YProperty, int3.Y);
        SetCurrentValue(ZProperty, int3.Z);
    }

    /// <inheritdoc/>
    protected override Int3? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == XProperty)
            return X.HasValue && Value.HasValue ? new Int3(X.Value, Value.Value.Y, Value.Value.Z) : null;
        if (property == YProperty)
            return Y.HasValue && Value.HasValue ? new Int3(Value.Value.X, Y.Value, Value.Value.Z) : null;
        if (property == ZProperty)
            return Z.HasValue && Value.HasValue ? new Int3(Value.Value.X, Value.Value.Y, Z.Value) : null;

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Int3? UpateValueFromFloat(float value)
    {
        return new Int3((int)MathF.Round(value, MidpointRounding.AwayFromZero));
    }
}
