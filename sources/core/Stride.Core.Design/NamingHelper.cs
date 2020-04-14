// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core
{
    /// <summary>
    /// Helper to verify naming conventions.
    /// </summary>
    public static class NamingHelper
    {
        private static readonly Regex MatchIdentifier = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$");
        private const string DefaultNamePattern = "{0} ({1})";

        /// <summary>
        /// Delegate to test if the specified location is already used.
        /// </summary>
        /// <param name="location">The location to try to use.</param>
        /// <returns><c>true</c> if the specified location is already used, <c>false</c> otherwise.</returns>
        public delegate bool ContainsLocationDelegate(UFile location);

        /// <summary>
        /// Determines whether the specified string is valid namespace identifier.
        /// </summary>
        /// <param name="text">The namespace text.</param>
        /// <returns><c>true</c> if is a valid namespace identifier; otherwise, <c>false</c>.</returns>
        public static bool IsValidNamespace([NotNull] string text)
        {
            string error;
            return IsValidNamespace(text, out error);
        }

        /// <summary>
        /// Determines whether the specified string is valid namespace identifier.
        /// </summary>
        /// <param name="text">The namespace text.</param>
        /// <param name="error">The error if return is false.</param>
        /// <returns><c>true</c> if is a valid namespace identifier; otherwise, <c>false</c>.</returns>
        public static bool IsValidNamespace([NotNull] string text, out string error)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Namespace cannot be empty";
            }
            else
            {
                var items = text.Split(new[] { '.' }, StringSplitOptions.None);
                error = items.Where(s => !IsIdentifier(s)).Select(item => $"[{item}]").FirstOrDefault();
            }
            return error == null;
        }

        /// <summary>
        /// Determines whether the specified text is a C# identifier.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if the specified text is an identifier; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">text</exception>
        public static bool IsIdentifier([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return MatchIdentifier.Match(text).Success;
        }

        /// <summary>
        /// Generate a name for a new object that is guaranteed to be unique among a collection of existing objects. To generate such name, a base name and a pattern for variations must be provided.
        /// </summary>
        /// <typeparam name="T">The type of object in the collection of existing object.</typeparam>
        /// <param name="baseName">The base name used to generate the new name. If the name is available in the collection, it will be returned as-is. Otherwise, a name following the given pattern will be returned.</param>
        /// <param name="existingItems">The collection of existing items, used to verify that the name being generated is not already used.</param>
        /// <param name="existingNameFunc">A function used to extract the name of an object of the given collection. If null, the <see cref="object.ToString"/> method will be used.</param>
        /// <param name="namePattern">The pattern used to generate the new name, when the base name is unavailable. This pattern must contains the token '{0}' that will be replaced by the base name, and the token '{1}' that will be replaced by the smallest numerical value that can generate an available name, starting from 2. If null, <see cref="DefaultNamePattern"/> will be used instead.</param>
        /// <returns><see cref="baseName"/> if no item of <see cref="existingItems"/> returns this value through <see cref="existingNameFunc"/>. Otherwise, a string formatted with <see cref="namePattern"/>, using <see cref="baseName"/> as token '{0}' and the smallest numerical value that can generate an available name, starting from 2</returns>
        [NotNull]
        public static string ComputeNewName<T>([NotNull] string baseName, [NotNull] IEnumerable<T> existingItems, Func<T, string> existingNameFunc = null, string namePattern = null)
        {
            if (existingItems == null) throw new ArgumentNullException(nameof(existingItems));
            if (existingNameFunc == null)
                existingNameFunc = x => x.ToString();

            var existingNames = new HashSet<string>(existingItems.Select(existingNameFunc).Select(x => x.ToUpperInvariant()));
            return ComputeNewName(baseName, url => existingNames.Contains(url.ToString().ToUpperInvariant()), namePattern);
        }

        /// <summary>
        /// Generate a name for a new object that is guaranteed to be unique for the provided "contains predicate". To generate such name, a base name and a pattern for variations must be provided.
        /// </summary>
        /// <param name="baseName">The base name used to generate the new name. If the name is available in the collection, it will be returned as-is. Otherwise, a name following the given pattern will be returned.</param>
        /// <param name="containsDelegate">The delegate used to determine if the asset is already existing</param>
        /// <param name="namePattern">The pattern used to generate the new name, when the base name is unavailable. This pattern must contains the token '{0}' that will be replaced by the base name, and the token '{1}' that will be replaced by the smallest numerical value that can generate an available name, starting from 2. If null, <see cref="DefaultNamePattern"/> will be used instead.</param>
        /// <returns><see cref="baseName"/> if the "contains predicate" returns false. Otherwise, a string formatted with <see cref="namePattern"/>, using <see cref="baseName"/> as token '{0}' and the smallest numerical value that can generate an available name, starting from 2</returns>
        [NotNull]
        public static string ComputeNewName([NotNull] string baseName, [NotNull] ContainsLocationDelegate containsDelegate, string namePattern = null)
        {
            if (baseName == null) throw new ArgumentNullException(nameof(baseName));
            if (containsDelegate == null) throw new ArgumentNullException(nameof(containsDelegate));
            if (namePattern == null) namePattern = DefaultNamePattern;
            if (!namePattern.Contains("{0}") || !namePattern.Contains("{1}")) throw new ArgumentException(@"This parameter must be a formattable string containing '{0}' and '{1}' tokens", nameof(namePattern));

            // First check if the base name itself is ok
            if (!containsDelegate(baseName))
                return baseName;

            // Initialize counter
            var counter = 1;
            // Checks whether baseName already 'implements' the namePattern
            var match = Regex.Match(baseName, $"^{Regex.Escape(namePattern).Replace(@"\{0}", @"(.*)").Replace(@"\{1}", @"(\d+)")}$");
            if (match.Success && match.Groups.Count >= 3)
            {
                // if so, extract the base name and the current counter
                var intValue = int.Parse(match.Groups[2].Value);
                // Ensure there is no leading 0 messing around
                if (intValue.ToString() == match.Groups[2].Value)
                {
                    baseName = match.Groups[1].Value;
                    counter = intValue;
                }
            }
            // Compute name
            string result;
            do
            {
                result = string.Format(namePattern, baseName, ++counter);
            }
            while (containsDelegate(result));
            return result;
        }
    }
}
