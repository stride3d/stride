// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Graphics;

public partial class GraphicsResourceBase : ComponentBase
{
    internal GraphicsResourceLifetimeState LifetimeState;

    public Action<GraphicsResourceBase, IServiceRegistry> Reload;

    public GraphicsDevice GraphicsDevice { get; private set; }

    public event EventHandler<EventArgs> Destroyed;


        /// <summary>
        /// Gets the graphics device attached to this instance.
        /// </summary>
        /// <value>The graphics device.</value>
        /// <summary>
        /// Raised when the internal graphics resource gets destroyed. 
        /// This event is useful when user allocated handles associated with the internal resource need to be released.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="name">The name.</param>

        /// <summary>
        /// Called when graphics device is inactive (put in the background and rendering is paused).
        /// It should voluntarily release objects that can be easily recreated, such as FBO and dynamic buffers.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Paused"/> state.</returns>

        /// <summary>
        /// Called when graphics device is resumed from either paused or destroyed state.
        /// If possible, resource should be recreated.
        /// </summary>
    protected GraphicsResourceBase() : this(device: null, name: null) { }

    protected GraphicsResourceBase(GraphicsDevice device) : this(device, name: null) { }

    protected GraphicsResourceBase(GraphicsDevice device, string name) : base(name)
    {
        AttachToGraphicsDevice(device);
    }


    internal void AttachToGraphicsDevice(GraphicsDevice device)
    {
        GraphicsDevice = device;

        if (device is not null)
        {
            // Add this Graphics Resource to the device's resources
            var resources = device.Resources;
            lock (resources)
            {
                resources.Add(this);
            }
        }

        /// <inheritdoc/>
        Initialize();
    }

    private partial void Initialize();

    protected internal virtual bool OnPause()
    {
        return false;
    }

    protected internal virtual void OnResume()
    {
    }

    protected override void Destroy()
    {
        var device = GraphicsDevice;

        if (device is not null)
        {
            // Remove this Graphics Resource from the GraphicsDevice's resources
            var resources = device.Resources;
            lock (resources)
            {
                resources.Remove(this);
            }
            if (LifetimeState != GraphicsResourceLifetimeState.Destroyed)
            {
                OnDestroyed();
                LifetimeState = GraphicsResourceLifetimeState.Destroyed;
            }
        }

        // No need for reload anymore, allow the delegate to be GC
        Reload = null;

        base.Destroy();
    }

    protected internal virtual partial void OnDestroyed();
}
