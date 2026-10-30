// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Threading;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.TextureConverter;

namespace Stride.Heightfield.Assets;

[AssetCompiler(typeof(NormalFromHeightAsset), typeof(AssetCompilationContext))]
internal class NormalFromHeightAssetCompiler : AssetCompilerBase
{
    public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
    {
        var asset = (NormalFromHeightAsset)assetItem.Asset;
        var texture = asset.HeightInput;
        if (texture is not null)
        {
            var url = AttachedReferenceManager.GetUrl(texture) ?? throw new NullReferenceException();
            yield return new ObjectUrl(UrlType.Content, url);
        }
    }

    protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
    {
        var asset = (NormalFromHeightAsset)assetItem.Asset;
        
        var gameSettingsAsset = context.GetGameSettingsAsset();
        var renderingSettings = gameSettingsAsset.GetOrCreate<RenderingSettings>();
        var convertParameters = new TextureHelper.ImportParameters(
            asset, 
            gameSettingsAsset.GetOrCreate<TextureSettings>().TextureQuality, 
            context.Platform.GetDefaultGraphicsPlatform(), 
            renderingSettings.DefaultGraphicsProfile, 
            context.Platform)
        {
            OutputUrl = targetUrlInStorage
        };
        result.BuildSteps = new AssetBuildStep(assetItem);
        result.BuildSteps.Add(new NormalFromHeightCompileCommand(targetUrlInStorage, asset, convertParameters, assetItem.Package));
    }

    private class NormalFromHeightCompileCommand : AssetCommand<NormalFromHeightAsset>
    {
        private readonly TextureHelper.ImportParameters importParameters;
        
        public NormalFromHeightCompileCommand(string url, NormalFromHeightAsset parameters, TextureHelper.ImportParameters importParameters, IAssetFinder assetFinder)
            : base(url, parameters, assetFinder)
        {
            this.importParameters = importParameters;
        }

        public override IEnumerable<ObjectUrl> GetInputFiles()
        {
            var texture = Parameters.HeightInput;
            if (texture is not null)
            {
                var url = AttachedReferenceManager.GetUrl(texture) ?? throw new NullReferenceException();
                yield return new ObjectUrl(UrlType.Content, url);
            }
        }

        protected override async Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var heightInput = Parameters.HeightInput;
            
            if (heightInput is null)
                return ResultStatus.Failed;
            
            float[] heights;
            Int2 texResolution;

            var content = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            var url = AttachedReferenceManager.GetUrl(heightInput) ?? throw new NullReferenceException();
            var loadedTexture = content.Load<Texture>(url, ContentManagerLoaderSettings.StreamingDisabled);
            var baseImage = loadedTexture.GetSerializationData().Image;

            using (var textureTool = new TextureTool())
            using (var texImage = textureTool.Load(baseImage, baseImage.Description.Format.IsSRgb))
            {
                if (texImage.Format.IsCompressed)
                    textureTool.Decompress(texImage, texImage.Format.IsSRgb);
                textureTool.Convert(texImage, PixelFormat.R32_Float);

                using (var image = textureTool.ConvertToStrideImage(texImage))
                {
                    var pixelBuffer = image.GetPixelBuffer(0, 0, 0);

                    heights = pixelBuffer.GetPixels<float>();

                    var description = image.Description;
                    texResolution = new Int2(description.Width, description.Height);
                }
            }

            await DispatcherLowPriority.ForBatchedAsync(heights.Length, (start, end) =>
            {
                var span = heights.AsSpan()[start..end];
                foreach (ref var height in span)
                    height *= Parameters.HeightInUnits;
            });

            CancellationToken.ThrowIfCancellationRequested();

            var dataPackedFormat = PixelFormat.R16G16_Float;
            var dataPacked = new Half2[texResolution.X * texResolution.Y];

            await DispatcherLowPriority.ForBatchedAsync(texResolution.Y, (yStart, yEnd) =>
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    // Taking span to elide bounds check
                    var span = dataPacked.AsSpan(y * texResolution.X, texResolution.X);
                    for (int x = 0; x < span.Length; x++)
                    {
                        var coord = new Int2(x, y);

                        Vector2 d0, d1;
                        coord.X -= 1; d0.X = SampleTexture(heights, Parameters.WrapMode, coord, texResolution);
                        coord.X += 2; d1.X = SampleTexture(heights, Parameters.WrapMode, coord, texResolution);

                        coord.X -= 1;

                        coord.Y -= 1; d0.Y = SampleTexture(heights, Parameters.WrapMode, coord, texResolution);
                        coord.Y += 2; d1.Y = SampleTexture(heights, Parameters.WrapMode, coord, texResolution);

                        coord.Y -= 1;

                        var pixelNormalizedSize = 1f / (Vector2)texResolution;
                        var vec = (d1 - d0) / (2f * pixelNormalizedSize * Parameters.SizeInUnits);
                        // Remap into texture range
                        vec = vec * 0.5f + new Vector2(0.5f);
                        if (Parameters.InvertX)
                            vec.X = 1 - vec.X;
                        if (Parameters.InvertY)
                            vec.Y = 1 - vec.Y;

                        span[x] = (Half2)vec;
                    }
                }
            });

            CancellationToken.ThrowIfCancellationRequested();

            unsafe
            {
                fixed (Half2* ptr = dataPacked)
                {
                    using (var texTool = new TextureTool())
                    using (var texImage = new TexImage((IntPtr)ptr, sizeof(Half2) * dataPacked.Length, texResolution.X, texResolution.Y, 1, dataPackedFormat, 1, 1, TexImage.TextureDimension.Texture2D))
                    {
                        var importResult = TextureAssetCompiler.TextureConvertCommand.Import(commandContext, texTool, texImage, importParameters, Parameters.IsStreamable, CancellationToken);
                        return importResult;
                    }
                }
            }
        }

        static float SampleTexture(float[] heights, NormalFromHeightAsset.WrapModes wrapModes, Int2 coord, Int2 texResolution)
        {
            switch (wrapModes)
            {
                case NormalFromHeightAsset.WrapModes.Repeat:
                    // We're sampling just one unit away, wrap can't ever be larger than texResolution.X
                    if (coord.X < 0)
                        coord.X += texResolution.X;
                    else if (coord.X >= texResolution.X)
                        coord.X -= texResolution.X;

                    if (coord.Y < 0)
                        coord.Y += texResolution.Y;
                    else if (coord.Y >= texResolution.Y)
                        coord.Y -= texResolution.Y;
                    break;
                case NormalFromHeightAsset.WrapModes.Clamp:
                    coord = Int2.Clamp(coord, default, texResolution - Int2.One);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(wrapModes.ToString());
            }

            return heights[coord.X + texResolution.X * coord.Y];
        }
    }
}
