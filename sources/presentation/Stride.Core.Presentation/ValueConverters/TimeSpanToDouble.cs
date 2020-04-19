// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This value converter will convert a TimeSpan to double by using the <see cref="TimeSpan.TotalSeconds"/> property.
    /// </summary>
    public class TimeSpanToDouble : ValueConverterBase<TimeSpanToDouble>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return targetType == typeof(double) ? ConverterHelper.ConvertToTimeSpan(value, culture).TotalSeconds : ConverterHelper.TryConvertToTimeSpan(value, culture)?.TotalSeconds;
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var doubleValue = targetType == typeof(TimeSpan) ? ConverterHelper.ConvertToDouble(value, culture) : ConverterHelper.TryConvertToDouble(value, culture);
            return doubleValue != null ? (object)TimeSpan.FromSeconds(doubleValue.Value) : null;
        }
    }
}
