// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Selectors
{
    /// <summary>
    /// Matches asset depending on their URL, using a gitignore-like format (based on fnmatch()).
    /// </summary>
    [DataContract("PathSelector")]
    public class PathSelector : AssetSelector
    {
        private KeyValuePair<string, Regex>[] regexes;

        public PathSelector()
        {
            Paths = new List<string>();
        }

        /// <summary>
        /// Gets or sets the paths (gitignore format).
        /// </summary>
        /// <value>
        /// The paths (gitignore format).
        /// </value>
        public List<string> Paths { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<string> Select(PackageSession packageSession, IContentIndexMap contentIndexMap)
        {
            // Check if we need to create or regenerate regex.
            bool needGenerateRegex = false;
            if (regexes == null || regexes.Length != Paths.Count)
            {
                needGenerateRegex = true;
            }
            else
            {
                // Check used pattern
                for (int i = 0; i < Paths.Count; ++i)
                {
                    if (Paths[i] != regexes[i].Key)
                    {
                        needGenerateRegex = true;
                        break;
                    }
                }
            }

            // Transform gitignore patterns to regex.
            if (needGenerateRegex)
                regexes = Paths.Select(x => new KeyValuePair<string, Regex>(x, new Regex(TransformToRegex(x)))).ToArray();

            return contentIndexMap.GetMergedIdMap()
                .Select(asset => asset.Key) // Select url
                .Where(assetUrl => regexes.Any(regex => regex.Value.IsMatch(assetUrl))); // Check if any Regex matches
        }

        internal static string TransformToRegex(string pattern)
        {
            // Try to allocate slightly more than original size
            var result = new StringBuilder(pattern.Length + pattern.Length / 2);

            int startPosition = 0;

            if (pattern.Length > 0 && pattern[0] == '/')
            {
                // If pattern start with a /, it must match from beginning
                result.Append('^');
                startPosition = 1;
            }
            else
            {
                // If pattern doesn't start with a /, it can match either beginning or right after a /
                result.Append(@"(^|/)");
            }

            for (int i = startPosition; i < pattern.Length; ++i)
            {
                var c = pattern[i];
                switch (c)
                {
                    case '*':
                        // Match everything (except '/')
                        result.Append("[^/]*");
                        break;
                    case '?':
                        // Match a single character (except '/')
                        result.Append("[^/]");
                        break;
                    case '\\':
                        // If not last character, escape next one
                        if (++i < pattern.Length)
                            c = pattern[i];

                        // Default case (add character as is)
                        goto default;
                    case '[':
                       throw new NotImplementedException("Can't match pattern that uses '['");
                    default:
                        result.Append(Regex.Escape(c.ToString()));
                        break;
                }
            }

            // If there is no '/' at the end, it must either finish or have another path after
            if (pattern.Length > 0 && pattern[pattern.Length - 1] != '/')
            {
                result.Append(@"($|/)");
            }
            return result.ToString();
        }
    }
}
