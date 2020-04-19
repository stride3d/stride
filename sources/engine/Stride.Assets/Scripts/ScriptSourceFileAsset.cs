// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;

namespace Stride.Assets.Scripts
{
    [DataContract("ScriptSourceFileAsset")]
    [AssetDescription(Extension, AlwaysMarkAsRoot = true, AllowArchetype = false, Referenceable = false)]
    public sealed partial class ScriptSourceFileAsset : ProjectSourceCodeAsset
    {
        public const string Extension = ".cs";
    }
}
