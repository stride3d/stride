// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Avalonia;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// Helper class with similar methods than <see cref="Convert"/> but returns the default value of the expected type if value is <see cref="AvaloniaProperty.UnsetValue"/>.
/// </summary>
public static class ConverterHelper
{
    /// <summary>
    /// Converts the given value to <see cref="bool"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="bool"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="bool"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ConvertToBoolean(object? value, IFormatProvider culture)
    {
        return value != AvaloniaProperty.UnsetValue && Convert.ToBoolean(value, culture);
    }

    /// <summary>
    /// Converts the given value to <see cref="char"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="char"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="char"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ConvertToChar(object? value, IFormatProvider culture)
    {
        return value != AvaloniaProperty.UnsetValue ? Convert.ToChar(Convert.ToUInt32(value), culture) : '\0';
    }

    /// <summary>
    /// Converts the given value to <see cref="decimal"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="decimal"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="decimal"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ConvertToDecimal(object? value, IFormatProvider culture)
    {
        return value != AvaloniaProperty.UnsetValue ? Convert.ToDecimal(value, culture) : 0;
    }

    /// <summary>
    /// Converts the given value to <see cref="double"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="double"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ConvertToDouble(object? value, IFormatProvider culture)
    {
        return value != AvaloniaProperty.UnsetValue ? Convert.ToDouble(value, culture) : 0;
    }

    /// <summary>
    /// Converts the given value to <see cref="int"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="int"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="int"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ConvertToInt32(object? value, IFormatProvider culture)
    {
        return value != AvaloniaProperty.UnsetValue ? Convert.ToInt32(value, culture) : 0;
    }

    /// <summary>
    /// Converts the given value to <see cref="TimeSpan"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="TimeSpan"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value converted to <see cref="TimeSpan"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan ConvertToTimeSpan(object? value)
    {
        return value != AvaloniaProperty.UnsetValue && value is not null ? (TimeSpan)value : TimeSpan.Zero;
    }

    /// <summary>
    /// Converts the given value to <see cref="AngleSingle"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="AngleSingle"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value converted to <see cref="AngleSingle"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AngleSingle ConvertToAngleSingle(object? value)
    {
        return value != AvaloniaProperty.UnsetValue && value is not null ? (AngleSingle)value : default;
    }

    /// <summary>
    /// Converts the given value to <see cref="string"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to <see cref="String.Empty"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="string"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? ConvertToString(object? value, IFormatProvider culture)
    {
        return value != AvaloniaProperty.UnsetValue ? Convert.ToString(value, culture) : string.Empty;
    }

    /// <summary>
    /// Converts the given value to the given type.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the target type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to the target type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? ChangeType(object? value, Type targetType, IFormatProvider culture)
    {
        // Retrieve the underlying type if the target type is a nullable.
        return value != AvaloniaProperty.UnsetValue ? Convert.ChangeType(value, targetType, culture) : targetType.Default();
    }

    /// <summary>
    /// Tries to convert the given value to <see cref="bool"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="bool"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? TryConvertToBoolean(object? value, IFormatProvider culture)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToBoolean(value, culture) : null;
    }

    /// <summary>
    /// Tries to convert the given value to <see cref="char"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="char"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char? TryConvertToChar(object? value, IFormatProvider culture)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToChar(value, culture) : null;
    }

    /// <summary>
    /// Tries to convert the given value to <see cref="decimal"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="decimal"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal? TryConvertToDecimal(object? value, IFormatProvider culture)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToDecimal(value, culture) : null;
    }

    /// <summary>
    /// Tries to convert the given value to <see cref="double"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="double"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? TryConvertToDouble(object? value, IFormatProvider culture)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToDouble(value, culture) : null;
    }

    /// <summary>
    /// Tries to convert the given value to <see cref="int"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="int"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int? TryConvertToInt32(object? value, IFormatProvider culture)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToInt32(value, culture) : null;
    }

    /// <summary>
    /// Converts the given value to <see cref="TimeSpan"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="TimeSpan"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value converted to <see cref="TimeSpan"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan? TryConvertToTimeSpan(object? value)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToTimeSpan(value) : null;
    }

    /// <summary>
    /// Converts the given value to <see cref="AngleSingle"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/>, it converts to the default value of the <see cref="AngleSingle"/> type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value converted to <see cref="AngleSingle"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AngleSingle? TryConvertToAngleSingle(object? value)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToAngleSingle(value) : null;
    }

    /// <summary>
    /// Tries to convert the given value to <see cref="string"/>.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to <see cref="string"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? TryConvertToString(object? value, IFormatProvider culture)
    {
        return value != null && value != AvaloniaProperty.UnsetValue ? ConvertToString(value, culture) : null;
    }

    /// <summary>
    /// Tries to convert the given value to the given type.
    /// If the given value is <see cref="AvaloniaProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="culture">The format provider to use for the conversion.</param>
    /// <returns>The value converted to the target type if the conversion was possible, <c>Null</c> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? TryChangeType(object? value, Type targetType, IFormatProvider culture)
    {
        // Retrieve the underlying type if the target type is a nullable.
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return value != null && value != AvaloniaProperty.UnsetValue ? Convert.ChangeType(value, targetType, culture) : null;
    }
}
