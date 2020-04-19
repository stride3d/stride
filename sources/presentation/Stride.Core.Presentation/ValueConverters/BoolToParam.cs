// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert a boolean to the object given in parameter if its true,
    /// and to <see cref="DependencyProperty.UnsetValue"/> if it's false.
    /// <see cref="ConvertBack"/> is supported and will return whether the given object is different from
    /// <see cref="DependencyProperty.UnsetValue"/>.
    /// </summary>
    public class BoolToParam : ValueConverterBase<BoolToParam>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = ConverterHelper.ConvertToBoolean(value, culture);
            return result ? parameter : DependencyProperty.UnsetValue;
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value != DependencyProperty.UnsetValue;
            return result.Box();
        }
    }
}
