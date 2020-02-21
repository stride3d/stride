// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Assets.Textures;
using Xenko.Assets.Textures.Packing;
using Xenko.Graphics;
using Xenko.TextureConverter;
using Xenko.Graphics.Data;

namespace Xenko.Assets.Sprite
{
    /// <summary>
    /// The <see cref="SpriteSheetAsset"/> compiler.
    /// </summary>
    [AssetCompiler(typeof(SpriteSheetAsset), typeof(AssetCompilationContext))]
    public class SpriteSheetAssetCompiler : AssetCompilerBase 
    {
        private static bool TextureFileIsValid(UFile file)
        {
            return file != null && File.Exists(file);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SpriteSheetAsset)assetItem.Asset;
            var gameSettingsAsset = context.GetGameSettingsAsset();
            var renderingSettings = gameSettingsAsset.GetOrCreate<RenderingSettings>(context.Platform);

            result.BuildSteps = new ListBuildStep();

            var prereqSteps = new Queue<BuildStep>();
            
            // create the registry containing the sprite assets texture index association
            var imageToTextureUrl = new Dictionary<SpriteInfo, string>();

            var colorSpace = context.GetColorSpace();

            // create and add import texture commands
            if (asset.Sprites != null && !asset.Packing.Enabled)
            {
                // sort sprites by referenced texture.
                var spriteByTextures = asset.Sprites.GroupBy(x => x.Source).ToArray();
                for (int i = 0; i < spriteByTextures.Length; i++)
                {
                    // skip the texture if the file is not valid.
                    var textureFile = spriteByTextures[i].Key;
                    if (!TextureFileIsValid(textureFile))
                        continue;

                    var textureUrl = SpriteSheetAsset.BuildTextureUrl(assetItem.Location, i);

                    var spriteAssetArray = spriteByTextures[i].ToArray();
                    foreach (var spriteAsset in spriteAssetArray)
                        imageToTextureUrl[spriteAsset] = textureUrl;

                    // create an texture asset.
                    var textureAsset = new TextureAsset
                    {
                        Id = AssetId.Empty, // CAUTION: It is important to use an empty GUID here, as we don't want the command to be rebuilt (by default, a new asset is creating a new guid)
                        IsStreamable = asset.IsStreamable && asset.Type != SpriteSheetType.UI,
                        IsCompressed = asset.IsCompressed,
                        GenerateMipmaps = asset.GenerateMipmaps,
                        Type = new ColorTextureType
                        {
                            Alpha = asset.Alpha,
                            PremultiplyAlpha = asset.PremultiplyAlpha,
                            ColorKeyColor = asset.ColorKeyColor,
                            ColorKeyEnabled = asset.ColorKeyEnabled,
                            UseSRgbSampling = true,
                        }
                    };

                    // Get absolute path of asset source on disk
                    var assetDirectory = assetItem.FullPath.GetParent();
                    var assetSource = UPath.Combine(assetDirectory, spriteAssetArray[0].Source);

                    // add the texture build command.
                    var textureConvertParameters = new TextureConvertParameters(assetSource, textureAsset, context.Platform, context.GetGraphicsPlatform(assetItem.Package), renderingSettings.DefaultGraphicsProfile, gameSettingsAsset.GetOrCreate<TextureSettings>().TextureQuality, colorSpace);
                    var textureConvertCommand = new TextureAssetCompiler.TextureConvertCommand(textureUrl, textureConvertParameters, assetItem.Package);
                    var assetBuildStep = new AssetBuildStep(new AssetItem(textureUrl, textureAsset));
                    assetBuildStep.Add(textureConvertCommand);
                    prereqSteps.Enqueue(assetBuildStep);
                    result.BuildSteps.Add(assetBuildStep);
                }
            }

            if (!result.HasErrors)
            {
                var parameters = new SpriteSheetParameters(asset, imageToTextureUrl, context.Platform, context.GetGraphicsPlatform(assetItem.Package), renderingSettings.DefaultGraphicsProfile, gameSettingsAsset.GetOrCreate<TextureSettings>().TextureQuality, colorSpace);

                var assetBuildStep = new AssetBuildStep(assetItem);
                assetBuildStep.Add(new SpriteSheetCommand(targetUrlInStorage, parameters, assetItem.Package));
                result.BuildSteps.Add(assetBuildStep);

                while (prereqSteps.Count > 0)
                {
                    var prereq = prereqSteps.Dequeue();
                    BuildStep.LinkBuildSteps(prereq, assetBuildStep);
                }
            }
        }

