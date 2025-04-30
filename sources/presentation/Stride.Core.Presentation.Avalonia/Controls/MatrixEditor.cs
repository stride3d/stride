// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Data;

namespace Stride.Core.Presentation.Avalonia.Controls;

using Matrix = Mathematics.Matrix;

public sealed class MatrixEditor : VectorEditorBase<Matrix?>
{
    public static readonly StyledProperty<float?> M11Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M11), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M12Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M12), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M13Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M13), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M14Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M14), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M21Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M21), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M22Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M22), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M23Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M23), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M24Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M24), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M31Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M31), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M32Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M32), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M33Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M33), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M34Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M34), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M41Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M41), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M42Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M42), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M43Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M43), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> M44Property =
        AvaloniaProperty.Register<RectangleEditor, float?>(nameof(M44), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    private static readonly Dictionary<AvaloniaProperty, int> PropertyToIndex;

    static MatrixEditor()
    {
        PropertyToIndex = new Dictionary<AvaloniaProperty, int>(16)
        {
            { M11Property, 0 }, { M12Property, 1 }, { M13Property, 2 }, { M14Property, 3 },
            { M21Property, 4 }, { M22Property, 5 }, { M23Property, 6 }, { M24Property, 7 },
            { M31Property, 8 }, { M32Property, 9 }, { M33Property, 10 }, { M34Property, 11 },
            { M41Property, 12 }, { M42Property, 13 }, { M43Property, 14 }, { M44Property, 15 },
        };
        M11Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M12Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M13Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M14Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M21Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M22Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M23Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M24Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M31Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M32Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M33Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M34Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M41Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M42Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M43Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
        M44Property.Changed.AddClassHandler<MatrixEditor>(OnComponentPropertyChanged);
    }

    public float? M11
    {
        get => GetValue(M11Property);
        set => SetValue(M11Property, value);
    }

    public float? M12
    {
        get => GetValue(M12Property);
        set => SetValue(M12Property, value);
    }

    public float? M13
    {
        get => GetValue(M13Property);
        set => SetValue(M13Property, value);
    }

    public float? M14
    {
        get => GetValue(M14Property);
        set => SetValue(M14Property, value);
    }

    public float? M21
    {
        get => GetValue(M21Property);
        set => SetValue(M21Property, value);
    }

    public float? M22
    {
        get => GetValue(M22Property);
        set => SetValue(M22Property, value);
    }

    public float? M23
    {
        get => GetValue(M23Property);
        set => SetValue(M23Property, value);
    }

    public float? M24
    {
        get => GetValue(M24Property);
        set => SetValue(M24Property, value);
    }

    public float? M31
    {
        get => GetValue(M31Property);
        set => SetValue(M31Property, value);
    }

    public float? M32
    {
        get => GetValue(M32Property);
        set => SetValue(M32Property, value);
    }

    public float? M33
    {
        get => GetValue(M33Property);
        set => SetValue(M33Property, value);
    }

    public float? M34
    {
        get => GetValue(M34Property);
        set => SetValue(M34Property, value);
    }

    public float? M41
    {
        get => GetValue(M41Property);
        set => SetValue(M41Property, value);
    }

    public float? M42
    {
        get => GetValue(M42Property);
        set => SetValue(M42Property, value);
    }

    public float? M43
    {
        get => GetValue(M43Property);
        set => SetValue(M43Property, value);
    }

    public float? M44
    {
        get => GetValue(M44Property);
        set => SetValue(M44Property, value);
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Matrix? value)
    {
        if (value is not { } matrix)
            return;

        foreach (var property in PropertyToIndex)
        {
            SetCurrentValue(property.Key, matrix[property.Value]);
        }
    }

    /// <inheritdoc/>
    protected override Matrix? UpdateValueFromComponent(AvaloniaProperty property)
    {
        if (!Value.HasValue || !((float?)GetValue(property)).HasValue)
            return null;

        var array = new float[16];
        foreach (var (localProperty, index) in PropertyToIndex)
        {
            array[index] = property == localProperty ? ((float?)GetValue(localProperty)).Value : Value.Value[index];
        }
        return new Matrix(array);
    }

    /// <inheritdoc/>
    [return: NotNull]
    protected override Matrix? UpateValueFromFloat(float value)
    {
        return new Matrix(value);
    }
}
