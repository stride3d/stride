// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Text;
using Stride.Core.Annotations;

namespace Stride.Core
{
    /// <summary>
    /// Extensions for <see cref="string"/> class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Safely trim a string.
        /// </summary>
        /// <param name="value">The string value. can be null</param>
        /// <returns>The string trimmed.May be null if string was null</returns>
        public static string SafeTrim(this string value)
        {
            return value?.Trim();
        }

        /// <summary>
        /// Calculates the index of a char inside the following <see cref="StringBuilder"/>, equivalent of
        /// <see cref="string.IndexOf(char)"/> for a StringBuilder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="testChar">The test character.</param>
        /// <returns>A position to the character found, or -1 if not found.</returns>
        /// <exception cref="System.ArgumentNullException">builder</exception>
        public static int IndexOf([NotNull] this StringBuilder builder, char testChar)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            for (var i = 0; i < builder.Length; i++)
            {
                if (builder[i] == testChar)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Calculates the last index of a char inside the following <see cref="StringBuilder" />, equivalent of
        /// <see cref="string.LastIndexOf(char)" /> for a StringBuilder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="testChar">The test character.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>A position to the character found, or -1 if not found.</returns>
        /// <exception cref="System.ArgumentNullException">builder</exception>
        public static int LastIndexOf([NotNull] this StringBuilder builder, char testChar, int startIndex = 0)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            startIndex = startIndex < 0 ? 0 : startIndex;
            for (var i = builder.Length - 1; i >= startIndex; i--)
            {
                if (builder[i] == testChar)
                {
                    return i;
                }
            }
            return -1;
        }

        [NotNull]
        public static string Substring([NotNull] this StringBuilder builder, int startIndex)
        {
            return builder.ToString(startIndex, builder.Length - startIndex);
        }

        [NotNull]
        public static string Substring([NotNull] this StringBuilder builder, int startIndex, int length)
        {
            return builder.ToString(startIndex, length);
        }

        /// <summary>
        /// Determines whether the end of this string ends by the specified character.
        /// </summary>
        /// <param name="stringToTest">The string automatic test.</param>
        /// <param name="endChar">The end character.</param>
        /// <returns><c>true</c> if the end of this string ends by the specified character, <c>false</c> otherwise.</returns>
        public static bool EndsWith([NotNull] this string stringToTest, char endChar)
        {
            if (stringToTest == null) throw new ArgumentNullException(nameof(stringToTest));
            return stringToTest.Length > 0 && endChar == stringToTest[stringToTest.Length - 1];
        }

        /// <summary>
        /// Determines whether the end of this string ends by the specified characters.
        /// </summary>
        /// <param name="stringToTest">The string automatic test.</param>
        /// <param name="endChars">The end characters.</param>
        /// <returns><c>true</c> if the end of this string ends by the specified character, <c>false</c> otherwise.</returns>
        public static bool EndsWith([NotNull] this string stringToTest, [NotNull] params char[] endChars)
        {
            if (stringToTest == null) throw new ArgumentNullException(nameof(stringToTest));
            if (endChars == null) throw new ArgumentNullException(nameof(endChars));
            return stringToTest.Length > 0 && endChars.Contains(stringToTest[stringToTest.Length - 1]);
        }

        /// <summary>
        /// Extension to format a string using <see cref="string.Format(string,object)"/> method by allowing to use it directly on a string.
        /// </summary>
        /// <param name="stringToFormat">The string automatic format.</param>
        /// <param name="argumentsToFormat">The arguments automatic format.</param>
        /// <returns>A formatted string. See <see cref="string.Format(string,object)"/> </returns>
        [NotNull]
        public static string ToFormat([NotNull] this string stringToFormat, [NotNull] params object[] argumentsToFormat)
        {
            return string.Format(stringToFormat, argumentsToFormat);
        }

        /// <summary>
        /// Reports the index number, or character position, of the first occurrence of the specified Unicode character in the current String object.
        /// The search starts at a specified character position starting from the end and examines a specified number of character positions.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="charToFind">The character automatic find.</param>
        /// <param name="matchCount">The number of match before stopping. Default is 1</param>
        /// <returns>The character position of the value parameter for the specified character if it is found, or -1 if it is not found.</returns>
        /// <exception cref="System.ArgumentNullException">text</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">matchCount;matchCount must be >= 1</exception>
        public static int IndexOfReverse([NotNull] this string text, char charToFind, int matchCount = 1)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (matchCount < 1) throw new ArgumentOutOfRangeException(nameof(matchCount), "matchCount must be >= 1");

            for (var i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == charToFind && (--matchCount) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Reports the index number, or character position, of the first occurrence of the specified Unicode character in the current String object.
        /// The search starts at a specified character position starting from the end and examines a specified number of character positions.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="charToFind">The character automatic find.</param>
        /// <param name="startIndexFromEnd">The starting index number for the search relative to the end of the string.</param>
        /// <param name="count">The number of character positions to be examined.</param>
        /// <param name="matchCount">The number of match before stopping. Default is 1</param>
        /// <returns>The character position of the value parameter for the specified character if it is found, or -1 if it is not found.</returns>
        /// <exception cref="System.ArgumentNullException">text</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// count;Count must be a positive value
        /// or
        /// startIndexFromEnd;StartIndexFromEnd must be a positive value
        /// or
        /// startIndexFromEnd;StartIndexFromEnd must be within the range of the string length
        /// or
        /// count;Count must be in the range of the string length minus the startIndexFromEnd
        /// </exception>
        public static int IndexOfReverse([NotNull] this string text, char charToFind, int startIndexFromEnd, int count, int matchCount = 1)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length == 0 || count == 0) return -1;
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be a positive value");
            if (startIndexFromEnd < 0) throw new ArgumentOutOfRangeException(nameof(startIndexFromEnd), "StartIndexFromEnd must be a positive value");
            if (startIndexFromEnd > text.Length - 1) throw new ArgumentOutOfRangeException(nameof(startIndexFromEnd), "StartIndexFromEnd must be within the range of the string length");
            if (count > (text.Length - startIndexFromEnd)) throw new ArgumentOutOfRangeException(nameof(count), "Count must be in the range of the string length minus the startIndexFromEnd");
            if (matchCount < 1) throw new ArgumentOutOfRangeException(nameof(matchCount), "matchCount must be >= 1");

            var startIndex = text.Length - startIndexFromEnd - 1;
            var endIndex = startIndex - count + 1;

            for (var i = startIndex; i >= endIndex; i--)
            {
                if (text[i] == charToFind && (--matchCount) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Indicate if the string contains a character.
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="value">The character to look for.</param>
        /// <returns>A boolean indicating if at least one instance of <paramref name="value"/> is present in <paramref name="text"/></returns>
        public static bool Contains([NotNull] this string text, char value)
        {
            return text.Contains(new string(value, 1));
        }
    }
}
