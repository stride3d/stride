// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows.Controls;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert a boolean value to a <see cref="SelectionMode"/> value, where <c>false</c> translates to <see cref="SelectionMode.Single"/>
    /// and <c>true></c> translates to <see cref="SelectionMode.Extended"/>.
    /// <see cref="ConvertBack"/> is supported.
    /// </summary>
    /// <remarks>If the boolean value <c>false</c> is passed as converter parameter, the visibility is inverted.</remarks>
    public class ExtendedOrSingle : ValueConverterBase<ExtendedOrSingle>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = ConverterHelper.ConvertToBoolean(value, culture);
            if (parameter as bool? == false)
            {
                result = !result;
            }
            return result ? SelectionMode.Extended : SelectionMode.Single;
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var selectionMode = (SelectionMode)value;
            var result = selectionMode == SelectionMode.Extended;
            if (parameter as bool? == false)
            {
                result = !result;
            }
            return result.Box();
        }
    }

}
