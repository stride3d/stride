// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class Int2Editor : VectorEditorBase<Int2?>
{
    static Int2Editor()
    {
        XProperty.Changed.AddClassHandler<Int2Editor>(OnComponentPropertyChanged);
        YProperty.Changed.AddClassHandler<Int2Editor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<int?> XProperty =
        AvaloniaProperty.Register<Int2Editor, int?>(nameof(X), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int?> YProperty =
        AvaloniaProperty.Register<Int2Editor, int?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay);

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

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Int2? value)
    {
        if (value is not { } int2)
            return;

        SetCurrentValue(XProperty, int2.X);
        SetCurrentValue(YProperty, int2.Y);
    }

    /// <inheritdoc/>
    protected override Int2? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == XProperty)
            return X.HasValue && Value.HasValue ? new Int2(X.Value, Value.Value.Y) : null;
        if (property == YProperty)
            return Y.HasValue && Value.HasValue ? new Int2(Value.Value.X, Y.Value) : null;

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Int2? UpateValueFromFloat(float value)
    {
        return new Int2((int)MathF.Round(value, MidpointRounding.AwayFromZero));
    }
}
