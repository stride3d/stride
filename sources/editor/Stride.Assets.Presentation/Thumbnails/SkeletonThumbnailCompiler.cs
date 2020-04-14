// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Compiler;
using Xenko.Assets.Models;
using Xenko.Assets.Presentation.Resources.Thumbnails;
using Xenko.Editor.Thumbnails;

namespace Xenko.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(SkeletonAsset), typeof(ThumbnailCompilationContext))]
    public class SkeletonThumbnailCompiler : StaticThumbnailCompiler<SkeletonAsset>
    {
        public SkeletonThumbnailCompiler()
            : base(StaticThumbnails.SkeletonThumbnail)
        {
        }
    }
}
