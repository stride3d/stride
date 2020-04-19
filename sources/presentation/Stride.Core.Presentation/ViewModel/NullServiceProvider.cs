// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Presentation.ViewModel
{
    /// <summary>
    /// A service provider that is empty and immutable.
    /// </summary>
    internal class NullServiceProvider : IViewModelServiceProvider
    {
        // We provide an empty `add' and `remove' to avoid a warning about unused events that we have
        // to implement as they are part of the IViewModelServiceProvider definition.
        /// <inheritdoc/>
        public event EventHandler<ServiceRegistrationEventArgs> ServiceRegistered { add { } remove { } }

        // We provide an empty `add' and `remove' to avoid a warning about unused events that we have
        // to implement as they are part of the IViewModelServiceProvider definition.
        /// <inheritdoc/>
        public event EventHandler<ServiceRegistrationEventArgs> ServiceUnregistered { add { } remove { } }

        /// <inheritdoc/>
        public void RegisterService(object service)
        {
            throw new InvalidOperationException("Cannot register a service on a NullServiceProvider.");
        }

        /// <inheritdoc/>
        public void UnregisterService(object service)
        {
            throw new InvalidOperationException("Cannot unregister a service on a NullServiceProvider.");
        }

        /// <inheritdoc/>
        public object TryGet(Type serviceType)
        {
            return null;
        }

        /// <inheritdoc/>
        public T TryGet<T>() where T : class
        {
            return null;
        }

        /// <inheritdoc/>
        public object Get(Type serviceType)
        {
            throw new InvalidOperationException("No service matches the given type.");
        }

        /// <inheritdoc/>
        public T Get<T>() where T : class
        {
            throw new InvalidOperationException("No service matches the given type.");
        }
    }
}
