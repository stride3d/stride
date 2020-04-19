// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will return the maximal result between the converter value and the converter parameter.
    /// </summary>
    public class MaxNum : OneWayValueConverter<MaxNum>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var doubleValue = ConverterHelper.ConvertToDouble(value, culture);
            var doubleParameter = ConverterHelper.ConvertToDouble(parameter, culture);
            return System.Convert.ChangeType(Math.Max(doubleValue, doubleParameter), value?.GetType() ?? targetType);
        }
    }
}
