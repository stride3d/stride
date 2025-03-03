// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

public static class ServiceRegistryExtensions
{
    /// <summary>
    /// Gets a service instance from a specified interface contract.
    /// </summary>
    /// <typeparam name="T">Type of the interface contract of the service</typeparam>
    /// <param name="registry">The registry.</param>
    /// <exception cref="ServiceNotFoundException">If the service was not found</exception>
    /// <returns>An instance of the requested service registered to this registry.</returns>
    public static T GetSafeServiceAs<T>(this IServiceRegistry registry)
        where T : class
    {
        return registry.GetService<T>() ?? throw new ServiceNotFoundException(typeof(T));
    }

    /// <summary>
    /// Gets a service instance from a specified interface contract.
    /// </summary>
    /// <typeparam name="T">Type of the interface contract of the service</typeparam>
    /// <param name="registry">The registry.</param>
    /// <param name="serviceReady">The service ready.</param>
    /// <returns>An instance of the requested service registered to this registry.</returns>
    /// <exception cref="ServiceNotFoundException">If the service was not found</exception>
    public static void GetServiceLate<T>(this IServiceRegistry registry, Action<T> serviceReady)
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

        public ServiceDeferredRegister(IServiceRegistry registry, Action<T> serviceReady)
        {
            services = registry;
            this.serviceReady = serviceReady;
        }

        public void Register()
        {
            services.ServiceAdded += OnServiceAdded;
        }

        private void OnServiceAdded(object? sender, ServiceEventArgs args)
        {
            if (args.ServiceType == typeof(T))
            {
                serviceReady((T)args.Instance);
                services.ServiceAdded -= OnServiceAdded;
            }
        }
    }
}
