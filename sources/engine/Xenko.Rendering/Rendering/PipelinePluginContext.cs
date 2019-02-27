// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Rendering
{
    /// <summary>
    /// Context used by <see cref="PipelinePluginManager"/>.
    /// </summary>
    public struct PipelinePluginContext
    {
        public readonly RenderContext RenderContext;
        public readonly RenderSystem RenderSystem;

        public PipelinePluginContext(RenderContext renderContext, RenderSystem renderSystem)
        {
            RenderContext = renderContext;
            RenderSystem = renderSystem;
        }
    }
}
