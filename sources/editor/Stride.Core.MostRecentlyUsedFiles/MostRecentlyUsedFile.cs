// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.IO;

namespace Stride.Core.MostRecentlyUsedFiles
{
    /// <summary>
    /// A class representing a recently used file associated with a timestamp.
    /// </summary>
    [DataContract("MRU")]
    [DebuggerDisplay("MRU: {FilePath} ({Timestamp})")]
    public class MostRecentlyUsedFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MostRecentlyUsedFile"/>.
        /// </summary>
        public MostRecentlyUsedFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MostRecentlyUsedFile"/> from a given file path.
        /// </summary>
        public MostRecentlyUsedFile([NotNull] UFile filePath)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Timestamp = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// The path of the recently used file.
        /// </summary>
        [DataMember]
        public UFile FilePath { get; set; }

        /// <summary>
        /// A timestamp of the last usage of the related file.
        /// </summary>
        [DataMember]
        public long Timestamp { get; set; }

        /// <summary>
        /// The version
        /// </summary>
        [DataMemberIgnore]
        public string Version { get; set; }
    }
}
