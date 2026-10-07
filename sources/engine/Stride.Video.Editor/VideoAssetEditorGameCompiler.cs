// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Editor.Preview;
using Stride.Video.Assets;

namespace Stride.Video.Editor
{
    [AssetCompiler(typeof(VideoAsset), typeof(EditorGameCompilationContext))]
    public class VideoAssetEditorGameCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new DummyAssetCommand<VideoAsset, global::Stride.Video.Video>(assetItem));
        }
    }
}
