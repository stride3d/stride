// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// Edits an angle stored in radians, shown and typed in degrees.
/// </summary>
public sealed class AngleEditor : VectorEditorBase<float?>
{
    public static readonly StyledProperty<float?> DegreesProperty =
        AvaloniaProperty.Register<AngleEditor, float?>(nameof(Degrees), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    static AngleEditor()
    {
        DegreesProperty.Changed.AddClassHandler<AngleEditor>(OnComponentPropertyChanged);
    }

    /// <summary>
    /// Gets or sets the angle in degrees.
    /// </summary>
    public float? Degrees
    {
        get => GetValue(DegreesProperty);
        set => SetValue(DegreesProperty, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(float? value)
    {
        if (value is not { } radians)
            return;

        SetCurrentValue(DegreesProperty, MathF.Round(MathUtil.RadiansToDegrees(radians), 4));
    }

    /// <inheritdoc/>
    protected override float? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (property == DegreesProperty)
            return Degrees is { } degrees ? MathUtil.DegreesToRadians(degrees) : null;

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override float? UpateValueFromFloat(float value)
    {
        return MathUtil.DegreesToRadians(value);
    }
}
