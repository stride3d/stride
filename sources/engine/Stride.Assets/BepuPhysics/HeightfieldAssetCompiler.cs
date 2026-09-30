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

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

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

    /// <summary>
    /// An <see cref="AssetCommand"/> that converts design time asset into runtime asset.
    /// </summary>
    public class HeightfieldAssetCommand(string url, HeightfieldAsset parameters, IAssetFinder assetFinder)
        : AssetCommand<HeightfieldAsset>(url, parameters, assetFinder)
    {
        public override IEnumerable<ObjectUrl> GetInputFiles()
        {
            return parameters.Layer?.GetInputFiles() ?? [];
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            if (Parameters.Layer is null)
                return Task.FromResult(ResultStatus.Failed);
            
            Parameters.Layer.BuildRuntimeRepresentation(Parameters.Size, Parameters.Subdivision, commandContext, AssetFinder, out var runtime, out float minHeight, out float maxHeight);

            var heightfieldFunction = runtime.BuildHeightfieldFunction();

            var coarseBlockInterval = Parameters.CoarseBlockInterval;
            var coarseBlocksSubdivision = Parameters.Subdivision / Parameters.CoarseBlockInterval + (Parameters.Subdivision % Parameters.CoarseBlockInterval == 0 ? 0 : 1);
            var coarseBlocks = new HeightRange[coarseBlocksSubdivision * coarseBlocksSubdivision];
            coarseBlocks.AsSpan().Fill(new HeightRange(float.PositiveInfinity, float.NegativeInfinity));

            var samples = new Sample[1];
            for (int cellZ = 0; cellZ < Parameters.Subdivision; cellZ++)
            {
                for (int cellX = 0; cellX < Parameters.Subdivision; cellX++)
                {
                    samples[0].SampleCoord = new Int2(cellX, cellZ);
                    heightfieldFunction.FillSamples(samples);

                    // Samples on block edges should be considered for all touching blocks
                    // Naive approach
                    for (int cZ = cellZ > 0 ? cellZ - 1 : cellZ; cZ <= cellZ; cZ++)
                    {
                        for (int cX = cellX > 0 ? cellX - 1 : cellX; cX <= cellX; cX++)
                        {
                            int i = cZ / coarseBlockInterval * coarseBlocksSubdivision + cX / coarseBlockInterval;
                            ref var range = ref coarseBlocks[i];
                            range.MinHeight = Math.Min(range.MinHeight, samples[0].Height);
                            range.MaxHeight = Math.Max(range.MaxHeight, samples[0].Height);
                        }
                    }
                }
            }

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

            return Task.FromResult(ResultStatus.Successful);
        }
    }
}
