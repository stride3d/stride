// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Assets;
using Xenko.SpriteStudio.Runtime;

namespace Xenko.SpriteStudio.Offline
{
    [DataContract("SpriteStudioSheetAsset")] // Name of the Asset serialized in YAML
    [AssetContentType(typeof(SpriteStudioSheet))]
    [AssetDescription(FileExtension)] // A description used to display in the asset editor
    [Display("SpriteStudio sheet")]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public class SpriteStudioModelAsset : Asset
    {
        public const string FileExtension = ".xkss4s";

        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// Gets or sets the source file of this asset.
        /// </summary>
        /// <value>The source.</value>
        /// <userdoc>
        /// The source file of this asset.
        /// </userdoc>
        [DataMember(-50)]
        [DefaultValue(null)]
        [SourceFileMember(true)]
        public UFile Source { get; set; } = new UFile("");

        [DataMember(1)]
        [Display(Browsable = false)]
        public List<string> NodeNames { get; set; } = new List<string>();

        [DataMemberIgnore]
        public List<string> BuildTextures { get; } = new List<string>();

        [DataMemberIgnore]
        public override UFile MainSource => Source;
    }
}
