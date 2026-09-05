// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;

namespace StrideAssetPlugin.Assets;

[AssetCompiler(typeof(SpinAsset), typeof(AssetCompilationContext))]
public class SpinAssetCompiler : AssetCompilerBase
{
    protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
    {
        result.BuildSteps = new AssetBuildStep(assetItem);
        result.BuildSteps.Add(new SpinCommand(targetUrlInStorage, (SpinAsset)assetItem.Asset, assetItem.Package));
    }

    private class SpinCommand : AssetCommand<SpinAsset>
    {
        public SpinCommand(string url, SpinAsset parameters, IAssetFinder assetFinder)
            : base(url, parameters, assetFinder)
        {
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var contentManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            contentManager.Save(Url, new SpinData { Speed = Parameters.Speed });
            return Task.FromResult(ResultStatus.Successful);
        }
    }
}
