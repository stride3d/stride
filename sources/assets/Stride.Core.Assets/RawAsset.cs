// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core.Assets.Compiler;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A raw asset, an asset that is imported as-is.
    /// </summary>
    /// <userdoc>A raw asset, an asset that is imported as-is.</userdoc>
    [DataContract("RawAsset")]
    [AssetDescription(FileExtension)]
    [Display(1050, "Raw Asset")]
    public sealed class RawAsset : AssetWithSource
    {
        public const string FileExtension = ".sdraw";

        /// <summary>
        /// Initializes a new instance of the <see cref="RawAsset"/> class.
        /// </summary>
        public RawAsset()
        {
            Compress = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RawAsset"/> will be compressed when compiled.
        /// </summary>
        /// <value><c>true</c> if this asset will be compressed when compiled; otherwise, <c>false</c>.</value>
        /// <userdoc>A boolean indicating whether this asset will be compressed when compiled</userdoc>
        [DefaultValue(true)]
        public bool Compress { get; set; }
    }
}
