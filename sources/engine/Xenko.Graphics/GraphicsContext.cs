// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// A graphics command context. You should usually stick to one per rendering thread.
    /// </summary>
    public class GraphicsContext
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly Dictionary<object, IDisposable> sharedData = new Dictionary<object, IDisposable>();

        public delegate T CreateSharedData<out T>(GraphicsContext device) where T : class, IDisposable;

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
            this.graphicsDevice = graphicsDevice;
            CommandList = commandList ?? (graphicsDevice.IsDeferred ? CommandList.New(graphicsDevice): graphicsDevice.DefaultCommandList);
            Allocator = allocator ?? new GraphicsResourceAllocator(graphicsDevice).DisposeBy(graphicsDevice);
            ResourceGroupAllocator = new ResourceGroupAllocator(Allocator, CommandList).DisposeBy(graphicsDevice);
        }

        public T GetOrCreateSharedData<T>(object key, CreateSharedData<T> sharedDataCreator) where T : class, IDisposable
        {
            lock (sharedData)
            {
                IDisposable localValue;
                if (!sharedData.TryGetValue(key, out localValue))
                {
                    localValue = sharedDataCreator(this);
                    if (localValue == null)
                    {
                        return null;
                    }

                    localValue.DisposeBy(graphicsDevice);
                    sharedData.Add(key, localValue);
                }
                return (T)localValue;
            }
        }
    }
}
