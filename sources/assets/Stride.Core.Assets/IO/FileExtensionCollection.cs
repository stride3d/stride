// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Stride.Core.Assets.IO
{
    /// <summary>
    /// A class describing a collection of file extensions, with a facultative description string.
    /// </summary>
    public sealed class FileExtensionCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileExtensionCollection"/> class.
        /// </summary>
        /// <param name="extensions">The extensions to add in this collection. Extensions must be separated with the semi-colon character (;).</param>
        public FileExtensionCollection(string extensions)
            : this(null, extensions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileExtensionCollection"/> class.
        /// </summary>
        /// <param name="description">The description of this file extension collection. Can be null.</param>
        /// <param name="extensions">The extensions to add in this collection. Extensions must be separated with the semi-colon character (;).</param>
        /// <param name="additionalExtensions">Additional extensions to add in this collection. Extensions inside each string must be separated with the semi-colon character (;).</param>
        public FileExtensionCollection(string description, string extensions, params string[] additionalExtensions)
        {
            var sb = new StringBuilder(extensions);
            if (additionalExtensions != null)
            {
                foreach (var nextExtensions in additionalExtensions)
                {
                    sb.Append(';');
                    sb.Append(nextExtensions);
                }
            }
            var allExtensions = sb.ToString();
            SingleExtensions = SplitExtensions(allExtensions);
            ConcatenatedExtensions = string.Join(";", SingleExtensions);
            Description = description;
        }

        /// <summary>
        /// Gets the description of this file extension collection.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets each extension in this collection individually splitted in an <see cref="IEnumerable{String}"/>.
        /// </summary>
        public IEnumerable<string> SingleExtensions { get; }

        /// <summary>
        /// Gets a string containing all extensions separated with the semi-colon character (;).
        /// </summary>
        public string ConcatenatedExtensions { get; }

        /// <summary>
        /// Indicates whether the given extension matches any of the extension in this collection.
        /// </summary>
        /// <param name="extension">The extension to match. Can contain wildcards.</param>
        /// <returns>True if the given extension matches, false otherwise.</returns>
        public bool Contains(string extension)
        {
            var normalized = NormalizeExtension(extension);
            var pattern = new Regex(normalized.Replace(".", "[.]").Replace("*", ".*"));
            return SingleExtensions.Any(x => pattern.IsMatch(x) || new Regex(x.Replace(".", "[.]").Replace("*", ".*")).IsMatch(normalized));
        }

        private static List<string> SplitExtensions(string extensions)
        {
            return extensions.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(NormalizeExtension).ToList();
        }

        private static string NormalizeExtension(string extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            if (extension.Contains(";") || extension.Contains(","))
                throw new ArgumentException("Expecting a single extension");

            if (extension.StartsWith("*."))
            {
                extension = extension.Substring(1);
            }
            if (extension.Any(x => x != '*' & Path.GetInvalidFileNameChars().Contains(x)))
                throw new ArgumentException("Extension contains invalid characters");

            if (!extension.StartsWith("."))
            {
                extension = $".{extension}";
            }
            return extension.ToLowerInvariant();
        }
    }
}
