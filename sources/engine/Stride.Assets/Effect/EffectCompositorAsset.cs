// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core;

namespace Stride.Assets.Effect
{
    /// <summary>
    /// Describes a shader effect asset (sdsl).
    /// </summary>
    [DataContract("EffectCompositorAsset")]
    [AssetDescription(FileExtension, AlwaysMarkAsRoot = true, AllowArchetype = false)]
    public sealed partial class EffectCompositorAsset : ProjectSourceCodeWithFileGeneratorAsset
    {
        /// <summary>
        /// The default file extension used by the <see cref="EffectCompositorAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdfx";

        public override string Generator => "StrideEffectCodeGenerator";

        public override void SaveGeneratedAsset(AssetItem assetItem)
        {
            // TODO: Implement this?
        }
    }
}
