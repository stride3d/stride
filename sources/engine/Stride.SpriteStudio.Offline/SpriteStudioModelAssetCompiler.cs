// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Assets;
using Stride.Assets.Textures;
using Stride.Graphics;
using Stride.SpriteStudio.Runtime;
using System.Collections.Generic;
using System.Globalization;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;

namespace Stride.SpriteStudio.Offline
{
    [AssetCompiler(typeof(SpriteStudioModelAsset), typeof(AssetCompilationContext))]
    internal class SpriteStudioModelAssetCompiler : AssetCompilerBase
    {
        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SpriteStudioModelAsset)assetItem.Asset;
            var gameSettingsAsset = context.GetGameSettingsAsset();
            var renderingSettings = gameSettingsAsset.GetOrCreate<RenderingSettings>(context.Platform);
            var colorSpace = renderingSettings.ColorSpace;

            var cells = new List<SpriteStudioCell>();
            var images = new List<UFile>();
            if (!SpriteStudioXmlImport.ParseCellMaps(asset.Source, images, cells)) throw new Exception("Failed to parse SpriteStudio cell textures.");

            var texIndex = 0;
            asset.BuildTextures.Clear();
            foreach (var texture in images)
            {
                var textureAsset = new TextureAsset
                {
                    Id = AssetId.Empty, // CAUTION: It is important to use an empty GUID here, as we don't want the command to be rebuilt (by default, a new asset is creating a new guid)
                    IsStreamable = false,
                    IsCompressed = false,
                    GenerateMipmaps = true,
                    Type = new ColorTextureType
                    {
                        Alpha = AlphaFormat.Auto,
                        PremultiplyAlpha = true,
                        UseSRgbSampling = true,
                    }
                };

                var textureConvertParameters = new TextureConvertParameters(texture, textureAsset, context.Platform, context.GetGraphicsPlatform(assetItem.Package), renderingSettings.DefaultGraphicsProfile, gameSettingsAsset.GetOrCreate<TextureSettings>().TextureQuality, colorSpace);
                var textureConvertCommand = new TextureAssetCompiler.TextureConvertCommand(targetUrlInStorage + texIndex, textureConvertParameters, assetItem.Package);
                result.BuildSteps.Add(textureConvertCommand);

                asset.BuildTextures.Add(targetUrlInStorage + texIndex);

                texIndex++;
            }

            var step = new AssetBuildStep(assetItem);
            step.Add(new SpriteStudioModelAssetCommand(targetUrlInStorage, asset, colorSpace, assetItem.Package));
            result.BuildSteps.Add(step);
        }

        /// <summary>
        /// Command used by the build engine to convert the asset
        /// </summary>
        private class SpriteStudioModelAssetCommand : AssetCommand<SpriteStudioModelAsset>
        {
            private readonly ColorSpace colorSpace;

            public SpriteStudioModelAssetCommand(string url, SpriteStudioModelAsset asset, ColorSpace colorSpace, IAssetFinder assetFinder)
                : base(url, asset, assetFinder)
            {
                this.colorSpace = colorSpace;
                Version = 2;
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var nodes = new List<SpriteStudioNode>();
                string modelName;
                if (!SpriteStudioXmlImport.ParseModel(Parameters.Source, nodes, out modelName)) return null;

                var cells = new List<SpriteStudioCell>();
                var textures = new List<UFile>();
                if (!SpriteStudioXmlImport.ParseCellMaps(Parameters.Source, textures, cells)) return null;

                var anims = new List<SpriteStudioAnim>();
                if (!SpriteStudioXmlImport.ParseAnimations(Parameters.Source, anims)) return null;             

                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

                var sheet = new SpriteSheet();

                foreach (var cell in cells)
                {
                    var sprite = new Sprite(cell.Name, AttachedReferenceManager.CreateProxyObject<Texture>(AssetId.Empty, Parameters.BuildTextures[cell.TextureIndex]))
                    {
                        Region = cell.Rectangle,
                        Center = cell.Pivot,
                        IsTransparent = true
                    };
                    sheet.Sprites.Add(sprite);
                }

                var nodeMapping = nodes.Select((x, i) => new { Name = x.Name, Index = i }).ToDictionary(x => x.Name, x => x.Index);

                //fill up some basic data for our model using the first animation in the array
                var anim = anims[0];
                foreach (var data in anim.NodesData)
                {
                    int nodeIndex;
                    if (!nodeMapping.TryGetValue(data.Key, out nodeIndex))
                        continue;

                    var node = nodes[nodeIndex];

                    foreach (var pair in data.Value.Data)
                    {
                        var tag = pair.Key;
                        if (pair.Value.All(x => x["time"] != "0")) continue;
                        var value = pair.Value.First()["value"]; //do we always have a frame 0? should be the case actually
                        switch (tag)
                        {
                            case "POSX":
                                node.BaseState.Position.X = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "POSY":
                                node.BaseState.Position.Y = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "ROTZ":
                                node.BaseState.RotationZ = MathUtil.DegreesToRadians(float.Parse(value, CultureInfo.InvariantCulture));
                                break;
                            case "PRIO":
                                node.BaseState.Priority = int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "SCLX":
                                node.BaseState.Scale.X = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "SCLY":
                                node.BaseState.Scale.Y = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "ALPH":
                                node.BaseState.Transparency = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "HIDE":
                                node.BaseState.Hide = int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "FLPH":
                                node.BaseState.HFlipped = int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "FLPV":
                                node.BaseState.VFlipped = int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "CELL":
                                node.BaseState.SpriteId = int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "COLV":
                                var color = new Color4(Color.FromBgra(int.Parse(value, CultureInfo.InvariantCulture)));
                                node.BaseState.BlendColor = colorSpace == ColorSpace.Linear ? color.ToLinear() : color;
                                break;
                            case "COLB":
                                node.BaseState.BlendType = (SpriteStudioBlending)int.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "COLF":
                                node.BaseState.BlendFactor = float.Parse(value, CultureInfo.InvariantCulture);
                                break;
                        }
                    }
                }

                var spriteStudioSheet = new SpriteStudioSheet
                {
                    NodesInfo = nodes,
                    SpriteSheet = sheet
                };

                assetManager.Save(Url, spriteStudioSheet);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
