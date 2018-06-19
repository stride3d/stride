// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Xenko.Core.Mathematics;

namespace Xenko.Core.Presentation.ValueConverters
{
    public class AngleSingleToDegrees : ValueConverterBase<AngleSingleToDegrees>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return targetType == typeof(double) ? ConverterHelper.ConvertToAngleSingle(value, culture).Degrees : ConverterHelper.TryConvertToAngleSingle(value, culture)?.Degrees;
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var doubleValue = targetType == typeof(AngleSingle) ? ConverterHelper.ConvertToDouble(value, culture) : ConverterHelper.TryConvertToDouble(value, culture);
            return doubleValue != null ? (object)new AngleSingle((float)doubleValue.Value, AngleType.Degree) : null;
        }
    }
}
