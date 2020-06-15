// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Stride.Core.Annotations;

// ReSharper disable once CheckNamespace
namespace Stride
{
    public static class Utilities
    {
        private const string RegexReservedCharacters = @"[ \-;',+*|!`~@#$%^&\?()=[\]{}<>\""]";

        /// <summary>
        /// Build a valid C# class name from the provided string.
        /// It replaces all the forbidden characters by the provided replacement character.
        /// </summary>
        /// <param name="originalName">The original name</param>
        /// <param name="replacementCharacter">The replacement character</param>
        /// <returns></returns>
        [NotNull]
        public static string BuildValidClassName([NotNull] string originalName, char replacementCharacter = '_')
        {
            return BuildValidClassName(originalName, null, replacementCharacter);
        }

        /// <summary>
        /// Build a valid C# class name from the provided string.
        /// It replaces all the forbidden characters by the provided replacement character.
        /// </summary>
        /// <param name="originalName">The original name</param>
        /// <param name="additionalReservedWords">Reserved words that must be escaped if used directly</param>
        /// <param name="replacementCharacter">The replacement character</param>
        /// <returns></returns>
        [NotNull]
        public static string BuildValidClassName([NotNull] string originalName, IEnumerable<string> additionalReservedWords, char replacementCharacter = '_')
        {
            // C# identifiers must start with a letter or underscore
            if (char.IsLetter(originalName[0]) == false && originalName[0] != '_')
                originalName = "_" + originalName;
            
            if (ReservedNames.Contains(originalName))
                return originalName + replacementCharacter;

            if (additionalReservedWords != null && additionalReservedWords.Contains(originalName))
                return originalName + replacementCharacter;

            return Regex.Replace(originalName, $"{RegexReservedCharacters}|[.]", replacementCharacter.ToString());
        }

        /// <summary>
        /// Build a valid C# namespace name from the provided string.
        /// It replaces all the forbidden characters by the provided replacement character.
        /// </summary>
        /// <param name="originalName">The original name</param>
        /// <param name="replacementCharacter">The replacement character</param>
        /// <returns></returns>
        [NotNull]
        public static string BuildValidNamespaceName([NotNull] string originalName, char replacementCharacter = '_')
        {
            return BuildValidNamespaceName(originalName, null, replacementCharacter);
        }

        /// <summary>
        /// Build a valid C# namespace name from the provided string.
        /// It replaces all the forbidden characters by the provided replacement character.
        /// </summary>
        /// <param name="originalName">The original name</param>
        /// <param name="additionalReservedWords">Reserved words that must be escaped if used directly</param>
        /// <param name="replacementCharacter">The replacement character</param>
        /// <returns></returns>
        [NotNull]
        public static string BuildValidNamespaceName([NotNull] string originalName, IEnumerable<string> additionalReservedWords, char replacementCharacter = '_')
        {
            // C# identifiers must start with a letter or underscore
            if (char.IsLetter(originalName[0]) == false && originalName[0] != '_')
                originalName = "_" + originalName;

            if (ReservedNames.Contains(originalName))
                return originalName + replacementCharacter;

            if (additionalReservedWords != null && additionalReservedWords.Contains(originalName))
                return originalName + replacementCharacter;

            return Regex.Replace(originalName, $"{RegexReservedCharacters}|[.](?=[0-9])", replacementCharacter.ToString());
        }

        /// <summary>
        /// Build a valid C# project name from the provided string.
        /// It replaces all the forbidden characters by the provided replacement character.
        /// </summary>
        /// <param name="originalName">The original name</param>
        /// <param name="replacementCharacter">The replacement character</param>
        /// <returns></returns>
        [NotNull]
        public static string BuildValidProjectName([NotNull] string originalName, char replacementCharacter = '_')
        {
            return Regex.Replace(originalName, "[=;,/\\?:&*<>|#%\"]", replacementCharacter.ToString());
        }

        /// <summary>
        /// Build a valid file name from the provided string.
        /// It replaces all the forbidden characters by the provided replacement character.
        /// For reference see: https://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx
        /// </summary>
        /// <param name="originalName">The original name</param>
        /// <param name="replacementCharacter">The replacement character</param>
        /// <returns></returns>
        [NotNull]
        public static string BuildValidFileName([NotNull] string originalName, char replacementCharacter = '_')
        {
            return Regex.Replace(originalName, "[=;,/\\?:&!.*<>|#%\"]", replacementCharacter.ToString());
        }

        private static readonly string[] ReservedNames =
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
            "void",
            "volatile",
            "while",
        };
    }
}
