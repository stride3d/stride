// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
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

                var items = new Heightmap[] { new Heightmap(), };

                foreach (var heightmap in items)
                {
                    var source = Parameters.Source;
                    if (!string.IsNullOrEmpty(source))
                    {
                        using (var textureTool = new TextureTool())
                        using (var texImage = textureTool.Load(source, Parameters.IsSRgb))
                        {
                            // Resize the image if need

                            var size = Parameters.Size.Enabled && Parameters.Size.Size.X > 1 && Parameters.Size.Size.Y > 1 ?
                                Parameters.Size.Size :
                                new Int2(texImage.Width, texImage.Height);

                            if (texImage.Width != size.X || texImage.Height != size.Y)
                            {
                                textureTool.Resize(texImage, size.X, size.Y, Filter.Rescaling.Nearest);
                            }

                            // Convert pixel format of the image

                            var heightfieldType = Parameters.HeightType;

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
                                            continue;
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
                                            continue;
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
                                            continue;
                                    }
                                    break;

                                default:
                                    continue;
                            }

                            // Read, scale and set heights

                            using (var image = textureTool.ConvertToXenkoImage(texImage))
                            {
                                var pixelBuffer = image.PixelBuffer[0];
                                var scale = Parameters.HeightScale;

                                switch (heightfieldType)
                                {
                                    case HeightfieldTypes.Float:
                                        heightmap.Floats = pixelBuffer.GetPixels<float>();
                                        for (int i = 0; i < heightmap.Floats.Length; ++i)
                                        {
                                            heightmap.Floats[i] *= scale;
                                        }
                                        break;

                                    case HeightfieldTypes.Short:
                                        heightmap.Shorts = pixelBuffer.GetPixels<short>();
                                        for (int i = 0; i < heightmap.Shorts.Length; ++i)
                                        {
                                            heightmap.Shorts[i] = (short)MathUtil.Clamp(heightmap.Shorts[i] * scale, short.MinValue, short.MaxValue);
                                        }
                                        break;

                                    case HeightfieldTypes.Byte:
                                        heightmap.Bytes = pixelBuffer.GetPixels<byte>();
                                        for (int i = 0; i < heightmap.Bytes.Length; ++i)
                                        {
                                            heightmap.Bytes[i] = (byte)MathUtil.Clamp(heightmap.Bytes[i] * scale, byte.MinValue, byte.MaxValue);
                                        }
                                        break;

                                    default:
                                        continue;
                                }

                                // Set rest of properties

                                heightmap.HeightType = heightfieldType;
                                heightmap.Width = size.X;
                                heightmap.Length = size.Y;
                            }
                        }
                    }
                }

                assetManager.Save(Url, items[0]);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
