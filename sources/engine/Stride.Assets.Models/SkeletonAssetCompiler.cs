// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;

namespace Stride.Assets.Models
{
    [AssetCompiler(typeof(SkeletonAsset), typeof(AssetCompilationContext))]
    public class SkeletonAssetCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SkeletonAsset)assetItem.Asset;
            var assetSource = GetAbsolutePath(assetItem, asset.Source);
            var extension = assetSource.GetFileExtension();
            var buildStep = new AssetBuildStep(assetItem);

            var importModelCommand = ImportModelCommand.Create(extension);
            if (importModelCommand == null)
            {
                result.Error($"No importer found for model extension '{extension}. The model '{assetSource}' can't be imported.");
                return;
            }

            importModelCommand.SourcePath = assetSource;
            importModelCommand.Location = targetUrlInStorage;
            importModelCommand.Mode = ImportModelCommand.ExportMode.Skeleton;
            importModelCommand.ScaleImport = asset.ScaleImport;
            importModelCommand.PivotPosition = asset.PivotPosition;
            importModelCommand.SkeletonNodesWithPreserveInfo = asset.NodesWithPreserveInfo;

            buildStep.Add(importModelCommand);

            result.BuildSteps = buildStep;
        }
    }
}