        /// <summary>
        /// Command used to convert the texture in the storage
        /// </summary>
        public class SpriteSheetCommand : AssetCommand<SpriteSheetParameters>
        {
            public SpriteSheetCommand(string url, SpriteSheetParameters parameters, IAssetFinder assetFinder)
                : base(url, parameters, assetFinder)
            {
                Version = 2;
            }

            protected override void ComputeAssemblyHash(BinarySerializationWriter writer)
            {
                base.ComputeAssemblyHash(writer);

                // If texture format changes, we want to compile again
                writer.Write(TextureSerializationData.Version);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Serializer.RegisterSerializer(new ImageTextureSerializer());

                // Create atlas texture
                Dictionary<SpriteInfo, PackedSpriteInfo> spriteToPackedSprite = null;

                // Generate texture atlas
                var isPacking = Parameters.SheetAsset.Packing.Enabled;
                if (isPacking)
                {
                    var resultStatus = CreateAtlasTextures(commandContext, out spriteToPackedSprite);

                    if (resultStatus != ResultStatus.Successful)
                        return Task.FromResult(resultStatus);
                }

                var imageGroupData = new SpriteSheet();

                // add the sprite data to the sprite list.
                foreach (var image in Parameters.SheetAsset.Sprites)
                {
                    string textureUrl;
                    RectangleF region;
                    ImageOrientation orientation;

                    var borders = image.Borders;
                    var center = image.Center + (image.CenterFromMiddle ? new Vector2(image.TextureRegion.Width, image.TextureRegion.Height) / 2 : Vector2.Zero);

                    if (isPacking
                        && spriteToPackedSprite.TryGetValue(image, out var packedSprite)) // ensure that unpackable elements (invalid because of null size/texture) are properly added in the sheet using the normal path
                    {
                        var isOriginalSpriteRotated = image.Orientation == ImageOrientation.Rotated90;

                        region = packedSprite.Region;
                        orientation = (packedSprite.IsRotated ^ isOriginalSpriteRotated) ? ImageOrientation.Rotated90 : ImageOrientation.AsIs;
                        textureUrl = SpriteSheetAsset.BuildTextureAtlasUrl(Url, spriteToPackedSprite[image].AtlasTextureIndex);

                        // update the center and border info, if the packer rotated the sprite 
                        // note: X->Left, Y->Top, Z->Right, W->Bottom.
                        if (packedSprite.IsRotated)
                        {
                            // turned the sprite CCW
                            if (isOriginalSpriteRotated)
                            {
                                var oldCenterX = center.X;
                                center.X = center.Y;
                                center.Y = region.Height - oldCenterX;

                                var oldBorderW = borders.W;
                                borders.W = borders.X;
                                borders.X = borders.Y;
                                borders.Y = borders.Z;
                                borders.Z = oldBorderW;
                            }
                            else // turned the sprite CW
                            {
                                var oldCenterX = center.X;
                                center.X = region.Width - center.Y;
                                center.Y = oldCenterX;

                                var oldBorderW = borders.W;
                                borders.W = borders.Z;
                                borders.Z = borders.Y;
                                borders.Y = borders.X;
                                borders.X = oldBorderW;
                            }
                        }
                    }
                    else
                    {
                        region = image.TextureRegion;
                        orientation = image.Orientation;
                        Parameters.ImageToTextureUrl.TryGetValue(image, out textureUrl);
                    }

                    // Affect the texture
                    Texture texture = null;
                    if (textureUrl != null)
                    {
                        texture = AttachedReferenceManager.CreateProxyObject<Texture>(AssetId.Empty, textureUrl);
                    }
                    else
                    {
                        commandContext.Logger.Warning($"Image '{image.Name}' has an invalid image source file '{image.Source}', resulting texture will be null.");
                    }

                    imageGroupData.Sprites.Add(new Graphics.Sprite
                    {
                        Name = image.Name,
                        Region = region,
                        Orientation = orientation,
                        Center = center,
                        Borders = borders,
                        PixelsPerUnit = new Vector2(image.PixelsPerUnit),
                        Texture = texture,
                        IsTransparent = false,
                    });
                }

                // set the transparency information to all the sprites
                if (Parameters.SheetAsset.Alpha != AlphaFormat.None) // Skip the calculation when format is forced without alpha.
                {
                    var urlToTexImage = new Dictionary<string, Tuple<TexImage, Image>>();
                    using (var texTool = new TextureTool())
                    {
                        foreach (var sprite in imageGroupData.Sprites)
                        {
                            if (sprite.Texture == null) // the sprite texture is invalid
                                continue;

                            var textureUrl = AttachedReferenceManager.GetOrCreateAttachedReference(sprite.Texture).Url;
                            if (!urlToTexImage.ContainsKey(textureUrl))
                            {
                                var image = assetManager.Load<Image>(textureUrl);
                                var newTexImage = texTool.Load(image, false);// the sRGB mode does not impact on the alpha level
                                texTool.Decompress(newTexImage, false);// the sRGB mode does not impact on the alpha level
                                urlToTexImage[textureUrl] = Tuple.Create(newTexImage, image);
                            }
                            var texImage = urlToTexImage[textureUrl].Item1;

                            var region = new Rectangle
                            {
                                X = (int)Math.Floor(sprite.Region.X),
                                Y = (int)Math.Floor(sprite.Region.Y)
                            };
                            region.Width = (int)Math.Ceiling(sprite.Region.Right) - region.X;
                            region.Height = (int)Math.Ceiling(sprite.Region.Bottom) - region.Y;

                            var alphaLevel = texTool.GetAlphaLevels(texImage, region, null, commandContext.Logger); // ignore transparent color key here because the input image has already been processed
                            sprite.IsTransparent = alphaLevel != AlphaLevels.NoAlpha; 
                        }

                        // free all the allocated images
                        foreach (var tuple in urlToTexImage.Values)
                        {
                            tuple.Item1.Dispose();
                            assetManager.Unload(tuple.Item2);
                        }
                    }
                }

                // save the imageData into the data base
                assetManager.Save(Url, imageGroupData);

                return Task.FromResult(ResultStatus.Successful);
            }

