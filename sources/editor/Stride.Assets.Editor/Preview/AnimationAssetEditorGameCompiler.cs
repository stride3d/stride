// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Animations;
using Stride.Assets.Models;
using Stride.Editor.Preview;

namespace Stride.Assets.Editor.Preview
{
    [AssetCompiler(typeof(AnimationAsset), typeof(EditorGameCompilationContext))]
    public class AnimationAssetEditorGameCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new DummyAssetCommand<AnimationAsset, AnimationClip>(assetItem));
        }
    }
}
