// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Compiler;
using Xenko.Assets.Presentation.Resources.Thumbnails;
using Xenko.Assets.Rendering;
using Xenko.Editor.Thumbnails;

namespace Xenko.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(GraphicsCompositorAsset), typeof(ThumbnailCompilationContext))]
    public class GraphicsCompositorThumbnailCompiler : StaticThumbnailCompiler<GraphicsCompositorAsset>
    {
        public GraphicsCompositorThumbnailCompiler()
            : base(StaticThumbnails.GraphicsCompositorThumbnail)
        {
        }
    }
}
