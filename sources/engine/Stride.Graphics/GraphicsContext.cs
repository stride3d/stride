// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// A graphics command context. You should usually stick to one per rendering thread.
    /// </summary>
    public class GraphicsContext
    {
        /// <summary>
        /// Gets the current command list.
        /// </summary>
        public CommandList CommandList { get; set; }

        /// <summary>
        /// Gets the current resource group allocator.
        /// </summary>
        public ResourceGroupAllocator ResourceGroupAllocator { get; private set; }

        public GraphicsResourceAllocator Allocator { get; private set; }

        public GraphicsContext(GraphicsDevice graphicsDevice, GraphicsResourceAllocator allocator = null, CommandList commandList = null)
        {
            CommandList = commandList ?? graphicsDevice.InternalMainCommandList ?? CommandList.New(graphicsDevice).DisposeBy(graphicsDevice);
            Allocator = allocator ?? new GraphicsResourceAllocator(graphicsDevice).DisposeBy(graphicsDevice);
            ResourceGroupAllocator = new ResourceGroupAllocator(Allocator, CommandList).DisposeBy(graphicsDevice);
        }
    }
}
