// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Assets.Media;
using Xenko.Audio;

namespace Xenko.Editor.Preview
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
