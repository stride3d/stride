// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    public class DateTimeToString : ValueConverterBase<DateTimeToString>
    {
        public override object Convert(object value, [NotNull] Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            DateTime dateTime = (DateTime)value;
            return dateTime.ToString(culture);
        }

        public override object ConvertBack(object value, [NotNull] Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            string stringValue = value.ToString();
            return DateTime.Parse(stringValue, culture);
        }
    }
}
