// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter convert a <see cref="Nullable"/> type to its underlying type. If the type is not nullable, it returns the type itself.
    /// </summary>
    public class UnderlyingType : OneWayValueConverter<UnderlyingType>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value as Type;
            if (type == null)
            {
                throw new ArgumentException("The object passed to this value converter is not a type.");
            }
            return Nullable.GetUnderlyingType(type) ?? value;
        }
    }
}
