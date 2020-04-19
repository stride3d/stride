// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;

namespace Stride.Core
{
    public static class ServiceRegistryExtensions
    {
        /// <summary>
        /// Gets a service instance from a specified interface contract.
        /// </summary>
        /// <typeparam name="T">Type of the interface contract of the service</typeparam>
        /// <param name="registry">The registry.</param>
        /// <returns>An instance of the requested service registered to this registry.</returns>
        [CanBeNull]
        [Obsolete("Use the generic overload of IServiceRegistry.GetService instead")]
        public static T GetServiceAs<T>([NotNull] this IServiceRegistry registry)
            where T : class
        {
            return registry.GetService<T>();
        }

        /// <summary>
        /// Gets a service instance from a specified interface contract.
        /// </summary>
        /// <typeparam name="T">Type of the interface contract of the service</typeparam>
        /// <param name="registry">The registry.</param>
        /// <exception cref="ServiceNotFoundException">If the service was not found</exception>
        /// <returns>An instance of the requested service registered to this registry.</returns>
        [NotNull]
        public static T GetSafeServiceAs<T>([NotNull] this IServiceRegistry registry)
            where T : class
        {
            var service = registry.GetService<T>();
            if (service == null) throw new ServiceNotFoundException(typeof(T));
            return service;
        }

        /// <summary>
        /// Gets a service instance from a specified interface contract.
        /// </summary>
        /// <typeparam name="T">Type of the interface contract of the service</typeparam>
        /// <param name="registry">The registry.</param>
        /// <param name="serviceReady">The service ready.</param>
        /// <returns>An instance of the requested service registered to this registry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service was not found</exception>
        public static void GetServiceLate<T>([NotNull] this IServiceRegistry registry, [NotNull] Action<T> serviceReady)
            where T : class
        {
            var service = registry.GetService<T>();
            if (service == null)
            {
                var deferred = new ServiceDeferredRegister<T>(registry, serviceReady);
                deferred.Register();
            }
            else
            {
                serviceReady(service);
            }
        }

        private class ServiceDeferredRegister<T>
        {
            private readonly IServiceRegistry services;
            private readonly Action<T> serviceReady;

            public ServiceDeferredRegister([NotNull] IServiceRegistry registry, [NotNull] Action<T> serviceReady)
            {
                services = registry;
                this.serviceReady = serviceReady;
            }

            public void Register()
            {
                services.ServiceAdded += OnServiceAdded;
            }

            private void OnServiceAdded(object sender, [NotNull] ServiceEventArgs args)
            {
                if (args.ServiceType == typeof(T))
                {
                    serviceReady((T)args.Instance);
                    services.ServiceAdded -= OnServiceAdded;
                }
            }
        }
    }
}
