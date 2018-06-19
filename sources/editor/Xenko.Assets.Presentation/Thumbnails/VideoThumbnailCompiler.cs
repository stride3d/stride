// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Compiler;
using Xenko.Assets.Media;
using Xenko.Assets.Presentation.Resources.Thumbnails;
using Xenko.Editor.Thumbnails;

namespace Xenko.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(VideoAsset), typeof(ThumbnailCompilationContext))]
    public class VideoThumbnailCompiler : StaticThumbnailCompiler<VideoAsset>
    {
        public VideoThumbnailCompiler()
            : base(StaticThumbnails.VideoThumbnail)
        {
        }
    }
}
