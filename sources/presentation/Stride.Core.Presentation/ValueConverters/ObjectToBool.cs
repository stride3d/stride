// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert an object to a boolean value, returning <c>false</c> if the object is equal to null, <c>true</c> otherwise.
    /// </summary>
    /// <remarks>Value types are always non-null and therefore always returns true.</remarks>
    public class ObjectToBool : OneWayValueConverter<ObjectToBool>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value != null;
            return result.Box();
        }
    }
}
