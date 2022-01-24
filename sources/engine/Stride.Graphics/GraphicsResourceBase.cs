// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Graphics
{
    public partial class GraphicsResourceBase : ComponentBase
    {
        internal GraphicsResourceLifetimeState LifetimeState;
        public Action<GraphicsResourceBase> Reload;

        private WeakReference<GraphicsResourceBase> key;

        /// <summary>
        /// Gets the graphics device attached to this instance.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice
        {
            get;
            private set;
        }

        /// <summary>
        /// Raised when the internal graphics resource gets destroyed. 
        /// This event is useful when user allocated handles associated with the internal resource need to be released.
        /// </summary>
        public event EventHandler<EventArgs> Destroyed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
        /// </summary>
        protected GraphicsResourceBase()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        protected GraphicsResourceBase(GraphicsDevice device) : this(device, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceBase"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="name">The name.</param>
        protected GraphicsResourceBase(GraphicsDevice device, string name) : base(name)
        {
            AttachToGraphicsDevice(device);
        }

        ~GraphicsResourceBase()
        {
            Destroy();
        }

        internal void AttachToGraphicsDevice(GraphicsDevice device)
        {
            GraphicsDevice = device;

            if (device != null)
            {
                // Add GraphicsResourceBase to device resources
                var resources = device.Resources;
                key = new WeakReference<GraphicsResourceBase>(this);
                lock (resources)
                {
                    resources.Add(key);
                }
            }

            Initialize();
        }

        /// <summary>
        /// Called when graphics device is inactive (put in the background and rendering is paused).
        /// It should voluntarily release objects that can be easily recreated, such as FBO and dynamic buffers.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Paused"/> state.</returns>
        protected internal virtual bool OnPause()
        {
            return false;
        }

        /// <summary>
        /// Called when graphics device is resumed from either paused or destroyed state.
        /// If possible, resource should be recreated.
        /// </summary>
        protected internal virtual void OnResume()
        {
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            GC.SuppressFinalize(this);
            var device = GraphicsDevice;

            if (device != null)
            {
                // Remove GraphicsResourceBase from device resources
                var resources = device.Resources;
                lock (resources)
                {
                    resources.Remove(key);
                }
                if (LifetimeState != GraphicsResourceLifetimeState.Destroyed)
                {
                    OnDestroyed();
                    LifetimeState = GraphicsResourceLifetimeState.Destroyed;
                }
            }

            // No need for reload anymore, allow it to be GC
            Reload = null;

            base.Destroy();
        }
    }
}
