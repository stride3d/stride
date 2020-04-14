// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Compiler;
using Xenko.Assets.Presentation.Resources.Thumbnails;
using Xenko.Assets.Scripts;
using Xenko.Editor.Thumbnails;

namespace Xenko.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(ScriptSourceFileAsset), typeof(ThumbnailCompilationContext))]
    public class ScriptSourceFileThumbnailCompiler : StaticThumbnailCompiler<ScriptSourceFileAsset>
    {
        public ScriptSourceFileThumbnailCompiler()
            : base(StaticThumbnails.ScriptSourceFileThumbnail)
        {
        }
    }
}
