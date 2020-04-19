// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ViewModel
{
    /// <summary>
    /// A service provider class for view model objects.
    /// </summary>
    public class ViewModelServiceProvider : IViewModelServiceProvider
    {
        private readonly IViewModelServiceProvider parentProvider;
        private readonly List<object> services = new List<object>();

        /// <summary>
        /// An empty service provider.
        /// </summary>
        public static IViewModelServiceProvider NullServiceProvider = new NullServiceProvider();

        public ViewModelServiceProvider(IEnumerable<object> services = null)
            : this(null, services)
        {
        }

        public ViewModelServiceProvider(IViewModelServiceProvider parentProvider, IEnumerable<object> services = null)
        {
            // If the parent provider is a ViewModelServiceProvider, try to merge its service list instead of using composition.
            var parent = parentProvider as ViewModelServiceProvider;
            if (parent != null)
            {
                parent.services.ForEach(RegisterService);
            }
            else
            {
                this.parentProvider = parentProvider;
            }
            if (services != null)
            {
                foreach (var service in services)
                {
                    RegisterService(service);
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<ServiceRegistrationEventArgs> ServiceRegistered;

        /// <inheritdoc/>
        public event EventHandler<ServiceRegistrationEventArgs> ServiceUnregistered;

        /// <inheritdoc/>
        public void RegisterService(object service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (services.Any(x => x.GetType() == service.GetType()))
                throw new InvalidOperationException("A service of the same type has already been registered.");

            services.Add(service);
            ServiceRegistered?.Invoke(this, new ServiceRegistrationEventArgs(service));
        }

        /// <inheritdoc/>
        public void UnregisterService(object service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            services.Remove(service);
            ServiceUnregistered?.Invoke(this, new ServiceRegistrationEventArgs(service));
        }

        /// <inheritdoc/>
        public object TryGet(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            object serviceFound = null;

            foreach (var service in services.Where(serviceType.IsInstanceOfType))
            {
                if (serviceFound != null)
                    throw new InvalidOperationException("Multiple services match the given type.");

                serviceFound = service;
            }

            return serviceFound ?? parentProvider?.TryGet(serviceType);
        }

        /// <inheritdoc/>
        public T TryGet<T>() where T : class
        {
            return (T)TryGet(typeof(T));
        }

        /// <inheritdoc/>
        public object Get(Type serviceType)
        {
            var result = TryGet(serviceType);
            if (result == null) throw new InvalidOperationException("No service matches the given type.");
            return result;
        }

        /// <inheritdoc/>
        public T Get<T>() where T : class
        {
            var result = (T)TryGet(typeof(T));
            if (result == null) throw new InvalidOperationException("No service matches the given type.");
            return result;
        }
    }
}
