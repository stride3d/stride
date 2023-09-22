using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Represents a programming language supported by the system.
    /// </summary>
    /// <param name="Name">The name of the programming language.</param>
    /// <param name="Extension">The associated project file extension.</param>
    public record SupportedProgrammingLanguage(string Name, string Extension);

    /// <summary>
    /// Provides a list of programming languages supported by the system.
    /// </summary>
    public static class SupportedProgrammingLanguages
    {
        /// <summary>
        /// Gets the list of supported programming languages.
        /// </summary>
        /// <value>
        /// A list of <see cref="SupportedProgrammingLanguage"/> objects.
        /// </value>
        /// <remarks>
        /// Use this list to check for supported programming languages when needed.
        /// The list is initialized with common programming languages and their respective project file extensions.
        /// </remarks>
        public static IReadOnlyList<SupportedProgrammingLanguage> Languages { get; } = new List<SupportedProgrammingLanguage>
        {
            new SupportedProgrammingLanguage("C#", ".csproj"),
            new SupportedProgrammingLanguage("F#", ".fsproj"),
            new SupportedProgrammingLanguage("VB", ".vbproj"),
        };

        /// <summary>
        /// Determines if a given project file extension is supported.
        /// </summary>
        /// <param name="extension">The project file extension to check.</param>
        /// <returns>True if the extension is supported, otherwise false.</returns>
        public static bool IsProjectExtensionSupported(string extension)
        {
            return Languages.Any(lang => string.Equals(lang.Extension, extension, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
