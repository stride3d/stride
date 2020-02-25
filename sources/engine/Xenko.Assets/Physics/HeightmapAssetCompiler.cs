// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Assets.Textures;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Graphics;
using Xenko.Physics;
using Xenko.TextureConverter;

namespace Xenko.Assets.Physics
{
    [AssetCompiler(typeof(HeightmapAsset), typeof(AssetCompilationContext))]
    internal class HeightmapAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(TextureAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var asset = (HeightmapAsset)assetItem.Asset;
            var url = asset.Source.FullPath;
            if (!string.IsNullOrEmpty(url))
            {
                yield return new ObjectUrl(UrlType.File, url);
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (HeightmapAsset)assetItem.Asset;

            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new HeightmapConvertCommand(targetUrlInStorage, asset, assetItem.Package) { InputFilesGetter = () => GetInputFiles(assetItem) });
        }

        public class HeightmapConvertCommand : AssetCommand<HeightmapAsset>
        {
            public HeightmapConvertCommand(string url, HeightmapAsset parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                Heightmap heightmap = null;

                // HeightRange

                var heightRange = Parameters.HeightConversionParameters.HeightRange;

                if (heightRange.Y < heightRange.X)
                {
                    throw new Exception($"Invalid HeightRange. Max height should be greater than min height.");
                }

                // HeightScale

                var heightScale = Parameters.HeightConversionParameters.HeightScale;

                // Heights

                var source = Parameters.Source;

                using (var textureTool = new TextureTool())
                using (var texImage = textureTool.Load(source, Parameters.IsSRgb))
                {
                    // Resize the image if needed

                    var size = Parameters.Resizing.Enabled ?
                        Parameters.Resizing.Size :
                        new Int2(texImage.Width, texImage.Height);

                    if (!HeightfieldColliderShapeDesc.IsValidHeightStickSize(size))
                    {
                        throw new Exception($"Invalid size. Width and length of the heightmap should be greater or than equal to 2.");
                    }

                    if (texImage.Width != size.X || texImage.Height != size.Y)
                    {
                        textureTool.Resize(texImage, size.X, size.Y, Filter.Rescaling.Nearest);
                    }

                    // Convert pixel format of the image

                    var heightfieldType = Parameters.HeightConversionParameters.HeightType;

                    switch (heightfieldType)
                    {
                        case HeightfieldTypes.Float:
                            switch (texImage.Format)
                            {
                                case PixelFormat.R32_Float:
                                    break;

                                case PixelFormat.R32G32B32A32_Float:
                                case PixelFormat.R16_Float:
                                    textureTool.Convert(texImage, PixelFormat.R32_Float);
                                    break;

                                case PixelFormat.R16G16B16A16_UNorm:
                                case PixelFormat.R16_UNorm:
                                    textureTool.Convert(texImage, PixelFormat.R16_SNorm);
                                    textureTool.Convert(texImage, PixelFormat.R32_Float);
                                    break;

                                case PixelFormat.B8G8R8A8_UNorm:
                                case PixelFormat.R8G8B8A8_UNorm:
                                case PixelFormat.R8_UNorm:
                                    textureTool.Convert(texImage, PixelFormat.R8_SNorm);
                                    textureTool.Convert(texImage, PixelFormat.R32_Float);
                                    break;

                                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                                case PixelFormat.B8G8R8X8_UNorm_SRgb:
                                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                                    textureTool.Convert(texImage, PixelFormat.R8_SNorm);
                                    textureTool.Convert(texImage, PixelFormat.R32_Float);
                                    break;

                                default:
                                    throw new Exception($"Not supported to convert {texImage.Format} to {PixelFormat.R32_Float}.");
                            }
                            break;

                        case HeightfieldTypes.Short:
                            switch (texImage.Format)
                            {
                                case PixelFormat.R16_SNorm:
                                    break;

                                case PixelFormat.R16G16B16A16_SNorm:
                                case PixelFormat.R16G16B16A16_UNorm:
                                case PixelFormat.R16_UNorm:
                                    textureTool.Convert(texImage, PixelFormat.R16_SNorm);
                                    break;

                                case PixelFormat.R8G8B8A8_SNorm:
                                case PixelFormat.B8G8R8A8_UNorm:
                                case PixelFormat.R8G8B8A8_UNorm:
                                case PixelFormat.R8_UNorm:
                                    textureTool.Convert(texImage, PixelFormat.R8_SNorm);
                                    textureTool.Convert(texImage, PixelFormat.R16_SNorm);
                                    break;

                                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                                case PixelFormat.B8G8R8X8_UNorm_SRgb:
                                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                                    textureTool.Convert(texImage, PixelFormat.R8_SNorm);
                                    textureTool.Convert(texImage, PixelFormat.R16_SNorm);
                                    break;

                                default:
                                    throw new Exception($"Not supported to convert {texImage.Format} to {PixelFormat.R16_SNorm}.");
                            }
                            break;

                        case HeightfieldTypes.Byte:
                            switch (texImage.Format)
                            {
                                case PixelFormat.R8_UNorm:
                                    break;

                                case PixelFormat.R8G8B8A8_SNorm:
                                case PixelFormat.B8G8R8A8_UNorm:
                                case PixelFormat.R8G8B8A8_UNorm:
                                    textureTool.Convert(texImage, PixelFormat.R8_UNorm);
                                    break;

                                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                                case PixelFormat.B8G8R8X8_UNorm_SRgb:
                                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                                    textureTool.Convert(texImage, PixelFormat.R8_UNorm);
                                    break;

                                default:
                                    throw new Exception($"Not supported to convert {texImage.Format} to {PixelFormat.R8_UNorm}.");
                            }
                            break;

                        default:
                            throw new Exception($"Not supported the image whose pixel format is {texImage.Format}.");
                    }

                    // Convert pixels to heights

                    using (var image = textureTool.ConvertToXenkoImage(texImage))
                    {
                        var pixelBuffer = image.PixelBuffer[0];

                        object heights = null;

                        switch (heightfieldType)
                        {
                            case HeightfieldTypes.Float:
                                {
                                    var floats = pixelBuffer.GetPixels<float>();

                                    var floatConversionParameters = Parameters.HeightConversionParameters as FloatHeightmapHeightConversionParamters;
                                    if (floatConversionParameters == null)
                                    {
                                        throw new NullReferenceException($"{nameof(Parameters.HeightConversionParameters)} is a null.");
                                    }

                                    float scale = 1f;

                                    if (floatConversionParameters.ScaleToRange)
                                    {
                                        var max = floats.Max(h => Math.Abs(h));
                                        if ((max - 1f) < float.Epsilon)
                                        {
                                            max = 1f;
                                        }
                                        scale = Math.Max(Math.Abs(heightRange.X), Math.Abs(heightRange.Y)) / max;
                                    }

                                    for (int i = 0; i < floats.Length; ++i)
                                    {
                                        floats[i] = MathUtil.Clamp(floats[i] * scale, heightRange.X, heightRange.Y);
                                    }

                                    heights = floats;
                                }
                                break;

                            case HeightfieldTypes.Short:
                                {
                                    heights = pixelBuffer.GetPixels<short>();
                                }
                                break;

                            case HeightfieldTypes.Byte:
                                {
                                    heights = pixelBuffer.GetPixels<byte>();
                                }
                                break;
                        }

                        heightmap = Heightmap.Create(size, heightRange, heightScale, heights);
                    }
                }

                if (heightmap == null)
                {
                    throw new Exception($"Failed to compile the heightmap asset.");
                }

                assetManager.Save(Url, heightmap);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
