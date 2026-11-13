// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;
using Stride.Core.Presentation.Avalonia.Controls;
using Thickness = Stride.UI.Thickness;

namespace Stride.UI.Editor.Avalonia.Views;

/// <summary>
/// Edits a <see cref="Thickness"/> of the UI (margin, padding) with one box per side: left, right, top, bottom, back and front.
/// </summary>
public sealed class ThicknessEditor : VectorEditorBase<Thickness?>
{
    public static readonly StyledProperty<float?> LeftProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Left), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> TopProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Top), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> BackProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Back), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> RightProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Right), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> BottomProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Bottom), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> FrontProperty =
        AvaloniaProperty.Register<ThicknessEditor, float?>(nameof(Front), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    static ThicknessEditor()
    {
        LeftProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        TopProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        BackProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        RightProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        BottomProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
        FrontProperty.Changed.AddClassHandler<ThicknessEditor>(OnComponentPropertyChanged);
    }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Left"/> component of the thickness.
    /// </summary>
    public float? Left
    {
        get => GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Top"/> component of the thickness.
    /// </summary>
    public float? Top
    {
        get => GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Back"/> component of the thickness.
    /// </summary>
    public float? Back
    {
        get => GetValue(BackProperty);
        set => SetValue(BackProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Right"/> component of the thickness.
    /// </summary>
    public float? Right
    {
        get => GetValue(RightProperty);
        set => SetValue(RightProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Bottom"/> component of the thickness.
    /// </summary>
    public float? Bottom
    {
        get => GetValue(BottomProperty);
        set => SetValue(BottomProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="Thickness.Front"/> component of the thickness.
    /// </summary>
    public float? Front
    {
        get => GetValue(FrontProperty);
        set => SetValue(FrontProperty, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Thickness? value)
    {
        if (value is not { } thickness)
            return;

        SetCurrentValue(LeftProperty, thickness.Left);
        SetCurrentValue(TopProperty, thickness.Top);
        SetCurrentValue(BackProperty, thickness.Back);
        SetCurrentValue(RightProperty, thickness.Right);
        SetCurrentValue(BottomProperty, thickness.Bottom);
        SetCurrentValue(FrontProperty, thickness.Front);
    }

    /// <inheritdoc/>
    protected override Thickness? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (Value is not { } value)
            return null;

        if (property == LeftProperty)
            return Left is { } left ? new Thickness(left, value.Top, value.Back, value.Right, value.Bottom, value.Front) : null;
        if (property == TopProperty)
            return Top is { } top ? new Thickness(value.Left, top, value.Back, value.Right, value.Bottom, value.Front) : null;
        if (property == BackProperty)
            return Back is { } back ? new Thickness(value.Left, value.Top, back, value.Right, value.Bottom, value.Front) : null;
        if (property == RightProperty)
            return Right is { } right ? new Thickness(value.Left, value.Top, value.Back, right, value.Bottom, value.Front) : null;
        if (property == BottomProperty)
            return Bottom is { } bottom ? new Thickness(value.Left, value.Top, value.Back, value.Right, bottom, value.Front) : null;
        if (property == FrontProperty)
            return Front is { } front ? new Thickness(value.Left, value.Top, value.Back, value.Right, value.Bottom, front) : null;

        throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Thickness? UpateValueFromFloat(float value)
    {
        return Thickness.UniformCuboid(value);
    }
}
