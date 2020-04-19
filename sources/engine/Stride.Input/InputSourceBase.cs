// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Collections;

namespace Stride.Input
{
    /// <summary>
    /// Base class for input sources, implements common parts of the <see cref="IInputSource"/> interface and keeps track of registered devices through <see cref="RegisterDevice"/> and <see cref="UnregisterDevice"/>
    /// </summary>
    public abstract class InputSourceBase : IInputSource
    {       
        public TrackingDictionary<Guid, IInputDevice> Devices { get; } = new TrackingDictionary<Guid, IInputDevice>();

        public abstract void Initialize(InputManager inputManager);

        public virtual void Update()
        {
        }
        
        public virtual void Pause()
        {
        }
        
        public virtual void Resume()
        {
        }
        
        public virtual void Scan()
        {
        }

        /// <summary>
        /// Unregisters all devices registered with <see cref="RegisterDevice"/> which have not been unregistered yet
        /// </summary>
        public virtual void Dispose()
        {
            // Remove all devices, done by clearing the tracking dictionary
            Devices.Clear();
        }

        /// <summary>
        /// Adds the device to the list <see cref="Devices"/>
        /// </summary>
        /// <param name="device">The device</param>
        protected void RegisterDevice(IInputDevice device)
        {
            if (Devices.ContainsKey(device.Id))
                throw new InvalidOperationException($"Input device with Id {device.Id} already registered");

            Devices.Add(device.Id, device);
        }

        /// <summary>
        /// CRemoves the device from the list <see cref="Devices"/>
        /// </summary>
        /// <param name="device">The device</param>
        protected void UnregisterDevice(IInputDevice device)
        {
            if (!Devices.ContainsKey(device.Id))
                throw new InvalidOperationException($"Input device with Id {device.Id} was not registered");

            Devices.Remove(device.Id);
        }
    }
}