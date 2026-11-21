// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Internal;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter converts a <see cref="Type"/> to a boolean indicating whether it is a nullable value type (<c>Nullable&lt;T&gt;</c>).
/// </summary>
public sealed class IsNullable : OneWayValueConverter<IsNullable>
{
    /// <inheritdoc/>
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Type type)
        {
            throw new ArgumentException("The object passed to this value converter is not a type.");
        }

        return type.IsNullable().Box();
    }
}
