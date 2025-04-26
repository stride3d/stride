
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

/// <summary>
/// A helper class that enables converting values specified in markup (strings) to their object representation.
/// </summary>
internal static class TypeConverterHelper
{
    /// <summary>
    /// Converts string representation of a value to its object representation.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="destinationType">The destination type.</param>
    /// <returns>Object representation of the string value.</returns>
    /// <exception cref="ArgumentNullException">destinationType cannot be null.</exception>
    public static object? Convert(string value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type destinationType)
    {
        TryConvert(value, destinationType, out var result);
        return result;
    }

    /// <summary>
    /// Try to convert the string representation of a value to its object representation.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="destinationType">The destination type.</param>
    /// <param name="result">When successful, the object representation of the string value; otherwise, <c>null</c></param>
    /// <returns><c>true</c> if the value was sucessfully converted; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">destinationType cannot be null.</exception>
    public static bool TryConvert(string value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type destinationType, out object? result)
    {
        if (destinationType is null)
        {
            throw new ArgumentNullException(nameof(destinationType));
        }

        result = null;
        var destinationTypeFullName = destinationType.FullName;
        if (destinationTypeFullName is null)
        {
            return false;
        }

        var scope = GetScope(destinationTypeFullName);

        // Value types in the "System" namespace must be special cased due to a bug in the xaml compiler
        if (string.Equals(scope, "System", StringComparison.Ordinal))
        {
            if (string.Equals(destinationTypeFullName, typeof(string).FullName, StringComparison.Ordinal))
            {
                result = value;
                return true;
            }

            if (string.Equals(destinationTypeFullName, typeof(bool).FullName, StringComparison.Ordinal))
            {
                result = bool.Parse(value);
                return true;
            }

            if (string.Equals(destinationTypeFullName, typeof(int).FullName, StringComparison.Ordinal))
            {
                result = int.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }

            if (string.Equals(destinationTypeFullName, typeof(double).FullName, StringComparison.Ordinal))
            {
                result = double.Parse(value, CultureInfo.InvariantCulture);
                return true;
            }
        }

        try
        {
            if (destinationType.BaseType == typeof(Enum))
            {
                result = Enum.Parse(destinationType, value);
                return true;
            }

            if (destinationType.GetInterfaces().Any(t => t == typeof(IConvertible)))
            {
                result = (value as IConvertible).ToType(destinationType, CultureInfo.InvariantCulture);
                return true;
            }

            var converter = TypeDescriptor.GetConverter(destinationType);
            result = converter.ConvertFromInvariantString(value);
            return true;
        }
        catch (ArgumentException)
        {
            // not an enum
        }
        catch (InvalidCastException)
        {
            // not able to convert to anything
        }
        catch (FormatException)
        {
            // not able to convert from string
        }
        catch (NotSupportedException)
        {
            // not able to convert from string
        }

        return false;
    }

    private static string GetScope(string name)
    {
        var indexOfLastPeriod = name.LastIndexOf('.');
#if !NET6_0_OR_GREATER
        if (indexOfLastPeriod != name.Length - 1)
        {
            return name.Substring(0, indexOfLastPeriod);
        }

        return name;
#else
        return indexOfLastPeriod != name.Length - 1 ? name[..indexOfLastPeriod] : name;
#endif
    }
}
