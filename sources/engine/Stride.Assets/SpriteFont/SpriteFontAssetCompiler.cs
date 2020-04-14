// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable 162 // Unreachable code detected (due to useCacheFonts)
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Assets.SpriteFont.Compiler;
using Stride.Graphics;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont
{
    [AssetCompiler(typeof(SpriteFontAsset), typeof(AssetCompilationContext))]
    public class SpriteFontAssetCompiler : AssetCompilerBase
    {
        private static readonly FontDataFactory FontDataFactory = new FontDataFactory();

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SpriteFontAsset)assetItem.Asset;
            UFile assetAbsolutePath = assetItem.FullPath;
            var colorSpace = context.GetColorSpace();

            var fontTypeSdf = asset.FontType as SignedDistanceFieldSpriteFontType;
            if (fontTypeSdf != null)
            {
                // copy the asset and transform the source and character set file path to absolute paths
                var assetClone = AssetCloner.Clone(asset);
                var assetDirectory = assetAbsolutePath.GetParent();
                assetClone.FontSource = asset.FontSource;
                fontTypeSdf.CharacterSet = !string.IsNullOrEmpty(fontTypeSdf.CharacterSet) ? UPath.Combine(assetDirectory, fontTypeSdf.CharacterSet) : null;

                result.BuildSteps = new AssetBuildStep(assetItem);
                result.BuildSteps.Add(new SignedDistanceFieldFontCommand(targetUrlInStorage, assetClone, assetItem.Package));
            }
            else
                if (asset.FontType is RuntimeRasterizedSpriteFontType)
                {
                    UFile fontPathOnDisk = asset.FontSource.GetFontPath(result);
                    if (fontPathOnDisk == null)
                    {
                        result.Error($"Runtime rasterized font compilation failed. Font {asset.FontSource.GetFontName()} was not found on this machine.");
                        result.BuildSteps = new AssetBuildStep(assetItem);
                        result.BuildSteps.Add(new FailedFontCommand());
                        return;
                    }

                    var fontImportLocation = FontHelper.GetFontPath(asset.FontSource.GetFontName(), asset.FontSource.Style);

                    result.BuildSteps = new AssetBuildStep(assetItem);
                    result.BuildSteps.Add(new ImportStreamCommand { SourcePath = fontPathOnDisk, Location = fontImportLocation });
                    result.BuildSteps.Add(new RuntimeRasterizedFontCommand(targetUrlInStorage, asset, assetItem.Package));
                }
                else
                {
                    var fontTypeStatic = asset.FontType as OfflineRasterizedSpriteFontType;
                    if (fontTypeStatic == null)
                        throw new ArgumentException("Tried to compile a non-offline rasterized sprite font with the compiler for offline resterized fonts!");

                    // copy the asset and transform the source and character set file path to absolute paths
                    var assetClone = AssetCloner.Clone(asset);
                    var assetDirectory = assetAbsolutePath.GetParent();
                    assetClone.FontSource = asset.FontSource;
                    fontTypeStatic.CharacterSet = !string.IsNullOrEmpty(fontTypeStatic.CharacterSet) ? UPath.Combine(assetDirectory, fontTypeStatic.CharacterSet): null;

                    result.BuildSteps = new AssetBuildStep(assetItem);
                    result.BuildSteps.Add(new OfflineRasterizedFontCommand(targetUrlInStorage, assetClone, colorSpace, assetItem.Package));
                }
        }

        internal class OfflineRasterizedFontCommand : AssetCommand<SpriteFontAsset>
        {
            private ColorSpace colorspace;

            public OfflineRasterizedFontCommand(string url, SpriteFontAsset description, ColorSpace colorspace, IAssetFinder assetFinder)
                : base(url, description, assetFinder)
            {
                this.colorspace = colorspace;
            }

            public override IEnumerable<ObjectUrl> GetInputFiles()
            {
                var asset = Parameters;
                var fontTypeStatic = asset.FontType as OfflineRasterizedSpriteFontType;
                if (fontTypeStatic != null)
                {
                    if (File.Exists(fontTypeStatic.CharacterSet))
                        yield return new ObjectUrl(UrlType.File, fontTypeStatic.CharacterSet);
                }

                var fontTypeSdf = asset.FontType as SignedDistanceFieldSpriteFontType;
                if (fontTypeSdf != null)
                {
                    if (File.Exists(fontTypeSdf.CharacterSet))
                        yield return new ObjectUrl(UrlType.File, fontTypeSdf.CharacterSet);

                }
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);
                writer.Write(colorspace);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // try to import the font from the original bitmap or ttf file
                Graphics.SpriteFont staticFont;
                try
                {
                    staticFont = OfflineRasterizedFontCompiler.Compile(FontDataFactory, Parameters, colorspace == ColorSpace.Linear);
                }
                catch (FontNotFoundException ex) 
                {
                    commandContext.Logger.Error($"Font [{ex.FontName}] was not found on this machine.", ex);
                    return Task.FromResult(ResultStatus.Failed);
                }

                // check that the font data is valid
                if (staticFont == null || staticFont.Textures.Count == 0)
                    return Task.FromResult(ResultStatus.Failed);

                // save the data into the database
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Save(Url, staticFont);

                // dispose textures allocated by the StaticFontCompiler
                foreach (var texture in staticFont.Textures)
                    texture.Dispose();

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        /// <summary>
        /// Scalable (SDF) font build step
        /// </summary>
        internal class SignedDistanceFieldFontCommand : AssetCommand<SpriteFontAsset>
        {
            public SignedDistanceFieldFontCommand(string url, SpriteFontAsset description, IAssetFinder assetFinder)
                : base(url, description, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // try to import the font from the original bitmap or ttf file
                Graphics.SpriteFont scalableFont;
                try
                {
                    scalableFont = SignedDistanceFieldFontCompiler.Compile(FontDataFactory, Parameters);
                }
                catch (FontNotFoundException ex)
                {
                    commandContext.Logger.Error($"Font [{ex.FontName}] was not found on this machine.", ex);
                    return Task.FromResult(ResultStatus.Failed);
                }

                // check that the font data is valid
                if (scalableFont == null || scalableFont.Textures.Count == 0)
                    return Task.FromResult(ResultStatus.Failed);

                // save the data into the database
                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Save(Url, scalableFont);

                // dispose textures allocated by the StaticFontCompiler
                foreach (var texture in scalableFont.Textures)
                    texture.Dispose();

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        internal class RuntimeRasterizedFontCommand : AssetCommand<SpriteFontAsset>
        {
            public RuntimeRasterizedFontCommand(string url, SpriteFontAsset description, IAssetFinder assetFinder)
                : base(url, description, assetFinder)
            {
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var dynamicFont = FontDataFactory.NewDynamic(
                    Parameters.FontType.Size, Parameters.FontSource.GetFontName(), Parameters.FontSource.Style, 
                    Parameters.FontType.AntiAlias, useKerning:false, extraSpacing:Parameters.Spacing, extraLineSpacing:Parameters.LineSpacing, 
                    defaultCharacter:Parameters.DefaultCharacter);

                var assetManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
                assetManager.Save(Url, dynamicFont);

                return Task.FromResult(ResultStatus.Successful);
            }
        }

        /// <summary>
        /// Proxy command which always fails, called when font is compiled with the wrong assets
        /// </summary>
        internal class FailedFontCommand : AssetCommand<SpriteFontAsset>
        {
            public FailedFontCommand() : base(null, null, null) { }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                return Task.FromResult(ResultStatus.Failed);
            }
        }
    }
}
