// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets;
using Xenko.Core;

namespace Xenko.Assets.Effect
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
        public const string FileExtension = ".xkeffectlog";
    }
}
