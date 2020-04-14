// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// Helper class with similar methods than <see cref="Convert"/> but returns the default value of the expected type if value is <see cref="DependencyProperty.UnsetValue"/>.
    /// </summary>
    public static class ConverterHelper
    {
        /// <summary>
        /// Converts the given value to <see cref="Boolean"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="Boolean"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Boolean"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ConvertToBoolean(object value, IFormatProvider culture)
        {
            return value != DependencyProperty.UnsetValue && Convert.ToBoolean(value, culture);
        }

        /// <summary>
        /// Converts the given value to <see cref="Char"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="Char"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ConvertToChar(object value, IFormatProvider culture)
        {
            return value != DependencyProperty.UnsetValue ? Convert.ToChar(Convert.ToUInt32(value), culture) : default(char);
        }

        /// <summary>
        /// Converts the given value to <see cref="Double"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="Double"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Double"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ConvertToDouble(object value, IFormatProvider culture)
        {
            return value != DependencyProperty.UnsetValue ? Convert.ToDouble(value, culture) : default(double);
        }

        /// <summary>
        /// Converts the given value to <see cref="Int32"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="Int32"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Int32"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ConvertToInt32(object value, IFormatProvider culture)
        {
            return value != DependencyProperty.UnsetValue ? Convert.ToInt32(value, culture) : default(int);
        }

        /// <summary>
        /// Converts the given value to <see cref="TimeSpan"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="TimeSpan"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="TimeSpan"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan ConvertToTimeSpan(object value, IFormatProvider culture)
        {
            return value != DependencyProperty.UnsetValue ? (TimeSpan)value : default(TimeSpan);
        }

        /// <summary>
        /// Converts the given value to <see cref="AngleSingle"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="AngleSingle"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="AngleSingle"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AngleSingle ConvertToAngleSingle(object value, IFormatProvider culture)
        {
            return value != DependencyProperty.UnsetValue ? (AngleSingle)value : default(AngleSingle);
        }

        /// <summary>
        /// Converts the given value to <see cref="String"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to <see cref="String.Empty"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="String"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ConvertToString(object value, IFormatProvider culture)
        {
            return value != DependencyProperty.UnsetValue ? Convert.ToString(value, culture) : string.Empty;
        }

        /// <summary>
        /// Converts the given value to the given type.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the target type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to the target type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object ChangeType(object value, [NotNull] Type targetType, IFormatProvider culture)
        {
            // Retrieve the underlying type if the target type is a nullable.
            return value != DependencyProperty.UnsetValue ? Convert.ChangeType(value, targetType, culture) : targetType.Default();            
        }

        /// <summary>
        /// Tries to convert the given value to <see cref="Boolean"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Boolean"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool? TryConvertToBoolean(object value, IFormatProvider culture)
        {
            return value != null && value != DependencyProperty.UnsetValue ? (bool?)ConvertToBoolean(value, culture) : null;
        }

        /// <summary>
        /// Tries to convert the given value to <see cref="Char"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Char"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char? TryConvertToChar(object value, IFormatProvider culture)
        {
            return value != null && value != DependencyProperty.UnsetValue ? (char?)ConvertToChar(value, culture) : null;
        }

        /// <summary>
        /// Tries to convert the given value to <see cref="Double"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Double"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double? TryConvertToDouble(object value, IFormatProvider culture)
        {
            return value != null && value != DependencyProperty.UnsetValue ? (double?)ConvertToDouble(value, culture) : null;
        }

        /// <summary>
        /// Tries to convert the given value to <see cref="Int32"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="Int32"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int? TryConvertToInt32(object value, IFormatProvider culture)
        {
            return value != null && value != DependencyProperty.UnsetValue ? (int?)ConvertToInt32(value, culture) : null;
        }

        /// <summary>
        /// Converts the given value to <see cref="TimeSpan"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="TimeSpan"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="TimeSpan"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan? TryConvertToTimeSpan(object value, IFormatProvider culture)
        {
            return value != null && value != DependencyProperty.UnsetValue ? (TimeSpan?)ConvertToTimeSpan(value, culture) : null;
        }

        /// <summary>
        /// Converts the given value to <see cref="AngleSingle"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/>, it converts to the default value of the <see cref="AngleSingle"/> type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="AngleSingle"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AngleSingle? TryConvertToAngleSingle(object value, IFormatProvider culture)
        {
            return value != null && value != DependencyProperty.UnsetValue ? (AngleSingle?)ConvertToAngleSingle(value, culture) : null;
        }

        /// <summary>
        /// Tries to convert the given value to <see cref="String"/>.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to <see cref="String"/> if the conversion was possible, <c>Null</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string TryConvertToString(object value, IFormatProvider culture)
        {
            return value != null && value != DependencyProperty.UnsetValue ? ConvertToString(value, culture) : null;
        }

        /// <summary>
        /// Tries to convert the given value to the given type.
        /// If the given value is <see cref="DependencyProperty.UnsetValue"/> or <c>Null</c>, then <c>Null</c> is returned.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="culture">The format provider to use for the conversion.</param>
        /// <returns>The value converted to the target type if the conversion was possible, <c>Null</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object TryChangeType(object value, Type targetType, IFormatProvider culture)
        {
            // Retrieve the underlying type if the target type is a nullable.
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return value != null && value != DependencyProperty.UnsetValue ? Convert.ChangeType(value, targetType, culture) : null;
        }
    }
}
