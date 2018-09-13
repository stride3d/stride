// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.IO;

namespace Xenko.GameStudio
{
    /// <summary>
    /// A class representing additional data associated with a <see cref="MostRecentlyUsedFiles.MostRecentlyUsedFile"/>
    /// </summary>
    [DataContract("MRUAdditionalData")]
    [DebuggerDisplay("MRUData: {FilePath}")]
    public class MRUAdditionalData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MRUAdditionalData"/>.
        /// </summary>
        public MRUAdditionalData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MRUAdditionalData"/> from a given file path.
        /// </summary>
        /// <param name="filePath"></param>
        public MRUAdditionalData([NotNull] UFile filePath)
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
        /// The list of assets that were opened in an editor.
        /// </summary>
        [DataMember]
        public IList<AssetId> OpenedAssets { get; set; } = new List<AssetId>();

        [DataMember]
        public int DockingLayoutVersion { get; set; }

        /// <summary>
        /// Game Studio layout when no editors are opened.
        /// </summary>
        [DataMember]
        public string DockingLayout { get; set; }

        /// <summary>
        /// Game Studio layout when the editor area is visible.
        /// </summary>
        [DataMember]
        public string DockingLayoutEditors { get; set; }
    }
}
