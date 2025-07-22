// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Compiler;
using Stride.Assets.Media;
using Stride.Assets.Presentation.Resources.Thumbnails;
using Stride.Editor.Thumbnails;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(SoundAsset), typeof(ThumbnailCompilationContext))]
    public class SoundThumbnailCompiler : StaticThumbnailCompiler<SoundAsset>
    {
        public SoundThumbnailCompiler()
            : base(StaticThumbnails.SoundThumbnail)
        {
        }
    }
}
