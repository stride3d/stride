// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Threading;

namespace Stride.Heightfield.Assets;

[AssetCompiler(typeof(HeightfieldAsset), typeof(AssetCompilationContext))]
internal sealed class HeightfieldAssetCompiler : AssetCompilerBase
{
    protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
    {
        var asset = (HeightfieldAsset)assetItem.Asset;

        result.BuildSteps = new AssetBuildStep(assetItem);
        result.BuildSteps.Add(new HeightfieldAssetCommand(targetUrlInStorage, asset, assetItem.Package ?? throw new NullReferenceException()));
    }

    public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
    {
        var asset = (HeightfieldAsset)assetItem.Asset;

        return asset.Layer?.GetInputFiles() ?? [];
    }

    public class HeightfieldAssetCommand(string url, HeightfieldAsset parameters, IAssetFinder assetFinder)
        : AssetCommand<HeightfieldAsset>(url, parameters, assetFinder)
    {
        public override IEnumerable<ObjectUrl> GetInputFiles()
        {
            return parameters.Layer?.GetInputFiles() ?? [];
        }

        protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            if (Parameters.Layer is null)
                return ResultStatus.Failed;
            
            Parameters.Layer.BuildRuntimeRepresentation(Parameters.Size, Parameters.Subdivision, commandContext, AssetFinder, out var runtime, out float minHeight, out float maxHeight);

            var heightfieldFunction = runtime.BuildHeightfieldFunction();

            var coarseBlockInterval = Parameters.CoarseBlockInterval;
            var coarseBlocksSubdivision = Parameters.Subdivision / Parameters.CoarseBlockInterval + (Parameters.Subdivision % Parameters.CoarseBlockInterval == 0 ? 0 : 1);
            var coarseBlocks = new HeightRange[coarseBlocksSubdivision * coarseBlocksSubdivision];
            coarseBlocks.AsSpan().Fill(new HeightRange(float.PositiveInfinity, float.NegativeInfinity));

            var cancellationToken = commandContext.CurrentCommand.CancellationToken;

            await DispatcherLowPriority.ForBatchedAsync(coarseBlocks.Length, (blockStart, blockEnd) =>
            {
                int blockIndex = blockStart;
                foreach (ref var range in coarseBlocks.AsSpan()[blockStart..blockEnd])
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    range = HeightRange.ExtractRange(blockIndex, coarseBlocksSubdivision, coarseBlockInterval, Parameters.Subdivision, heightfieldFunction);
                    blockIndex++;
                }
            });

            cancellationToken.ThrowIfCancellationRequested();

            var runtimeObject = new Heightfield
            {
                TopmostLayer = runtime,
                Subdivision = Parameters.Subdivision,
                Size = Parameters.Size,
                MinHeight = minHeight,
                MaxHeight = maxHeight,
                CoarseBlockSubdivision = coarseBlocksSubdivision,
                CoarseBlocks = coarseBlocks
            };

            var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            assetManager.Save(Url, runtimeObject);

            return ResultStatus.Successful;
        }
    }
}
