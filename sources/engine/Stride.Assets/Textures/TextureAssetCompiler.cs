// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Streaming;
using Stride.Graphics;
using Stride.TextureConverter;
using Stride.Graphics.Data;
using Stride.Streaming;

namespace Stride.Assets.Textures
{
    /// <summary>
    /// Texture asset compiler.
    /// </summary>
    [AssetCompiler(typeof(TextureAsset), typeof(AssetCompilationContext))]
    public class TextureAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(GameSettingsAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileAsset);
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (TextureAsset)assetItem.Asset;
            // Get absolute path of asset source on disk
            var assetSource = GetAbsolutePath(assetItem, asset.Source);

            var gameSettingsAsset = context.GetGameSettingsAsset();
            var colorSpace = context.GetColorSpace();

            var parameter = new TextureConvertParameters(assetSource, asset, context.Platform, context.GetGraphicsPlatform(assetItem.Package), gameSettingsAsset.GetOrCreate<RenderingSettings>(context.Platform).DefaultGraphicsProfile, gameSettingsAsset.GetOrCreate<TextureSettings>().TextureQuality, colorSpace);
            result.BuildSteps = new AssetBuildStep(assetItem);
            result.BuildSteps.Add(new TextureConvertCommand(targetUrlInStorage, parameter, assetItem.Package));
        }

        /// <summary>
        /// Command used to convert the texture in the storage
        /// </summary>
        public class TextureConvertCommand : AssetCommand<TextureConvertParameters>
        {
            public TextureConvertCommand(string url, TextureConvertParameters description, IAssetFinder assetFinder)
                : base(url, description, assetFinder)
            {
                Version = 3;
            }

            public override IEnumerable<ObjectUrl> GetInputFiles()
            {
                yield return new ObjectUrl(UrlType.File, Parameters.SourcePathFromDisk);
            }

            private ResultStatus Import(ICommandContext commandContext, TextureTool textureTool, TexImage texImage, TextureHelper.ImportParameters convertParameters)
            {
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                var useSeparateDataContainer = TextureHelper.ShouldUseDataContainer(Parameters.IsStreamable, texImage.Dimension);

                // Note: for streamable textures we want to store mip maps in a separate storage container and read them on request instead of whole asset deserialization (at once)

                return useSeparateDataContainer
                    ? TextureHelper.ImportStreamableTextureImage(assetManager, textureTool, texImage, convertParameters, CancellationToken, commandContext)
                    : TextureHelper.ImportTextureImage(assetManager, textureTool, texImage, convertParameters, CancellationToken, commandContext.Logger);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var convertParameters = new TextureHelper.ImportParameters(Parameters) { OutputUrl = Url };

                using (var texTool = new TextureTool())
                using (var texImage = texTool.Load(Parameters.SourcePathFromDisk, convertParameters.IsSRgb))
                {
                    var importResult = Import(commandContext, texTool, texImage, convertParameters);

                    return Task.FromResult(importResult);
                }
            }

            protected override void ComputeAssemblyHash(BinarySerializationWriter writer)
            {
                writer.Write(DataSerializer.BinaryFormatVersion);
                writer.Write(TextureSerializationData.Version);

                // Since Image format is quite stable, we want to manually control it's assembly hash here
                writer.Write(1);
            }
        }
    }


    /// <summary>
    /// SharedParameters used for converting/processing the texture in the storage.
    /// </summary>
    [DataContract]
    public class TextureConvertParameters
    {
        public TextureConvertParameters()
        {
        }

        public TextureConvertParameters(
            UFile sourcePathFromDisk, 
            TextureAsset texture, 
            PlatformType platform, 
            GraphicsPlatform graphicsPlatform, 
            GraphicsProfile graphicsProfile, 
            TextureQuality textureQuality,
            ColorSpace colorSpace)
        {
            SourcePathFromDisk = sourcePathFromDisk;
            Texture = texture;
            IsStreamable = texture.IsStreamable;
            Platform = platform;
            GraphicsPlatform = graphicsPlatform;
            GraphicsProfile = graphicsProfile;
            TextureQuality = textureQuality;
            ColorSpace = colorSpace;
        }

        public UFile SourcePathFromDisk;

        public TextureAsset Texture;

        public bool IsStreamable;
        
        public PlatformType Platform;

        public GraphicsPlatform GraphicsPlatform;

        public GraphicsProfile GraphicsProfile;

        public TextureQuality TextureQuality;

        public ColorSpace ColorSpace;
    }
}
