// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.IO;
using Stride.Core.IO;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert an <see cref="UDirectory"/> object to its string representation. <see cref="ConvertBack"/> is supported.
    /// </summary>
    /// <seealso cref="UFileToString"/>
    public class UDirectoryToString : ValueConverterBase<UDirectoryToString>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().Replace('/', Path.DirectorySeparatorChar);
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            try
            {
                return new UDirectory((string)value);
            }
            catch
            {
                return new UDirectory("");
            }
        }
    }
}
