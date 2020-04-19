// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;

namespace Stride.Assets.Effect
{
    /// <summary>
    /// Describes an effect asset. 
    /// </summary>
    [DataContract("EffectLibrary")]
    [AssetDescription(FileExtension, AlwaysMarkAsRoot = true, AllowArchetype = false)]
    public sealed partial class EffectLogAsset : SourceCodeAsset
    {
        /// <summary>
        /// The default file name used to store effect compile logs.
        /// </summary>
        public const string DefaultFile = "EffectCompileLog";

        /// <summary>
        /// The default file extension used by the <see cref="EffectLogAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdeffectlog";
    }
}
