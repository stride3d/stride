// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter converts a <see cref="Type"/> to a boolean indicating whether it is a nullable value type (<c>Nullable&lt;T&gt;</c>).
    /// </summary>
    public class IsNullable : OneWayValueConverter<IsNullable>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Type type)
            {
                throw new ArgumentException("The object passed to this value converter is not a type.");
            }

            return type.IsNullable().Box();
        }
    }
}
