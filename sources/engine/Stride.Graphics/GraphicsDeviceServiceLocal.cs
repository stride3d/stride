// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    ///   A default simple implementation of <see cref="IGraphicsDeviceService"/> that is used by
    ///   some systems that only need quick access to the <see cref="GraphicsDevice"/>.
    /// </summary>
    /// <remarks>
    ///   For a full-fledged implementation of <see cref="IGraphicsDeviceService"/> that manages
    ///   correctly the device life-cycle and provides many more features, see <c>GraphicsDeviceManager</c>
    ///   in the <c>Stride.Games</c> namespace.
    /// </remarks>
    public class GraphicsDeviceServiceLocal : IGraphicsDeviceService
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsDeviceServiceLocal"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        public GraphicsDeviceServiceLocal(GraphicsDevice graphicsDevice)
            : this(registry: null, graphicsDevice)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="GraphicsDeviceServiceLocal"/> class.
        /// </summary>
        /// <param name="registry">The registry of registered services.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        public GraphicsDeviceServiceLocal(IServiceRegistry registry, GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        // We provide an empty `add' and `remove' to avoid a warning about unused events that we have
        // to implement as they are part of the IGraphicsDeviceService definition.

        /// <inheritdoc/>
        public event EventHandler<EventArgs> DeviceCreated { add { } remove { } }
        /// <inheritdoc/>
        public event EventHandler<EventArgs> DeviceDisposing { add { } remove { } }
        /// <inheritdoc/>
        public event EventHandler<EventArgs> DeviceReset { add { } remove { } }
        /// <inheritdoc/>
        public event EventHandler<EventArgs> DeviceResetting { add { } remove { } }

        /// <inheritdoc/>
        public GraphicsDevice GraphicsDevice { get; private set; }
    }
}
