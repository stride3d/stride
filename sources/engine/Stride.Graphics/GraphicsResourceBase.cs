// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Base class for a Graphics Resource.
/// </summary>
public partial class GraphicsResourceBase : ComponentBase
{
    /// <summary>
    ///   Lifetime state of the Graphics Resource.
    /// </summary>
    internal GraphicsResourceLifetimeState LifetimeState;

    /// <summary>
    ///   Gets the <see cref="Graphics.GraphicsDevice"/> the Graphics Resource depends on.
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; private set; }

    /// <summary>
    ///   Raised when the internal Graphics Resource gets destroyed.
    /// </summary>
    /// <remarks>
    ///   This event is useful when user-allocated handles associated with the internal resource need to be released.
    /// </remarks>
    public event EventHandler<EventArgs> Destroyed;


    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
    /// </summary>
    protected GraphicsResourceBase() : this(device: null, name: null) { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
    /// </summary>
    /// <param name="device">The graphics device.</param>
    protected GraphicsResourceBase(GraphicsDevice device) : this(device, name: null) { }

    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
    /// </summary>
    /// <param name="device">The graphics device.</param>
    /// <param name="name">
    ///   A name that can be used to identify the Graphics Resource.
    ///   Specify <see langword="null"/> to use the type's name instead.
    /// </param>
    protected GraphicsResourceBase(GraphicsDevice device, string? name) : base(name)
    {
        AttachToGraphicsDevice(device);
    }


    /// <summary>
    ///   Registers this Graphics Resource with the specified <see cref="Graphics.GraphicsDevice"/>.
    /// </summary>
    /// <param name="device">The graphics device.</param>
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

        Initialize();
    }

    /// <summary>
    ///   Perform platform-specific initialization of the Graphics Resource.
    /// </summary>
    private partial void Initialize();

    /// <summary>
    ///   Disposes the resources associated with the Graphics Resource, removes itself from
    ///   the <see cref="GraphicsDevice"/>'s resource registry, and transitions to the
    ///   <see cref="GraphicsResourceLifetimeState.Destroyed"/> state.
    /// </summary>
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

        base.Destroy();
    }

    /// <summary>
    ///   Called when the <see cref="GraphicsDevice"/> has been detected to be internally destroyed,
    ///   or when the <see cref="Destroy"/> methad has been called. Raises the <see cref="Destroyed"/> event.
    /// </summary>
    /// <param name="immediately">
    ///   A value indicating whether the resource should be destroyed immediately (<see langword="true"/>),
    ///   or if it can be deferred until it's safe to do so (<see langword="false"/>).
    /// </param>
    protected internal virtual partial void OnDestroyed(bool immediately = false);
}
