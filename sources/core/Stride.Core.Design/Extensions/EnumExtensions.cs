// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Extensions;

/// <summary>
/// Helper functions to process enum flags.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Returns an enumerable of all values in the flag enum, excluding values of zero and values matching multiple bytes.
    /// </summary>
    /// <param name="enumType">The type of flag enum.</param>
    /// <returns>An enumerable of all values in the flag enum, excluding values of zero and values matching multiple bytes.</returns>
    public static IEnumerable<Enum> GetIndividualFlags(Type enumType)
    {
        foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
        {
            ulong flag = 0x1;

            var bits = ToUInt64Bits(value);
            if (bits == 0L)
                continue; // skip the zero value

            while (flag < bits && flag != 0)
                flag <<= 1;

            if (flag == bits)
                yield return value;
        }
    }
    
    /// <summary>
    /// Returns all the flags that are contained in the given value, including zero flags and flags that contains more than a single bit.
    /// </summary>
    /// <param name="value">The value for which to return matching flags</param>
    /// <returns>An enumerable of all the flags that are contained in the given value.</returns>
    public static IEnumerable<Enum> GetAllFlags(this Enum value)
    {
        return GetFlags(value, Enum.GetValues(value.GetType()).Cast<Enum>().ToList());
    }

    /// <summary>
    /// Returns all the individual flags that are contained in the given value, excluding zero flags and flags that contains more than a single bit.
    /// </summary>
    /// <param name="value">The value for which to return matching flags</param>
    /// <returns>An enumerable of all the flags that are contained in the given value.</returns>
    public static IEnumerable<Enum> GetIndividualFlags(this Enum value)
    {
        return GetFlags(value, GetIndividualFlags(value.GetType()).ToList());
    }

    /// <summary>
    /// Returns an enum value of all the given flags set together with the bitwise OR operator.
    /// </summary>
    /// <param name="enumType">The type of enum.</param>
    /// <param name="flags">The list of flags to set together.</param>
    /// <returns></returns>
    public static Enum GetEnum(Type enumType, IEnumerable<Enum> flags)
    {
        var value = flags.Select(ToUInt64Bits).Aggregate<ulong, ulong>(0, (current, bits) => current | bits);
        return (Enum)Enum.ToObject(enumType, value);
    }

    /// <summary>
    /// Returns all the flags from the given list of flags that are contained in the given value, using the bitwise AND operator.
    /// </summary>
    /// <param name="value">The value for which to return matching flags</param>
    /// <param name="flags">The list of flags to test.</param>
    /// <returns>An enumerable of flags from the list of flags that are contained in the given value.</returns>
    private static IEnumerable<Enum> GetFlags(Enum value, IList<Enum> flags)
    {
        var bits = ToUInt64Bits(value);
        // Empty flag enum
        if (bits == 0L)
            return Enumerable.Empty<Enum>();

        var results = new List<Enum>();
        for (var i = flags.Count - 1; i >= 0; i--)
        {
            var mask = ToUInt64Bits(flags[i]);
            if (mask == 0L)
                continue;

            if ((bits & mask) == mask)
            {
                results.Add(flags[i]);
            }
        }

        return results.Reverse<Enum>();
    }

    private static ulong ToUInt64Bits(Enum value)
    {
        return value.GetTypeCode() switch
        {
            TypeCode.SByte => unchecked((byte)Convert.ToSByte(value)),
            TypeCode.Int16 => unchecked((ushort)Convert.ToInt16(value)),
            TypeCode.Int32 => unchecked((uint)Convert.ToInt32(value)),
            TypeCode.Int64 => unchecked((ulong)Convert.ToInt64(value)),
            _ => Convert.ToUInt64(value)
        };
    }
}
