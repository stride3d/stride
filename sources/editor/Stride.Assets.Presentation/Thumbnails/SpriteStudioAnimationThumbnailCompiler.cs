// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Compiler;
using Stride.Assets.Models;
using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Editor.Resources;
using Stride.Editor.Thumbnails;
using Stride.SpriteStudio.Offline;

namespace Stride.Assets.Presentation.Thumbnails
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
