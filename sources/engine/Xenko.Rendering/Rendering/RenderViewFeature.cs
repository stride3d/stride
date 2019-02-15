// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Threading;

namespace Xenko.Rendering
{
    /// <summary>
    /// Describes a specific <see cref="RenderView"/> and <see cref="RootRenderFeature"/> combination.
    /// </summary>
    public class RenderViewFeature
    {
        public RootRenderFeature RootFeature;

        /// <summary>
        /// List of render nodes. It might cover multiple RenderStage, RenderStages contains range information.
        /// </summary>
        public readonly ConcurrentCollector<RenderNodeReference> RenderNodes = new ConcurrentCollector<RenderNodeReference>();

        /// <summary>
        /// The list of object nodes contained in this view.
        /// </summary>
        public readonly ConcurrentCollector<ViewObjectNodeReference> ViewObjectNodes = new ConcurrentCollector<ViewObjectNodeReference>();

        /// <summary>
        /// List of resource layouts used by this render view.
        /// </summary>
        public readonly ConcurrentCollector<ViewResourceGroupLayout> Layouts = new ConcurrentCollector<ViewResourceGroupLayout>();
    }
}
