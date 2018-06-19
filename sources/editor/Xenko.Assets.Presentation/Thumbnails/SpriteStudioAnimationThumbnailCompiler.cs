// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Compiler;
using Xenko.Assets.Models;
using Xenko.Assets.Presentation.Resources.Thumbnails;
using Xenko.Editor.Resources;
using Xenko.Editor.Thumbnails;
using Xenko.SpriteStudio.Offline;

namespace Xenko.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(SpriteStudioAnimationAsset), typeof(ThumbnailCompilationContext))]
    public class SpriteStudioAnimationThumbnailCompiler : StaticThumbnailCompiler<SpriteStudioAnimationAsset>
    {
        public SpriteStudioAnimationThumbnailCompiler()
            : base(StaticThumbnails.AnimationThumbnail)
        {
        }
    }
}
