// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Extensions
{
    public static class StringExtensions
    {
        [NotNull]
        public static List<string> CamelCaseSplit([NotNull] this string str)
        {
            var result = new List<string>();
            var wordStart = 0;
            var wordLength = 0;
            var prevChar = '\0';

            foreach (var currentChar in str)
            {
                if (prevChar != '\0')
                {
                    // Split white spaces
                    if (char.IsWhiteSpace(currentChar))
                    {
                        var word = str.Substring(wordStart, wordLength);
                        result.Add(word);
                        wordStart += wordLength;
                        wordLength = 0;
                    }

                    // aA -> split between a and A
                    if (char.IsLower(prevChar) && char.IsUpper(currentChar))
                    {
                        var word = str.Substring(wordStart, wordLength);
                        result.Add(word);
                        wordStart += wordLength;
                        wordLength = 0;
                    }
                    // This will manage abbreviation words that does not contain lower case character: ABCDef should split into ABC and Def
                    if (char.IsUpper(prevChar) && char.IsLower(currentChar) && wordLength > 1)
                    {
                        var word = str.Substring(wordStart, wordLength - 1);
                        result.Add(word);
                        wordStart += wordLength - 1;
                        wordLength = 1;
                    }
                }
                ++wordLength;
                prevChar = currentChar;
            }

            result.Add(str.Substring(wordStart, wordLength));

            return result.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        }

        /// <summary>
        /// Returns a new string in which the last occurrence of <paramref name="oldValue"/> in the current instance is replaced with <paramref name="newValue"/>.
        /// </summary>
        /// <param name="str">The current string.</param>
        /// <param name="oldValue">The string to be replaced.</param>
        /// <param name="newValue">The string to replace the last occurrence of <paramref name="oldValue"/>.</param>
        /// <returns>A string that is equivalent to the current string except that the last occurrence of <paramref name="oldValue"/> is replaced with <paramref name="newValue"/>.</returns>
        /// <remarks>If <paramref name="oldValue"/> is not found in the current instance, the method returns the current instance unchanged.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="oldValue"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="oldValue"/> is an empty string ("").</exception>
        [NotNull, Pure]
        public static string ReplaceLast([NotNull] this string str, [NotNull] string oldValue, [NotNull] string newValue)
        {
            if (oldValue == null) throw new ArgumentNullException(nameof(oldValue));
            if (oldValue.Length == 0) throw new ArgumentException($"{oldValue} can't be an empty string", nameof(oldValue));

            var startIndex = str.LastIndexOf(oldValue, StringComparison.CurrentCulture);
            return startIndex != -1 ? str.Remove(startIndex, oldValue.Length).Insert(startIndex, newValue) : str;
        }

        public static bool MatchCamelCase(this string inputText, string text)
        {
            var camelCaseSplit = text.CamelCaseSplit();
            var filter = inputText.ToLowerInvariant();
            var currentFilterChar = 0;

            foreach (var word in camelCaseSplit)
            {
                var currentWordChar = 0;
                while (currentFilterChar > 0)
                {
                    if (char.ToLower(word[currentWordChar]) == filter[currentFilterChar])
                        break;
                    --currentFilterChar;
                }

                while (char.ToLower(word[currentWordChar]) == filter[currentFilterChar])
                {
                    ++currentWordChar;
                    ++currentFilterChar;
                    if (currentFilterChar == filter.Length)
                        return true;

                    if (currentWordChar == word.Length)
                        break;
                }
            }
            return currentFilterChar == filter.Length;
        }
    }
}
