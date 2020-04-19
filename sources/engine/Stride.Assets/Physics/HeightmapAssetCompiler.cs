// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Physics;
using Stride.TextureConverter;

namespace Stride.Assets.Physics
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

                var heightType = Parameters.HeightConversionParameters.HeightType;
                var heightScale = Parameters.HeightConversionParameters.HeightScale;
                var heightRange = Parameters.HeightConversionParameters.HeightRange;

                // Heights

                var source = Parameters.Source;

                using (var textureTool = new TextureTool())
                using (var texImage = textureTool.Load(source, Parameters.IsSRgb))
                {
                    // Resize if needed.

                    var size = Parameters.Resizing.Enabled ?
                        Parameters.Resizing.Size :
                        new Int2(texImage.Width, texImage.Height);

                    HeightmapUtils.CheckHeightParameters(size, heightType, heightRange, heightScale, true);

                    // Convert the pixel format to single component one.

                    var isConvertedR16 = false;

                    switch (texImage.Format)
                    {
                        case PixelFormat.R32_Float:
                        case PixelFormat.R16_SNorm:
                        case PixelFormat.R8_UNorm:
                            break;

                        case PixelFormat.R32G32B32A32_Float:
                        case PixelFormat.R16G16B16A16_Float:
                        case PixelFormat.R16_Float:
                            textureTool.Convert(texImage, PixelFormat.R32_Float);
                            break;

                        case PixelFormat.R16_UNorm:
                        case PixelFormat.R16G16B16A16_UNorm:
                        case PixelFormat.R16G16_UNorm:
                            textureTool.Convert(texImage, PixelFormat.R16_SNorm);
                            isConvertedR16 = true;
                            break;

                        case PixelFormat.R16G16B16A16_SNorm:
                        case PixelFormat.R16G16_SNorm:
                            textureTool.Convert(texImage, PixelFormat.R16_SNorm);
                            break;

                        case PixelFormat.R8_SNorm:
                        case PixelFormat.B8G8R8A8_UNorm:
                        case PixelFormat.B8G8R8X8_UNorm:
                        case PixelFormat.R8G8B8A8_UNorm:
                        case PixelFormat.R8G8_UNorm:
                            textureTool.Convert(texImage, PixelFormat.R8_UNorm);
                            break;

                        case PixelFormat.R8G8B8A8_SNorm:
                        case PixelFormat.R8G8_SNorm:
                            textureTool.Convert(texImage, PixelFormat.R8_UNorm);
                            break;

                        case PixelFormat.B8G8R8A8_UNorm_SRgb:
                        case PixelFormat.B8G8R8X8_UNorm_SRgb:
                        case PixelFormat.R8G8B8A8_UNorm_SRgb:
                            textureTool.Convert(texImage, PixelFormat.R8_UNorm);
                            break;

                        default:
                            throw new Exception($"{ texImage.Format } format is not supported.");
                    }

                    // Convert pixels to heights

                    using (var image = textureTool.ConvertToStrideImage(texImage))
                    {
                        var pixelBuffer = image.PixelBuffer[0];
                        var pixelBufferSize = new Int2(pixelBuffer.Width, pixelBuffer.Height);

                        var minFloat = Parameters.FloatingPointComponentRange.X;
                        var maxFloat = Parameters.FloatingPointComponentRange.Y;
                        var isSNorm = (Math.Abs(-1 - minFloat) < float.Epsilon) && (Math.Abs(1 - maxFloat) < float.Epsilon);

                        if (maxFloat < minFloat)
                        {
                            throw new Exception($"{ nameof(Parameters.FloatingPointComponentRange) }.{ nameof(Parameters.FloatingPointComponentRange.Y) } should be greater than { nameof(Parameters.FloatingPointComponentRange.X) }.");
                        }

                        var useScaleToRange = Parameters.ScaleToHeightRange;

                        switch (heightType)
                        {
                            case HeightfieldTypes.Float:
                                {
                                    float[] floats = null;

                                    switch (image.Description.Format)
                                    {
                                        case PixelFormat.R32_Float:
                                            floats = HeightmapUtils.Resize(pixelBuffer.GetPixels<float>(), pixelBufferSize, size);
                                            floats = isSNorm ?
                                                floats :
                                                HeightmapUtils.ConvertToFloatHeights(floats, minFloat, maxFloat);
                                            break;

                                        case PixelFormat.R16_SNorm:
                                            var shorts = HeightmapUtils.Resize(pixelBuffer.GetPixels<short>(), pixelBufferSize, size);
                                            floats = !isConvertedR16 && Parameters.IsSymmetricShortComponent ?
                                                HeightmapUtils.ConvertToFloatHeights(shorts, -short.MaxValue, short.MaxValue) :
                                                HeightmapUtils.ConvertToFloatHeights(shorts);
                                            break;

                                        case PixelFormat.R8_UNorm:
                                            var bytes = HeightmapUtils.Resize(pixelBuffer.GetPixels<byte>(), pixelBufferSize, size);
                                            floats = HeightmapUtils.ConvertToFloatHeights(bytes);
                                            break;
                                    }

                                    if (useScaleToRange)
                                    {
                                        ScaleToHeightRange(floats, -1, 1, heightRange, heightScale, commandContext);
                                    }

                                    heightmap = Heightmap.Create(size, heightType, heightRange, heightScale, floats);
                                }
                                break;

                            case HeightfieldTypes.Short:
                                {
                                    short[] shorts = null;

                                    switch (image.Description.Format)
                                    {
                                        case PixelFormat.R32_Float:
                                            var floats = HeightmapUtils.Resize(pixelBuffer.GetPixels<float>(), pixelBufferSize, size);
                                            shorts = HeightmapUtils.ConvertToShortHeights(floats, minFloat, maxFloat);
                                            break;

                                        case PixelFormat.R16_SNorm:
                                            shorts = HeightmapUtils.Resize(pixelBuffer.GetPixels<short>(), pixelBufferSize, size);
                                            shorts = !isConvertedR16 && Parameters.IsSymmetricShortComponent ?
                                                shorts :
                                                HeightmapUtils.ConvertToShortHeights(shorts);
                                            break;

                                        case PixelFormat.R8_UNorm:
                                            var bytes = HeightmapUtils.Resize(pixelBuffer.GetPixels<byte>(), pixelBufferSize, size);
                                            shorts = HeightmapUtils.ConvertToShortHeights(bytes);
                                            break;
                                    }

                                    if (useScaleToRange)
                                    {
                                        ScaleToHeightRange(shorts, short.MinValue, short.MaxValue, heightRange, heightScale, commandContext);
                                    }

                                    heightmap = Heightmap.Create(size, heightType, heightRange, heightScale, shorts);
                                }
                                break;

                            case HeightfieldTypes.Byte:
                                {
                                    byte[] bytes = null;

                                    switch (image.Description.Format)
                                    {
                                        case PixelFormat.R32_Float:
                                            var floats = HeightmapUtils.Resize(pixelBuffer.GetPixels<float>(), pixelBufferSize, size);
                                            bytes = HeightmapUtils.ConvertToByteHeights(floats, minFloat, maxFloat);
                                            break;

                                        case PixelFormat.R16_SNorm:
                                            var shorts = HeightmapUtils.Resize(pixelBuffer.GetPixels<short>(), pixelBufferSize, size);
                                            bytes = !isConvertedR16 && Parameters.IsSymmetricShortComponent ?
                                                HeightmapUtils.ConvertToByteHeights(shorts, -short.MaxValue, short.MaxValue) :
                                                HeightmapUtils.ConvertToByteHeights(shorts);
                                            break;

                                        case PixelFormat.R8_UNorm:
                                            bytes = HeightmapUtils.Resize(pixelBuffer.GetPixels<byte>(), pixelBufferSize, size);
                                            break;
                                    }

                                    if (useScaleToRange)
                                    {
                                        ScaleToHeightRange(bytes, byte.MinValue, byte.MaxValue, heightRange, heightScale, commandContext);
                                    }

                                    heightmap = Heightmap.Create(size, heightType, heightRange, heightScale, bytes);
                                }
                                break;

                            default:
                                throw new Exception($"{ heightType } height type is not supported.");
                        }

                        commandContext.Logger.Info($"[{Url}] Convert Image(Format={ texImage.Format }, Width={ texImage.Width }, Height={ texImage.Height }) " +
                            $"to Heightmap(HeightType={ heightType }, MinHeight={ heightRange.X }, MaxHeight={ heightRange.Y }, HeightScale={ heightScale }, Size={ size }).");
                    }
                }

                if (heightmap == null)
                {
                    throw new Exception($"Failed to compile { Url }.");
                }

                assetManager.Save(Url, heightmap);

                return Task.FromResult(ResultStatus.Successful);
            }

            private void CalculateByteOrShortRange(Vector2 heightRage, float heightScale, out float min, out float max)
            {
                var minHeight = heightRage.X;
                var maxHeight = heightRage.Y;

                min = (float)Math.Round((minHeight / heightScale), MidpointRounding.AwayFromZero);
                max = (float)Math.Round((maxHeight / heightScale), MidpointRounding.AwayFromZero);

                if (heightScale < 0)
                {
                    min = (min * heightScale) < minHeight ? min - 1 : min;
                    max = (max * heightScale) > maxHeight ? max + 1 : max;
                    Core.Utilities.Swap(ref min, ref max);
                }
                else
                {
                    min = (min * heightScale) < minHeight ? min + 1 : min;
                    max = (max * heightScale) > maxHeight ? max - 1 : max;
                }
            }

            private void ScaleToHeightRange<T>(T[] heights, float minT, float maxT, Vector2 heightRange, float heightScale, ICommandContext commandContext) where T : struct
            {
                float min;
                float max;

                var typeOfT = typeof(T);

                if (typeOfT == typeof(float))
                {
                    min = heightRange.X;
                    max = heightRange.Y;
                }
                else if (typeOfT == typeof(short) || typeOfT == typeof(byte))
                {
                    CalculateByteOrShortRange(heightRange, heightScale, out min, out max);

                    if (!MathUtil.IsInRange(min, minT, maxT) ||
                        !MathUtil.IsInRange(max, minT, maxT))
                    {
                        throw new Exception(
                            $"{ nameof(ScaleToHeightRange) } failed to scale { minT }..{ maxT } to { min }..{ max }. Check HeightScale and HeightRange are proper.");
                    }
                }
                else
                {
                    throw new NotSupportedException($"{ typeof(T[]) } type is not supported.");
                }

                commandContext?.Logger.Info($"[{Url}] ScaleToHeightRange : { minT }..{ maxT } -> { min }..{ max }");

                if (typeOfT == typeof(float))
                {
                    float[] floats = heights as float[];
                    for (var i = 0; i < floats.Length; ++i)
                    {
                        floats[i] = MathUtil.Clamp(MathUtil.Lerp(min, max, MathUtil.InverseLerp(minT, maxT, floats[i])), min, max);
                    }
                }
                else if (typeOfT == typeof(short))
                {
                    short[] shorts = heights as short[];
                    for (var i = 0; i < shorts.Length; ++i)
                    {
                        shorts[i] = (short)MathUtil.Clamp(MathUtil.Lerp(min, max, MathUtil.InverseLerp(minT, maxT, shorts[i])), min, max);
                    }
                }
                else if (typeOfT == typeof(byte))
                {
                    byte[] bytes = heights as byte[];
                    for (var i = 0; i < bytes.Length; ++i)
                    {
                        bytes[i] = (byte)MathUtil.Clamp(MathUtil.Lerp(min, max, MathUtil.InverseLerp(minT, maxT, bytes[i])), min, max);
                    }
                }
            }
        }
    }
}
