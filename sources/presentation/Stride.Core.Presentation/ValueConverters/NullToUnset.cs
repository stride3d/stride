// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert a null value to <see cref="DependencyProperty.UnsetValue"/>. If the given object is not null, it will be returned as it is.
    /// </summary>
    public class NullToUnset : OneWayValueConverter<NullToUnset>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value ?? DependencyProperty.UnsetValue;
        }
    }
}
