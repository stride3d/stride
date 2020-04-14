// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Compiler;

namespace Stride.Editor.Thumbnails
{
    public interface IThumbnailCompiler : IAssetCompiler
    {
        int Priority { get; set; }

        bool IsStatic { get; set; }
    }
}
