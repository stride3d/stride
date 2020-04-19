// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Core.Assets
{
    [DataContract("ProjectSourceCodeWithFileGeneratorAsset")]
    public abstract class ProjectSourceCodeWithFileGeneratorAsset : ProjectSourceCodeAsset, IProjectFileGeneratorAsset
    {
        /// <inheritdoc/>
        [DataMember(Mask = DataMemberAttribute.IgnoreMask)]
        [Display(Browsable = false)]
        public abstract string Generator { get; }

        /// <param name="assetItem"></param>
        /// <inheritdoc/>
        public abstract void SaveGeneratedAsset(AssetItem assetItem);
    }
}
