// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Internal;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// This converter will convert a numerical value to a boolean. The result will be <c>false</c> if the given value is equal to zero, <c>true</c> otherwise.
/// </summary>
/// <remarks>Supported types are: <see cref="sbyte"/>, <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="byte"/>, <see cref="ushort"/>, <see cref="uint"/>, <see cref="ulong"/></remarks>
public sealed class NumericToBool : OneWayValueConverter<NumericToBool>
{
    /// <inheritdoc/>
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool result = value switch
        {
            sbyte sb => sb != 0,
            short s => s != 0,
            int i => i != 0,
            long l => l != 0,
            byte b => b != 0,
            ushort us => us != 0,
            uint ui => ui != 0,
            ulong ul => ul != 0,
            _ => throw new ArgumentException($"{nameof(value)} is not a numeric type")
        };
        return result.Box();
    }
}