            /// <summary>
            /// Creates and Saves texture atlas image from images in GroupAsset
            /// </summary>
            /// <param name="commandContext">The command context</param>
            /// <param name="spriteToPackedSprite">A map associating the packed sprite info to the original sprite</param>
            /// <returns>Status of building</returns>
            private ResultStatus CreateAtlasTextures(ICommandContext commandContext, out Dictionary<SpriteInfo, PackedSpriteInfo> spriteToPackedSprite)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                spriteToPackedSprite = new Dictionary<SpriteInfo, PackedSpriteInfo>();

                // Pack textures
                using (var texTool = new TextureTool())
                {
                    var textureElements = new List<AtlasTextureElement>();

                    // Input textures
                    var imageDictionary = new Dictionary<string, Image>();
                    var imageInfoDictionary = new Dictionary<string, SpriteInfo>();

                    var sprites = Parameters.SheetAsset.Sprites;
                    var packingParameters = Parameters.SheetAsset.Packing;
                    bool isSRgb = Parameters.SheetAsset.IsSRGBTexture(Parameters.ColorSpace);

                    for (var i = 0; i < sprites.Count; ++i)
                    {
                        var sprite = sprites[i];
                        if (sprite.TextureRegion.Height == 0 || sprite.TextureRegion.Width == 0 || sprite.Source == null)
                            continue;

                        // Lazy load input texture and cache in the dictionary for the later use
                        Image texture;

                        if (!imageDictionary.ContainsKey(sprite.Source))
                        {
                            texture = LoadImage(texTool, new UFile(sprite.Source), isSRgb);
                            imageDictionary[sprite.Source] = texture;
                        }
                        else
                        {
                            texture = imageDictionary[sprite.Source];
                        }

                        var key = Url + "_" + i;

                        var sourceRectangle = new RotableRectangle(sprite.TextureRegion, sprite.Orientation == ImageOrientation.Rotated90);
                        textureElements.Add(new AtlasTextureElement(key, texture, sourceRectangle, packingParameters.BorderSize, sprite.BorderModeU, sprite.BorderModeV, sprite.BorderColor));

                        imageInfoDictionary[key] = sprite;
                    }

                    // find the maximum texture size supported
                    var maximumSize = TextureHelper.FindMaximumTextureSize(new TextureHelper.ImportParameters(Parameters), new Size2(int.MaxValue/2, int.MaxValue/2));

                    // Initialize packing configuration from GroupAsset
                    var texturePacker = new TexturePacker
                    {
                        Algorithm = packingParameters.PackingAlgorithm,
                        AllowMultipack = packingParameters.AllowMultipacking,
                        MaxWidth = maximumSize.Width,
                        MaxHeight = maximumSize.Height,
                        AllowRotation = packingParameters.AllowRotations,
                    };

                    var canPackAllTextures = texturePacker.PackTextures(textureElements);

                    if (!canPackAllTextures)
                    {
                        commandContext.Logger.Error("Failed to pack all textures");
                        return ResultStatus.Failed;
                    }

                    // Create and save every generated texture atlas
                    for (var textureAtlasIndex = 0; textureAtlasIndex < texturePacker.AtlasTextureLayouts.Count; ++textureAtlasIndex)
                    {
                        var atlasLayout = texturePacker.AtlasTextureLayouts[textureAtlasIndex];

                        ResultStatus resultStatus;
                        using (var atlasImage = AtlasTextureFactory.CreateTextureAtlas(atlasLayout, isSRgb))
                        using (var texImage = texTool.Load(atlasImage, isSRgb))
                        {
                            var outputUrl = SpriteSheetAsset.BuildTextureAtlasUrl(Url, textureAtlasIndex);
                            var convertParameters = new TextureHelper.ImportParameters(Parameters) { OutputUrl = outputUrl };
                            resultStatus = TextureHelper.ShouldUseDataContainer(Parameters.SheetAsset.IsStreamable && Parameters.SheetAsset.Type != SpriteSheetType.UI, texImage.Dimension)? 
                                TextureHelper.ImportStreamableTextureImage(assetManager, texTool, texImage, convertParameters, CancellationToken, commandContext) : 
                                TextureHelper.ImportTextureImage(assetManager, texTool, texImage, convertParameters, CancellationToken, commandContext.Logger);
                        }

                        foreach (var texture in atlasLayout.Textures)
                            spriteToPackedSprite.Add(imageInfoDictionary[texture.Name], new PackedSpriteInfo(texture.DestinationRegion, textureAtlasIndex, packingParameters.BorderSize));

                        if (resultStatus != ResultStatus.Successful)
                        {
                            // Dispose used textures
                            foreach (var image in imageDictionary.Values)
                                image.Dispose();

                            return resultStatus;
                        }
                    }

                    // Dispose used textures
                    foreach (var image in imageDictionary.Values)
                        image.Dispose();
                }

