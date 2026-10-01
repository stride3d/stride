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
                var samples = new Sample[1];
                for (int block = blockStart; block < blockEnd; block++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ref var range = ref coarseBlocks[block];
                    Int2 block2D = new Int2(block % coarseBlocksSubdivision, block / coarseBlocksSubdivision);
                    Int2 sampleCorner = block2D * coarseBlockInterval;
                    Int2 sampleEnd = sampleCorner + new Int2(coarseBlockInterval);

                    // We're defining block ranges as operating on N amount of samples, although the physics shape uses
                    // them when evaluating triangles, those lay between samples.
                    // If we specify three samples of coverage, e.g.: block0{s0, s1, s2}, block1{s3, s4, s5}
                    // The triangle laying between s2 and s3 would not be covered by either blocks.
                    // s2 would be the coordinate used when a position between s2 and s3 is tested,
                    // which in turn means that block0 would be the block retrieved for evaluation.
                    // We will have each end of the blocks extend to the next sample over to ensure we capture those triangles
                    Int2 sampleCornerEndInclusive = Int2.Min(sampleEnd, new Int2(Parameters.Subdivision - 1));
                    for (int sampleY = sampleCorner.Y; sampleY <= sampleCornerEndInclusive.Y; sampleY++)
                    {
                        for (int sampleX = sampleCorner.X; sampleX <= sampleCornerEndInclusive.X; sampleX++)
                        {
                            samples[0].SampleCoord = new Int2(sampleX, sampleY);
                            heightfieldFunction.FillSamples(samples);
                            range.MinHeight = Math.Min(range.MinHeight, samples[0].Height);
                            range.MaxHeight = Math.Max(range.MaxHeight, samples[0].Height);
                        }
                    }
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
