// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    public class ThicknessMultiConverter : OneWayMultiValueConverter<ThicknessMultiConverter>
    {
        [NotNull]
        public override object Convert([NotNull] object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            switch (values.Length)
            {
                case 1:
                    var uniform = ConverterHelper.ConvertToDouble(values[0], culture);
                    return new Thickness(uniform);

                case 2:
                    var horizontal = ConverterHelper.ConvertToDouble(values[0], culture);
                    var vertical = ConverterHelper.ConvertToDouble(values[1], culture);
                    return new Thickness(horizontal, vertical, horizontal, vertical);

                case 4:
                    var left = ConverterHelper.ConvertToDouble(values[0], culture);
                    var top = ConverterHelper.ConvertToDouble(values[1], culture);
                    var right = ConverterHelper.ConvertToDouble(values[2], culture);
                    var bottom = ConverterHelper.ConvertToDouble(values[3], culture);
                    return new Thickness(left, top, right, bottom);

                default:
                    throw new ArgumentException($"Inconsistent number of parameters: expected 1, 2 or 4 values, got {values.Length}.", nameof(values));
            }
        }
    }
}
