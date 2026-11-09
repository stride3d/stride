// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Annotations;

/// <summary>
/// Defines range values for a property or field.
/// </summary>
/// <remarks><see cref="Minimum"/>, <see cref="Maximum"/> and <see cref="SmallStep"/> must have the same type</remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DataMemberRangeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataMemberRangeAttribute" /> class.
    /// </summary>
    /// <param name="minimum">The minimum.</param>
    /// <param name="decimalPlaces">The decimal places</param>
    public DataMemberRangeAttribute(double minimum, int decimalPlaces)
    {
        if (double.IsNaN(minimum)) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (decimalPlaces < 0) throw new ArgumentException("The decimalPlaces should be greater or equal to zero.", nameof(decimalPlaces));
        Minimum = minimum;
        DecimalPlaces = decimalPlaces;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMemberRangeAttribute" /> class, values left at default or matching defaults are interpreted as undefined.
    /// </summary>
    /// <param name="minimum">The minimum value this member accepts.</param>
    /// <param name="maximum">The maximum value this member accepts.</param>
    /// <param name="smallStep">The default step used to go from minimum to maximum.</param>
    /// <param name="largeStep">The 'accelerated' step used to go from minimum to maximum.</param>
    /// <param name="decimalPlaces">The amount of digits to show past the decimal.</param>
    public DataMemberRangeAttribute(double minimum = double.NaN, double maximum = double.NaN, double smallStep = double.NaN, double largeStep = double.NaN, int decimalPlaces = -1)
    {
        if (double.IsNaN(maximum) == false && double.IsNaN(minimum) == false && minimum > maximum)
            throw new ArgumentException("The minimum should be lesser or equal to the maximum.", nameof(minimum));

        if (double.IsNaN(smallStep) == false && double.IsNaN(largeStep) == false && smallStep > largeStep)
            throw new ArgumentException("The smallStep should be lesser or equal to the largeStep.", nameof(smallStep));

        if (decimalPlaces < -1)
            throw new ArgumentException("The decimalPlaces should be greater or equal to zero.", nameof(decimalPlaces));
        
        DecimalPlaces = decimalPlaces == -1 ? null : decimalPlaces;
        Minimum = double.IsNaN(minimum) ? null : minimum;
        Maximum = double.IsNaN(maximum) ? null : maximum;
        SmallStep = double.IsNaN(smallStep) ? null : smallStep;
        LargeStep = double.IsNaN(largeStep) ? null : largeStep;
    }

    /// <summary>
    /// Gets the minimum inclusive.
    /// </summary>
    /// <value>The minimum.</value>
    public double? Minimum { get; }

    /// <summary>
    /// Gets the maximum inclusive.
    /// </summary>
    /// <value>The maximum.</value>
    public double? Maximum { get; }

    /// <summary>
    /// Gets the minimum step.
    /// </summary>
    /// <value>The minimum step.</value>
    public double? SmallStep { get; }

    /// <summary>
    /// Gets the maximum step.
    /// </summary>
    /// <value>The maximum step.</value>
    public double? LargeStep { get; }

    /// <summary>
    /// Gets the decimal places.
    /// </summary>
    /// <value>The decimal places.</value>
    public int? DecimalPlaces { get; }
}
