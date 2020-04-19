// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Rendering
{
    /// <summary>
    /// Defines a way to filter RenderObject.
    /// </summary>
    [DataContract("RenderStageFilter")]
    public abstract class RenderStageFilter
    {
        public abstract bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage);
    }
}
