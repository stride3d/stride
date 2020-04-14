// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.ValueConverters
{
    public class FormatString : OneWayValueConverter<FormatString>
    {
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == DependencyProperty.UnsetValue)
                return value;

            var format = parameter as string;
            return string.Format(format ?? "{0}", value);
        }
    }
}
