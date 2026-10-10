// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Core.Assets.Compiler;
using Stride.Editor.Thumbnails;
using Stride.SpriteStudio.Assets;

namespace Stride.SpriteStudio.Editor;

[AssetCompiler(typeof(SpriteStudioAnimationAsset), typeof(ThumbnailCompilationContext))]
public class SpriteStudioAnimationThumbnailCompiler : StaticThumbnailCompiler<SpriteStudioAnimationAsset>
{
    public SpriteStudioAnimationThumbnailCompiler()
        : base(StaticThumbnails.AnimationThumbnail)
    {
    }
}
