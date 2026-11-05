// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Assets.Compiler;
using Stride.Assets.Entities;
using Stride.Editor.Resources;
using Stride.Editor.Thumbnails;
using Stride.Assets.Editor.Resources.Thumbnails;

namespace Stride.Assets.Editor.Thumbnails
{
    [AssetCompiler(typeof(SceneAsset), typeof(ThumbnailCompilationContext))]
    public class SceneThumbnailCompiler : StaticThumbnailCompiler<SceneAsset>
    {
        public SceneThumbnailCompiler()
            : base(StaticThumbnails.SceneThumbnail)
        {
        }
    }
}
