// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will multiply a given numeric value by the numeric value given as parameter.
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class Multiply : ValueConverterBase<Multiply>
    {
        /// <inheritdoc/>
        public override object Convert(object value, [NotNull] Type targetType, object parameter, CultureInfo culture)
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

            double param;
            try
            {
                param = ConverterHelper.ConvertToDouble(parameter, culture);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The parameter of this converter must be convertible to a double.", exception);
            }

            return System.Convert.ChangeType(scalar * param, targetType);
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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

            double param;
            try
            {
                param = ConverterHelper.ConvertToDouble(parameter, culture);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The parameter of this converter must be convertible to a double.", exception);
            }

            if (Math.Abs(param) > double.Epsilon)
            {
                return System.Convert.ChangeType(scalar / param, targetType);
            }

            return DependencyProperty.UnsetValue;
        }
    }
}
