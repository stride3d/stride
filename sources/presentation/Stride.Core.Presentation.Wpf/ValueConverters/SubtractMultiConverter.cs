// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// Subtracts every value after the first (and the numeric parameter, if any) from the first value.
    /// The result is clamped to be non-negative. Useful to compute a remaining layout width.
    /// </summary>
    public class SubtractMultiConverter : OneWayMultiValueConverter<SubtractMultiConverter>
    {
        [NotNull]
        public override object Convert([NotNull] object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                throw new InvalidOperationException("This multi converter must be invoked with at least two elements");

            double result;
            try
            {
                result = ConverterHelper.ConvertToDouble(values[0], culture);
                for (var i = 1; i < values.Length; i++)
                    result -= ConverterHelper.ConvertToDouble(values[i], culture);
                if (parameter != null)
                    result -= ConverterHelper.ConvertToDouble(parameter, culture);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The values of this converter must be convertible to a double.", exception);
            }

            return Math.Max(0.0, result);
        }
    }
}
