// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Extensions
{
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
        [NotNull]
        public static IEnumerable<Enum> GetIndividualFlags([NotNull] Type enumType)
        {
            foreach (var value in Enum.GetValues(enumType).Cast<Enum>())
            {
                ulong flag = 0x1;

                var bits = Convert.ToUInt64(value);
                if (bits == 0L)
                    continue; // skip the zero value

                while (flag < bits)
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
        [NotNull]
        public static IEnumerable<Enum> GetAllFlags([NotNull] this Enum value)
        {
            return GetFlags(value, Enum.GetValues(value.GetType()).Cast<Enum>().ToList());
        }

        /// <summary>
        /// Returns all the individual flags that are contained in the given value, excluding zero flags and flags that contains more than a single bit.
        /// </summary>
        /// <param name="value">The value for which to return matching flags</param>
        /// <returns>An enumerable of all the flags that are contained in the given value.</returns>
        [NotNull]
        public static IEnumerable<Enum> GetIndividualFlags([NotNull] this Enum value)
        {
            return GetFlags(value, GetIndividualFlags(value.GetType()).ToList());
        }

        /// <summary>
        /// Returns an enum value of all the given flags set together with the bitwise OR operator.
        /// </summary>
        /// <param name="enumType">The type of enum.</param>
        /// <param name="flags">The list of flags to set together.</param>
        /// <returns></returns>
        [NotNull]
        public static Enum GetEnum([NotNull] Type enumType, [NotNull] IEnumerable<Enum> flags)
        {
            var value = flags.Select(Convert.ToUInt64).Aggregate<ulong, ulong>(0, (current, bits) => current | bits);
            return (Enum)Enum.ToObject(enumType, value);
        }

        /// <summary>
        /// Returns all the flags from the given list of flags that are contained in the given value, using the bitwise AND operator.
        /// </summary>
        /// <param name="value">The value for which to return matching flags</param>
        /// <param name="flags">The list of flags to test.</param>
        /// <returns>An enumerable of flags from the list of flags that are contained in the given value.</returns>
        [NotNull]
        private static IEnumerable<Enum> GetFlags(Enum value, IList<Enum> flags)
        {
            var bits = Convert.ToUInt64(value);
            // Empty flag enum
            if (bits == 0L)
                return Enumerable.Empty<Enum>();

            var results = new List<Enum>();
            for (var i = flags.Count - 1; i >= 0; i--)
            {
                var mask = Convert.ToUInt64(flags[i]);
                if (mask == 0L)
                    continue;

                if ((bits & mask) == mask)
                {
                    results.Add(flags[i]);
                }
            }

            return results.Reverse<Enum>();
        }
    }
}
