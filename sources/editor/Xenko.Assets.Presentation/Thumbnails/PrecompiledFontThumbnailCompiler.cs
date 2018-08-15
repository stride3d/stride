// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.BuildEngine;
using Xenko.Assets.SpriteFont;
using Xenko.Editor.Thumbnails;

namespace Xenko.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(PrecompiledSpriteFontAsset), typeof(ThumbnailCompilationContext))]
    [Obsolete("The PrecompiledSpriteFontAsset will be removed soon")]
    public class PrecompiledFontThumbnailCompiler : ThumbnailCompilerBase<PrecompiledSpriteFontAsset>
    {
        public PrecompiledFontThumbnailCompiler()
        {
            IsStatic = false;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new ThumbnailBuildStep(new PrecompiledFontBuildCommand(context, thumbnailStorageUrl, assetItem, originalPackage,
                new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
        }

        private class PrecompiledFontBuildCommand : FontThumbnailBuildCommand
        {
            public PrecompiledFontBuildCommand(ThumbnailCompilerContext context, string url, AssetItem assetItem, IAssetFinder assetFinder, ThumbnailCommandParameters description) 
                : base(context, url, assetItem, assetFinder, description)
            {
            }

            protected override string BuildTitleText()
            {
                var asset = (PrecompiledSpriteFontAsset)Parameters.Asset;
                var hasNoFontName = string.IsNullOrEmpty(asset.FontName);
            
                var splitedFontName = SpitStringIntoSeveralLines(asset.FontName ?? asset.FontDataFile.GetFileNameWithoutExtension()); // if the name is too long we insert line returns

                return "Precompiled" + "\n" + splitedFontName + (hasNoFontName? "" : "\n" + asset.Size + " " + asset.Style);
            }
        }
    }
}