                return ResultStatus.Successful;
            }

            /// <summary>
            /// Loads image from a path with texTool
            /// </summary>
            /// <param name="texTool">A tool for loading an image</param>
            /// <param name="sourcePath">Source path of an image</param>
            /// <param name="isSRgb">Indicate if the texture to load is sRGB</param>
            /// <returns></returns>
            private static Image LoadImage(TextureTool texTool, UFile sourcePath, bool isSRgb)
            {
                using (var texImage = texTool.Load(sourcePath, isSRgb))
                {
                    texTool.Decompress(texImage, isSRgb);

                    if (texImage.Format == PixelFormat.B8G8R8A8_UNorm || texImage.Format == PixelFormat.B8G8R8A8_UNorm_SRgb)
                        texTool.SwitchChannel(texImage);

                    return texTool.ConvertToXenkoImage(texImage);
                }
            }

            private class PackedSpriteInfo
            {
                private RotableRectangle packedRectangle;
                private readonly float borderSize;

                /// <summary>
                /// The index of the atlas texture the sprite has been packed in.
                /// </summary>
                public int AtlasTextureIndex { get; }

                /// <summary>
                /// Gets the region of the packed sprite.
                /// </summary>
                public RectangleF Region => new RectangleF(
                    borderSize + packedRectangle.X, 
                    borderSize + packedRectangle.Y,
                    packedRectangle.Width - 2 * borderSize,
                    packedRectangle.Height - 2 * borderSize);

                /// <summary>
                /// Indicate if the packed sprite have been rotated.
                /// </summary>
                public bool IsRotated => packedRectangle.IsRotated;

                public PackedSpriteInfo(RotableRectangle packedRectangle, int atlasTextureIndex, float borderSize)
                {
                    this.packedRectangle = packedRectangle;
                    this.borderSize = borderSize;
                    AtlasTextureIndex = atlasTextureIndex;
                }
            }
        }

        /// <summary>
        /// SharedParameters used for converting/processing the texture in the storage.
        /// </summary>
        [DataContract]
        public class SpriteSheetParameters
        {
            public SpriteSheetParameters()
            {
            }

            public SpriteSheetParameters(SpriteSheetAsset sheetAsset, Dictionary<SpriteInfo, string> imageToTextureUrl, 
                PlatformType platform, GraphicsPlatform graphicsPlatform, GraphicsProfile graphicsProfile, TextureQuality textureQuality, ColorSpace colorSpace)
            {
                ImageToTextureUrl = imageToTextureUrl;
                SheetAsset = sheetAsset;
                Platform = platform;
                GraphicsPlatform = graphicsPlatform;
                GraphicsProfile = graphicsProfile;
                TextureQuality = textureQuality;
                ColorSpace = colorSpace;
            }

            public SpriteSheetAsset SheetAsset;

            public PlatformType Platform;

            public GraphicsPlatform GraphicsPlatform;

            public GraphicsProfile GraphicsProfile;

            public TextureQuality TextureQuality;

            public ColorSpace ColorSpace;

            public Dictionary<SpriteInfo, string> ImageToTextureUrl { get; set; }
        } 
    }
}
