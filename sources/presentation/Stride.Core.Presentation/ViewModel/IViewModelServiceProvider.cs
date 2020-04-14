// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ViewModel
{
    /// <summary>
    /// A service provider class for view model objects.
    /// </summary>
    public interface IViewModelServiceProvider
    {
        /// <summary>
        /// Raised when a service is registered to this service provider.
        /// </summary>
        event EventHandler<ServiceRegistrationEventArgs> ServiceRegistered;

        /// <summary>
        /// Raised when a service is unregistered from this service provider.
        /// </summary>
        event EventHandler<ServiceRegistrationEventArgs> ServiceUnregistered;

        /// <summary>
        /// Register a new service in this <see cref="ViewModelServiceProvider"/>.
        /// </summary>
        /// <param name="service">The service to register.</param>
        /// <exception cref="ArgumentNullException"><c>service</c> is null.</exception>
        /// <exception cref="InvalidOperationException">A service of the same type has already been registered.</exception>
        void RegisterService([NotNull] object service);

        /// <summary>
        /// Unregister a service from this <see cref="ViewModelServiceProvider"/>.
        /// </summary>
        /// <param name="service">The service to unregister.</param>
        /// <exception cref="ArgumentNullException"><c>service</c> is null.</exception>
        void UnregisterService([NotNull] object service);

        /// <summary>
        /// Gets a service of the given type, if available.
        /// </summary>
        /// <param name="serviceType">The type of service to retrieve.</param>
        /// <returns>An instance of the service that match the given type if available, <c>null</c> otherwise.</returns>
        /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
        [CanBeNull]
        object TryGet([NotNull] Type serviceType);

        /// <summary>
        /// Gets a service of the given type, if available.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>An instance of the service that match the given type if available, <c>null</c> otherwise.</returns>
        /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
        [CanBeNull]
        T TryGet<T>() where T : class;

        /// <summary>
        /// Gets a service of the given type.
        /// </summary>
        /// <param name="serviceType">The type of service to retrieve.</param>
        /// <returns>An instance of the service that match the given type.</returns>
        /// <exception cref="InvalidOperationException">No service matches the given type.</exception>
        /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
        [NotNull]
        object Get([NotNull] Type serviceType);

        /// <summary>
        /// Gets a service of the given type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>An instance of the service that match the given type.</returns>
        /// <exception cref="InvalidOperationException">No service matches the given type.</exception>
        /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
        [NotNull]
        T Get<T>() where T : class;
    }
}
