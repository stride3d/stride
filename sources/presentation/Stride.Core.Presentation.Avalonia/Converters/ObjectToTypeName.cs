// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class ObjectToTypeName : OneWayValueConverter<ObjectToTypeName>
{
    /// <summary>
    /// The string representation of the type of a null object
    /// </summary>
    public const string NullObjectType = "(None)";

    /// <inheritdoc/>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.GetType().ToSimpleCSharpName() ?? NullObjectType;
    }
}
