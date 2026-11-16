// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// A service provider class for view model objects.
/// </summary>
/// <remarks>
/// A provider can have a parent: a service it doesn't have is looked up in its parent at the time it is asked for. The editor
/// has one for the whole process, a child of it for the session, and a child of that one for each asset editor.
/// </remarks>
public interface IViewModelServiceProvider : IServiceProvider
{
    /// <summary>
    /// Raised when a service is registered to this service provider.
    /// </summary>
    event EventHandler<ServiceRegistrationEventArgs>? ServiceRegistered;

    /// <summary>
    /// Raised when a service is unregistered from this service provider.
    /// </summary>
    event EventHandler<ServiceRegistrationEventArgs>? ServiceUnregistered;

    /// <summary>
    /// Register a new service in this <see cref="ViewModelServiceProvider"/>.
    /// </summary>
    /// <param name="service">The service to register.</param>
    /// <exception cref="ArgumentNullException"><c>service</c> is null.</exception>
    /// <exception cref="InvalidOperationException">A service of the same type has already been registered.</exception>
    void RegisterService(object service);

    /// <summary>
    /// Unregister a service from this <see cref="ViewModelServiceProvider"/>.
    /// </summary>
    /// <param name="service">The service to unregister.</param>
    /// <exception cref="ArgumentNullException"><c>service</c> is null.</exception>
    void UnregisterService(object service);

    /// <summary>
    /// Registers a service made by a factory, under an explicit service type. A lookup of exactly <typeparamref name="TService"/>
    /// gets it, even when several registered instances are also assignable to that type.
    /// </summary>
    /// <typeparam name="TService">The type the service is registered as, and looked up by.</typeparam>
    /// <param name="factory">Makes the service, with the provider that it is made for.</param>
    /// <param name="lifetime">How long an instance lives: one for this provider, or one per scope that asks for it.</param>
    /// <returns>An object that removes the registration when disposed, and disposes the instances it made.</returns>
    /// <exception cref="ArgumentNullException"><c>factory</c> is null.</exception>
    /// <exception cref="InvalidOperationException">A factory is already registered for <typeparamref name="TService"/> in this provider.</exception>
    /// <exception cref="NotSupportedException">This provider has no factory registrations.</exception>
    [Experimental("STREXP006")]
    IDisposable Register<TService>(Func<IServiceProvider, TService> factory, ViewModelServiceLifetime lifetime = ViewModelServiceLifetime.Singleton)
        where TService : class
    {
        throw new NotSupportedException("This service provider does not support factory registrations.");
    }

    /// <summary>
    /// Gets a service of the given type, if available.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve.</param>
    /// <returns>An instance of the service that match the given type if available, <c>null</c> otherwise.</returns>
    /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
    object? TryGet(Type serviceType);

    /// <summary>
    /// Gets a service of the given type, if available.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>An instance of the service that match the given type if available, <c>null</c> otherwise.</returns>
    /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
    T? TryGet<T>() where T : class;

    /// <summary>
    /// Gets a service of the given type.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve.</param>
    /// <returns>An instance of the service that match the given type.</returns>
    /// <exception cref="InvalidOperationException">No service matches the given type.</exception>
    /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
    object Get(Type serviceType);

    /// <summary>
    /// Gets a service of the given type.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>An instance of the service that match the given type.</returns>
    /// <exception cref="InvalidOperationException">No service matches the given type.</exception>
    /// <exception cref="InvalidOperationException">Multiple services match the given type.</exception>
    T Get<T>() where T : class;

    /// <summary>
    /// Gets a service of the given type, as <see cref="TryGet(Type)"/> does. Asked for <see cref="IServiceProvider"/> or
    /// <see cref="IViewModelServiceProvider"/>, it gives this provider.
    /// </summary>
    object? IServiceProvider.GetService(Type serviceType)
    {
        return serviceType == typeof(IServiceProvider) || serviceType == typeof(IViewModelServiceProvider) ? this : TryGet(serviceType);
    }
}
