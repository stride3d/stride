// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core;

namespace Stride.Graphics
{
    public class ResumeManager
    {
        private GraphicsDevice graphicsDevice;
        private bool deviceHasBeenDestroyed = false;
        private bool deviceHasBeenPaused = false;
        private IServiceRegistry services;

        public ResumeManager(IServiceRegistry services)
        {
            this.services = services;
            graphicsDevice = services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
        }

        public void OnRender()
        {
            if (deviceHasBeenDestroyed)
            {
                // Destroy resources
                OnDestroyed();

                // Reload items (data from HDD: texture, buffers, etc...)
                OnReload();

                // Recreate other items
                OnRecreate();
            }
            else if (deviceHasBeenPaused)
            {
                // Recreate items that were freed voluntarily during pause (render target, dynamic resources, etc...)
                OnResume();
            }
        }

        public void Pause()
        {
            lock (graphicsDevice.Resources)
            {
                foreach (var resource in graphicsDevice.Resources)
                {
                    if (resource.OnPause())
                        resource.LifetimeState = GraphicsResourceLifetimeState.Paused;
                }
            }
        }

        public void OnResume()
        {
            lock (graphicsDevice.Resources)
            {
                foreach (var resource in graphicsDevice.Resources)
                {
                    if (resource.LifetimeState == GraphicsResourceLifetimeState.Paused)
                    {
                        resource.OnResume();
                        resource.LifetimeState = GraphicsResourceLifetimeState.Active;
                    }
                }
            }
        }

        public void OnRecreate()
        {
            // Recreate presenter
            graphicsDevice.Presenter?.OnRecreated();

            bool wasSomethingRecreated = true;
            bool hasDestroyedObjects = true;

            lock (graphicsDevice.Resources)
            {
                // Only continue if we made some progress, otherwise that means we reached something that could not be solved
                // This allows for dependencies to still be handled without some complex system
                // (we don't really care of recreation performance/complexity).
                while (wasSomethingRecreated && hasDestroyedObjects)
                {
                    // Let's track if something happened during this loop
                    wasSomethingRecreated = false;
                    hasDestroyedObjects = false;

                    foreach (var resource in graphicsDevice.Resources)
                    {
                        if (resource.LifetimeState == GraphicsResourceLifetimeState.Destroyed)
                        {
                            if (resource.OnRecreate())
                            {
                                wasSomethingRecreated = true;
                                resource.LifetimeState = GraphicsResourceLifetimeState.Active;
                            }
                            else
                            {
                                // Couldn't be recreated?
                                hasDestroyedObjects = true;
                            }
                        }
                    }
                }

                if (hasDestroyedObjects)
                {
                    // Attach the list of objects that could not be recreated to the exception.
                    var destroyedObjects = graphicsDevice.Resources.Where(x => x.LifetimeState == GraphicsResourceLifetimeState.Destroyed).ToList();
                    throw new InvalidOperationException("Could not recreate all objects.") { Data = { { "DestroyedObjects", destroyedObjects } } };
                }
            }
        }

        /// <summary>
        ///   Called when the Resume Manager is being destroyed.
        /// </summary>
        /// <param name="immediately">
        ///   A value indicating whether the resources used by the Resume Manager should be destroyed immediately
        ///   (<see langword="true"/>), or if it can be deferred until it's safe to do so (<see langword="false"/>).
        /// </param>
        public void OnDestroyed(bool immediately = false)
        {
            lock (graphicsDevice.Resources)
            {
                foreach (var resource in graphicsDevice.Resources)
                {
                    resource.OnDestroyed(immediately);
                    resource.LifetimeState = GraphicsResourceLifetimeState.Destroyed;
                }
            }

            // Clear various graphics device internal states (input layouts, FBOs, etc...)
            graphicsDevice.OnDestroyed(immediately);
        }

        public void OnReload()
        {
            lock (graphicsDevice.Resources)
            {
                foreach (var resource in graphicsDevice.Resources)
                {
                    if (resource.LifetimeState == GraphicsResourceLifetimeState.Destroyed)
                    {
                        if (resource.Reload != null)
                        {
                            resource.Reload(resource, services);
                            resource.LifetimeState = GraphicsResourceLifetimeState.Active;
                        }
                    }
                }
            }
        }
    }
}
