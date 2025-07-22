// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Data;
using Stride.Core.Mathematics;

namespace Stride.Core.Presentation.Avalonia.Controls;

using Matrix = Mathematics.Matrix;

public sealed class QuaternionEditor : VectorEditorBase<Quaternion?>
{
    private Vector3 decomposedRotation;

    static QuaternionEditor()
    {
        XProperty.Changed.AddClassHandler<QuaternionEditor>(OnComponentPropertyChanged);
        YProperty.Changed.AddClassHandler<QuaternionEditor>(OnComponentPropertyChanged);
        ZProperty.Changed.AddClassHandler<QuaternionEditor>(OnComponentPropertyChanged);
    }

    public static readonly StyledProperty<float?> XProperty =
        AvaloniaProperty.Register<QuaternionEditor, float?>(nameof(X), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> YProperty =
        AvaloniaProperty.Register<QuaternionEditor, float?>(nameof(Y), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

    public static readonly StyledProperty<float?> ZProperty =
        AvaloniaProperty.Register<QuaternionEditor, float?>(nameof(Z), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceComponentValue);

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

    /// <inheritdoc/>
    public override void ResetValue()
    {
        Value = Quaternion.Identity;
    }

    /// <inheritdoc/>
    protected override void UpdateComponentsFromValue(Quaternion? value)
    {
        if (value is not { } quaternion)
            return;

        // This allows iterating on the euler angles when resulting rotation are equivalent
        var current = Recompose(ref decomposedRotation);
        if (current == quaternion && X.HasValue && Y.HasValue && Z.HasValue)
            return;

        var rotationMatrix = Matrix.RotationQuaternion(quaternion);
        rotationMatrix.Decompose(out decomposedRotation.Y, out decomposedRotation.X, out decomposedRotation.Z);
        SetCurrentValue(XProperty, GetDisplayValue(decomposedRotation.X));
        SetCurrentValue(YProperty, GetDisplayValue(decomposedRotation.Y));
        SetCurrentValue(ZProperty, GetDisplayValue(decomposedRotation.Z));
    }

    /// <inheritdoc/>
    protected override Quaternion? UpdateValueFromComponent(AvaloniaProperty property)
    {
        Vector3? newDecomposedRotation;
        if (property == XProperty)
            newDecomposedRotation = X.HasValue ? new Vector3(MathUtil.DegreesToRadians(X.Value), decomposedRotation.Y, decomposedRotation.Z) : null;
        else if (property == YProperty)
            newDecomposedRotation = Y.HasValue ? new Vector3(decomposedRotation.X, MathUtil.DegreesToRadians(Y.Value), decomposedRotation.Z) : null;
        else if (property == ZProperty)
            newDecomposedRotation = Z.HasValue ? new Vector3(decomposedRotation.X, decomposedRotation.Y, MathUtil.DegreesToRadians(Z.Value)) : null;
        else
            throw new ArgumentException($"Property {property} is unsupported by method {nameof(UpdateValueFromComponent)}.");

        if (newDecomposedRotation.HasValue)
        {
            decomposedRotation = newDecomposedRotation.Value;
            return Recompose(ref decomposedRotation);
        }
        return null;
    }

    /// <inheritdoc/>
    protected override Quaternion? UpateValueFromFloat(float value)
    {
        var radian = MathUtil.DegreesToRadians(value);
        decomposedRotation = new Vector3(radian);
        return Recompose(ref decomposedRotation);
    }

    private static float GetDisplayValue(float angleRadians)
    {
        var degrees = MathUtil.RadiansToDegrees(angleRadians);
        if (degrees == 0 && float.IsNegative(degrees))
        {
            // Matrix.DecomposeXYZ can give -0 when MathF.Asin(-0) == -0,
            // whereas previously Math.Asin(-0) == +0 (ie. did not respect the sign value at zero).
            // This shows up in the editor but we don't want to see this.
            degrees = 0;
        }
        return degrees;
    }

    private static Quaternion Recompose(ref Vector3 vector)
    {
        return Quaternion.RotationYawPitchRoll(vector.Y, vector.X, vector.Z);
    }
}
