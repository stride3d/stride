// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Stride.Rendering
{
    /// <summary>
    ///   Provides extension methods for <see cref="ParameterCollection"/>.
    /// </summary>
    public static class ParameterCollectionExtensions
    {
        /// <summary>
        ///   Generates a detailed string representation of the permutation parameters within
        ///   a parameter collection.
        /// </summary>
        /// <param name="parameterCollection">The parameter collection to process.</param>
        /// <returns>
        ///   A string detailing the permutation parameters and their values.
        ///   Each parameter is prefixed with <c>"@P"</c>.
        /// </returns>
        public static string ToStringPermutationsDetailed(this ParameterCollection parameterCollection)
        {
            var builder = new StringBuilder();

            var first = true;
            foreach (var usedParam in parameterCollection.ParameterKeyInfos)
            {
                // Ignore any non-permutation key
                if (usedParam.Key.Type != ParameterKeyType.Permutation)
                    continue;

                builder.Append("@P ");
                if (first)
                {
                    builder.Append("  - ");
                    first = false;
                }

                builder.Append(usedParam.Key.ToString() ?? "null");
                builder.Append(": ");

                var value = parameterCollection.ObjectValues[usedParam.BindingSlot];
                builder.AppendLine(value switch
                {
                    null => "null",

                    Array or IList => string.Join(", ", (IEnumerable<object>) value),
                    _ => value.ToString()
                });
            }

            return builder.ToString();
        }
    }
}
