// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// Converts the type to its type name.
    /// </summary>
    public class TypeToTypeName : OneWayValueConverter<TypeToTypeName>
    {
        /// <inheritdoc/>
        public override object Convert(object value, [NotNull] Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            Type type = (Type)value;
            if (parameter is null)
                return type.Name;
            return parameter.ToString().Replace("$", type.Name);
        }
    }
}
