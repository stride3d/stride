// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Assets.Media;
using Stride.Audio;

namespace Stride.Editor.Preview
{
    [AssetCompiler(typeof(SoundAsset), typeof(EditorGameCompilationContext))]
    public class SoundAssetEditorGameCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new DummyAssetCommand<SoundAsset, Sound>(assetItem));
        }
    }
}
