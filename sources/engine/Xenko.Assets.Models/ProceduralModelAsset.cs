// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Serialization;
using Xenko.Rendering;
using Xenko.Rendering.ProceduralModels;

namespace Xenko.Assets.Models
{
    /// <summary>
    /// The geometric primitive asset.
    /// </summary>
    [DataContract("ProceduralModelAsset")]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(Model))]
    [Display((int)AssetDisplayPriority.Models + 40, "Procedural model")]
    [AssetFormatVersion(XenkoConfig.PackageName, CurrentVersion, "2.0.0.0")]
    public sealed class ProceduralModelAsset : Asset, IModelAsset
    {
        private const string CurrentVersion = "2.0.0.0";

        /// <summary>
        /// The default file extension used by the <see cref="ProceduralModelAsset"/>.
        /// </summary>
        public const string FileExtension = ".xkpromodel";

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        /// <userdoc>The type of procedural model to generate</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Type", Expand = ExpandRule.Always)]
        public IProceduralModel Type { get; set; }

        /// <inheritdoc/>
        [DataMemberIgnore]
        public List<ModelMaterial> Materials => Type.MaterialInstances.Select(x => new ModelMaterial { Name = x.Key, MaterialInstance = x.Value }).ToList();
    }
}
