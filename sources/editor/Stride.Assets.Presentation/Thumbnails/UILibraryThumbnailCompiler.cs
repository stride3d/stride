// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Compiler;
using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Assets.UI;
using Stride.Editor.Thumbnails;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(UILibraryAsset), typeof(ThumbnailCompilationContext))]
    public class UILibraryThumbnailCompiler : StaticThumbnailCompiler<UILibraryAsset>
    {
        public UILibraryThumbnailCompiler()
            : base(StaticThumbnails.UILibraryThumbnail)
        {
        }
    }
}
