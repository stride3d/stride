// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Stride.Core.Extensions;

namespace Stride.Core.Presentation.ValueConverters
{
    [ValueConversion(typeof(object), typeof(IEnumerable<object>))]
    public class Yield : OneWayValueConverter<Yield>
    {
        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == DependencyProperty.UnsetValue ? DependencyProperty.UnsetValue : value.Yield();
        }
    }
}
