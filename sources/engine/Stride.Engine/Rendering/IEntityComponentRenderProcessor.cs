// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;

namespace Stride.Rendering
{
    /// <summary>
    /// An <see cref="EntityProcessor"/> dedicated for rendering.
    /// </summary>
    /// Note that it might be instantiated multiple times in a given <see cref="SceneInstance"/>.
    public interface IEntityComponentRenderProcessor
    {
        VisibilityGroup VisibilityGroup { get; set; }
    }
}
