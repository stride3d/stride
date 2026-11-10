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
    /// <param name="min">The minimum.</param>
    /// <param name="decimals">The decimal places</param>
    /// <remarks>
    /// The parameter names differ from the other constructor on purpose: named arguments
    /// (<c>minimum:</c>, <c>decimalPlaces:</c>) must bind to the other constructor, not to this one.
    /// </remarks>
    [Obsolete("Use named arguments instead: [DataMemberRange(minimum: x, decimalPlaces: y)]. Two positional arguments are ambiguous with (minimum, maximum).")]
    public DataMemberRangeAttribute(double min, int decimals)
    {
        if (double.IsNaN(min)) throw new ArgumentOutOfRangeException(nameof(min));
        if (decimals < 0) throw new ArgumentException("The decimalPlaces should be greater or equal to zero.", nameof(decimals));
        Minimum = min;
        DecimalPlaces = decimals;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMemberRangeAttribute" /> class.
    /// </summary>
    /// <param name="minimum">The minimum value this member accepts.</param>
    /// <param name="maximum">The maximum value this member accepts.</param>
    /// <param name="smallStep">The default step used to go from minimum to maximum.</param>
    /// <param name="largeStep">The 'accelerated' step used to go from minimum to maximum.</param>
    /// <param name="decimalPlaces">The number of digits to show after the decimal point.</param>
    /// <remarks>
    /// Parameters left at their default value (<see cref="double.NaN"/>, or -1 for <paramref name="decimalPlaces"/>) are undefined.
    /// </remarks>
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
