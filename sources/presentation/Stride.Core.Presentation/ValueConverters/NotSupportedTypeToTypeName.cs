// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter convert any unsupported object type to a string representing the name of its type (without assembly or namespace qualification).
    /// </summary>
    /// <seealso cref="ObjectToFullTypeName"/>
    /// <seealso cref="ObjectToType"/>
    public class NotSupportedTypeToTypeName : OneWayValueConverter<NotSupportedTypeToTypeName>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Type objectType) || value == DependencyProperty.UnsetValue)
                return DependencyProperty.UnsetValue;

            var typeDescriptor = TypeDescriptorFactory.Default.Find(objectType);
            if (typeDescriptor is NotSupportedObjectDescriptor)
                return objectType.ToSimpleCSharpName();

            return DependencyProperty.UnsetValue;
        }
    }
}
