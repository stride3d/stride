// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Annotations
{
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
            if (decimalPlaces < 0) throw new ArgumentException(@"The decimalPlaces should be greater or equal to zero.", nameof(decimalPlaces));
            Minimum = minimum;
            DecimalPlaces = decimalPlaces;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberRangeAttribute" /> class.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="smallStep">The minimum step used to go from minimum to maximum.</param>
        /// <param name="largeStep">The maximum step used to go from minimum to maximum.</param>
        /// <param name="decimalPlaces">The decimal places.</param>
        public DataMemberRangeAttribute(double minimum, double maximum, double smallStep, double largeStep, int decimalPlaces)
            : this(minimum, decimalPlaces)
        {
            if (double.IsNaN(maximum)) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (minimum > maximum) throw new ArgumentException(@"The minimum should be lesser or equal to the maximum.", nameof(minimum));
            if (double.IsNaN(smallStep)) throw new ArgumentOutOfRangeException(nameof(smallStep));
            if (double.IsNaN(largeStep)) throw new ArgumentOutOfRangeException(nameof(largeStep));
            if (smallStep > largeStep) throw new ArgumentException(@"The smallStep should be lesser or equal to the largeStep.", nameof(smallStep));
            if (decimalPlaces < 0) throw new ArgumentException(@"The decimalPlaces should be greater or equal to zero.", nameof(decimalPlaces));
            Maximum = maximum;
            SmallStep = smallStep;
            LargeStep = largeStep;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberRangeAttribute" /> class.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        [Obsolete("This method will be removed in a future release. Use DataMemberRangeAttribute(double minimum, double maximum, double smallStep, double largeStep, int decimalPlaces) instead")]
        public DataMemberRangeAttribute(double minimum, double maximum)
            : this(minimum, maximum, 1, 2, 3)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberRangeAttribute" /> class.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="decimalPlaces">The decimal places</param>
        [Obsolete("This method will be removed in a future release. Use DataMemberRangeAttribute(double minimum, double maximum, double smallStep, double largeStep, int decimalPlaces) instead")]
        public DataMemberRangeAttribute(double minimum, double maximum, int decimalPlaces)
            : this(minimum, maximum, 1, 2, decimalPlaces)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberRangeAttribute" /> class.
        /// </summary>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        /// <param name="smallStep">The minimum step used to go from minimum to maximum.</param>
        /// <param name="largeStep">The maximum step.</param>
        [Obsolete("This method will be removed in a future release. Use DataMemberRangeAttribute(double minimum, double maximum, double smallStep, double largeStep, int decimalPlaces) instead")]
        public DataMemberRangeAttribute(double minimum, double maximum, double smallStep, double largeStep)
            : this(minimum, maximum, smallStep, largeStep, 3)
        {
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
}
