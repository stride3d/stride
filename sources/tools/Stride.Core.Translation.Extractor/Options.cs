// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Translation.Extractor
{
    internal class Options
    {
        /// <summary>
        /// <c>true</c> if a backup of an existing output file should be created; otherwise, <c>false</c>.
        /// </summary>
        public bool Backup { get; set; }

        /// <summary>
        /// Patterns to exclude from the list of inputs.
        /// </summary>
        public List<string> Excludes { get; } = new List<string>();

        /// <summary>
        /// Directories to search for input files.
        /// </summary>
        public List<string> InputDirs { get; } = new List<string>();

        /// <summary>
        /// Patterns or input filnames to extract the messages from.
        /// </summary>
        public List<string> InputFiles { get; } = new List<string>();

        /// <summary>
        /// Name of generated catalog file.
        /// </summary>
        public string OutputFile { get; set; } = "messages.pot";

        /// <summary>
        /// <c>true</c> if an existing catalog should be ignored and not merged into the new one; otherwise, <c>false</c>.
        /// </summary>
        public bool Overwrite { get; set; } = true;

        /// <summary>
        /// <c>true</c> if previous comments from an existing catalog should be preserved; otherwise, <c>false</c>.
        /// </summary>
        public bool PreserveComments { get; set; }

        public bool Recursive { get; set; }

        public bool ShowUsage { get; set; }

        public bool Verbose { get; set; }

    }
}
