// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Compiler;
using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Assets.Skyboxes;
using Stride.Editor.Thumbnails;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(SkyboxAsset), typeof(ThumbnailCompilationContext))]
    public class SkyboxThumbnailCompiler : StaticThumbnailCompiler<SkyboxAsset>
    {
        public SkyboxThumbnailCompiler()
            : base(StaticThumbnails.SkyboxThumbnail)
        {
        }
    }
}
