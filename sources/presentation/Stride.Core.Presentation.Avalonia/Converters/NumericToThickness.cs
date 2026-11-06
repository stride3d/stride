// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter will convert a double value to a <see cref="Thickness"/> structure that can be used for Margin, Padding, etc.
/// A <see cref="Thickness"/> must be passed as a parameter of this converter. You can use the <see cref="MarkupExtensions.ThicknessExtension"/>
/// markup extension to easily pass one, with the following syntax: {sd:Thickness (arguments)}. The resulting thickness will
/// be the parameter thickness multiplied bu the scalar double value.
/// </summary>
public sealed class NumericToThickness : ValueConverterBase<NumericToThickness>
{
    /// <inheritdoc/>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double scalar;
        try
        {
            scalar = ConverterHelper.ConvertToDouble(value, culture);
        }
        catch (Exception exception)
        {
            throw new ArgumentException("The value of this converter must be convertible to a double.", exception);
        }

        if (parameter is not Thickness thickness)
        {
            throw new ArgumentException("The parameter of this converter must be an instance of the Thickness structure. Use {sd:Thickness (arguments)} to construct one.");
        }

        return new Thickness(thickness.Left * scalar, thickness.Top * scalar, thickness.Right * scalar, thickness.Bottom * scalar);
    }

    /// <inheritdoc/>
    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Thickness thicknessValue)
        {
            throw new ArgumentException("The value of the ConvertBack method of this converter must be a an instance of the Thickness structure.");
        }
        if (parameter is not Thickness thicknessParameter)
        {
            throw new ArgumentException("The parameter of the ConvertBack method of this converter must be a an instance of the Thickness structure.");
        }

        var scalar = 0.0;
        if (Math.Abs(thicknessParameter.Left) > double.Epsilon)
        {
            scalar = thicknessValue.Left / thicknessParameter.Left;
        }
        else if (Math.Abs(thicknessParameter.Right) > double.Epsilon)
        {
            scalar = thicknessValue.Right / thicknessParameter.Right;
        }
        else if (Math.Abs(thicknessParameter.Top) > double.Epsilon)
        {
            scalar = thicknessValue.Top / thicknessParameter.Top;
        }
        else if (Math.Abs(thicknessParameter.Bottom) > double.Epsilon)
        {
            scalar = thicknessValue.Bottom / thicknessParameter.Bottom;
        }

        return System.Convert.ChangeType(scalar, targetType);
    }
}
