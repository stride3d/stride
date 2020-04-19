// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;

using Stride.Core.Presentation.Core;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will format a CamelCase string by inserting spaces between words.
    /// </summary>
    public class CamelCaseTextConverter : OneWayValueConverter<CamelCaseTextConverter>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var strVal = value.ToString();
            return Utils.SplitCamelCase(strVal);
        }
    }
}
