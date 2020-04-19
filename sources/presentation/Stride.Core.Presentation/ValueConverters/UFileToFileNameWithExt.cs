// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;

using Stride.Core.IO;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert an <see cref="UFile"/> to a string representing the file name with its extension.
    /// </summary>
    public class UFileToFileNameWithExt : OneWayValueConverter<UFileToFileNameWithExt>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var ufile = (UFile)value;
            return ufile.GetFileName();
        }
    }
}
