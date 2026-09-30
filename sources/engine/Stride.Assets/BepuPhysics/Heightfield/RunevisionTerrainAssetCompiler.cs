// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.TextureConverter;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

[AssetCompiler(typeof(RunevisionTerrainAsset), typeof(AssetCompilationContext))]
internal class RunevisionTerrainAssetCompiler : AssetCompilerBase
{
    public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
    {
        yield break;
    }

    public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
    {
        var asset = (RunevisionTerrainAsset)assetItem.Asset;
        foreach (var inputFile in asset.HeightInput.GetInputFiles())
        {
            yield return inputFile;
        }
    }

    protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
    {
        var asset = (RunevisionTerrainAsset)assetItem.Asset;
        result.BuildSteps = new AssetBuildStep(assetItem);
        result.BuildSteps.Add(new RunevisionTerrainCompileCommand(targetUrlInStorage, asset, assetItem.Package));
    }

    private class RunevisionTerrainCompileCommand : AssetCommand<RunevisionTerrainAsset>
    {
        public RunevisionTerrainCompileCommand(string url, RunevisionTerrainAsset parameters, IAssetFinder assetFinder)
            : base(url, parameters, assetFinder)
        {
        }

        protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

            var minCollector = new ConcurrentCollector<float>();
            var maxCollector = new ConcurrentCollector<float>();

            var samples = new RunevisionTerrainErosion.Sample[Parameters.Resolution * Parameters.Resolution];

            var dataPackedFormat = PixelFormat.R32G32B32A32_Float;
            var dataPacked = new Vector4[Parameters.Resolution * Parameters.Resolution];

            var datatypes = Parameters.Data;
            if (Parameters.Settings.Trees == false)
                datatypes &= ~RunevisionTerrainAsset.DataTypes.TreesWhenEnabled;
            var dataTypes = new List<RunevisionTerrainAsset.DataTypes>();
            for (int i = 1; i <= (int)RunevisionTerrainAsset.DataTypes.Debug; i = i << 1)
            {
                var bit = (RunevisionTerrainAsset.DataTypes)i;
                if ((datatypes & bit) != 0)
                    dataTypes.Add(bit);
            }

            if (dataTypes.Count > 4)
            {
                dataTypes.RemoveRange(4, dataTypes.Count - 4);
                commandContext.Logger.Warning($"{nameof(RunevisionTerrainAsset)}: More than four datatypes enabled for {Url} ({Parameters.Data}), keeping the first four ({string.Join(',', datatypes)})");
            }

            var heightSampler = Parameters.HeightInput.BuildSampler();

            await Task.Run(() =>
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount > 1 ? Environment.ProcessorCount / 2 : 1
                };

                // Using parallel instead of the dispatcher as this could take a while
                Parallel.For(0, Parameters.Resolution, parallelOptions, y =>
                {
                    float min = float.PositiveInfinity;
                    float max = float.NegativeInfinity;
                    // Taking span to elide bounds check
                    var span = samples.AsSpan(y * Parameters.Resolution, Parameters.Resolution);
                    for (int x = 0; x < span.Length; x++)
                    {
                        ref var v = ref span[x];
                        v = RunevisionTerrainErosion.Heightmap(new Vector2(x, y) * Parameters.Tiling / Parameters.Resolution, Parameters.Settings, heightSampler);
                        v.Height *= Parameters.Height;
                        min = MathF.Min(v.Height, min);
                        max = MathF.Max(v.Height, max);
                    }
                    minCollector.Add(min);
                    maxCollector.Add(max);
                });

                minCollector.Close();
                maxCollector.Close();
                float min = float.PositiveInfinity;
                float max = float.NegativeInfinity;
                foreach (var x in minCollector)
                    min = MathF.Min(x, min);
                foreach (var x in maxCollector)
                    max = MathF.Max(x, max);

                switch (Parameters.Normalization)
                {
                    case RunevisionTerrainAsset.NormalizationMethod.None:
                        break;
                    case RunevisionTerrainAsset.NormalizationMethod.Root:
                        Parallel.For(0, Parameters.Resolution, parallelOptions, y =>
                        {
                            var span = samples.AsSpan(y * Parameters.Resolution, Parameters.Resolution);
                            for (int x = 0; x < span.Length; x++)
                            {
                                ref var v = ref span[x];
                                v.Height -= min;
                            }
                        });
                        break;
                    case RunevisionTerrainAsset.NormalizationMethod.Normalize:
                        float range = max - min;
                        Parallel.For(0, Parameters.Resolution, parallelOptions, y =>
                        {
                            var span = samples.AsSpan(y * Parameters.Resolution, Parameters.Resolution);
                            for (int x = 0; x < span.Length; x++)
                            {
                                ref var v = ref span[x];
                                v.Height -= min;
                                v.Height /= range;
                            }
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Parameters.Height != 1)
                {
                    Parallel.For(0, Parameters.Resolution, parallelOptions, y =>
                    {
                        var span = samples.AsSpan(y * Parameters.Resolution, Parameters.Resolution);
                        for (int x = 0; x < span.Length; x++)
                        {
                            ref var v = ref span[x];
                            v.Height *= Parameters.Height;
                        }
                    });
                }

                Parallel.For(0, Parameters.Resolution, parallelOptions, y =>
                {
                    var spanSamples = samples.AsSpan(y * Parameters.Resolution, Parameters.Resolution);
                    var spanPacked = dataPacked.AsSpan(y * Parameters.Resolution, Parameters.Resolution);

                    for (int x = 0; x < spanSamples.Length; x++)
                    {
                        ref var v = ref spanSamples[x];
                        for (int i = 0; i < dataTypes.Count; i++)
                        {
                            var dataType = dataTypes[i];
                            spanPacked[x][i] = dataType switch
                            {
                                RunevisionTerrainAsset.DataTypes.Height => v.Height,
                                RunevisionTerrainAsset.DataTypes.Erosion => v.Erosion,
                                RunevisionTerrainAsset.DataTypes.Ridges => v.Ridge,
                                RunevisionTerrainAsset.DataTypes.TreesWhenEnabled => v.Tree,
                                RunevisionTerrainAsset.DataTypes.Debug => v.Debug,
                                _ => throw new ArgumentOutOfRangeException()
                            };
                        }
                    }
                });
            });

            unsafe
            {
                fixed (Vector4* ptr = dataPacked)
                {
                    using (var textureTool = new TextureTool())
                    using (var texImage = new TexImage((IntPtr)ptr, sizeof(Vector4) * samples.Length, Parameters.Resolution, Parameters.Resolution, 1, dataPackedFormat, 1, 1, TexImage.TextureDimension.Texture2D))
                    {
                        if (dataPackedFormat != Parameters.Format)
                            textureTool.Convert(texImage, Parameters.Format);

                        var boxFilteringIsSupported = !texImage.Format.IsSRgb || MathUtil.IsPow2(Parameters.Resolution);
                        textureTool.GenerateMipMaps(texImage, boxFilteringIsSupported ? Filter.MipMapGeneration.Box : Filter.MipMapGeneration.Linear);
                        var imageWithMipmaps = textureTool.ConvertToStrideImage(texImage);
                        commandContext.Logger.Info($"Saving {Url} - {imageWithMipmaps.TotalSizeInBytes / 1024}MB");
                        assetManager.Save(Url, imageWithMipmaps.ToSerializableVersion(), typeof(Texture));
                    }
                }
            }

            return ResultStatus.Successful;
        }
    }
}
