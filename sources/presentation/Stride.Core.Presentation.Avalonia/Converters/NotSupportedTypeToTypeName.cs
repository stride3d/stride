// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class NotSupportedTypeToTypeName : OneWayValueConverter<NotSupportedTypeToTypeName>
{
    /// <inheritdoc/>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Type objectType || value == AvaloniaProperty.UnsetValue)
            return AvaloniaProperty.UnsetValue;

        var typeDescriptor = TypeDescriptorFactory.Default.Find(objectType);
        return typeDescriptor is NotSupportedObjectDescriptor
            ? objectType.ToSimpleCSharpName()
            : AvaloniaProperty.UnsetValue;
    }
}
