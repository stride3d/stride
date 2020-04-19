// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This value converter will convert any numeric value to double. <see cref="ConvertBack"/> is supported and
    /// will convert the value to the target if it is numeric, otherwise it returns the value as-is.
    /// </summary>
    public class ToDouble : ValueConverterBase<ToDouble>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return targetType == typeof(double) ? ConverterHelper.ConvertToDouble(value, culture) : ConverterHelper.TryConvertToDouble(value, culture);
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return targetType.IsValueType && !targetType.IsNullable() ? ConverterHelper.ChangeType(value, targetType, culture) : ConverterHelper.TryChangeType(value, targetType, culture);
        }
    }
}
