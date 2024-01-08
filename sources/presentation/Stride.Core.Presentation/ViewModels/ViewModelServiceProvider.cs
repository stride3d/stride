// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// A service provider class for view model objects.
/// </summary>
public sealed class ViewModelServiceProvider : IViewModelServiceProvider
{
    private readonly IViewModelServiceProvider? parentProvider;
    private readonly List<object> services = [];

    /// <summary>
    /// An empty service provider.
    /// </summary>
    public static readonly IViewModelServiceProvider NullServiceProvider = new NullServiceProvider();

    public ViewModelServiceProvider(IEnumerable<object>? services = null)
        : this(null, services)
    {
    }

    public ViewModelServiceProvider(IViewModelServiceProvider? parentProvider, IEnumerable<object>? services = null)
    {
        // If the parent provider is a ViewModelServiceProvider, try to merge its service list instead of using composition.
        if (parentProvider is ViewModelServiceProvider parent)
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
    public event EventHandler<ServiceRegistrationEventArgs>? ServiceRegistered;

    /// <inheritdoc/>
    public event EventHandler<ServiceRegistrationEventArgs>? ServiceUnregistered;

    /// <inheritdoc/>
    public void RegisterService(object service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (services.Any(x => x.GetType() == service.GetType()))
            throw new InvalidOperationException("A service of the same type has already been registered.");

        services.Add(service);
        ServiceRegistered?.Invoke(this, new ServiceRegistrationEventArgs(service));
    }

    /// <inheritdoc/>
    public void UnregisterService(object service)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (services.Remove(service))
        {
            ServiceUnregistered?.Invoke(this, new ServiceRegistrationEventArgs(service));
        }
    }

    /// <inheritdoc/>
    public object? TryGet(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        object? serviceFound = null;

        foreach (var service in services.Where(serviceType.IsInstanceOfType))
        {
            if (serviceFound != null)
                throw new InvalidOperationException("Multiple services match the given type.");

            serviceFound = service;
        }

        return serviceFound ?? parentProvider?.TryGet(serviceType);
    }

    /// <inheritdoc/>
    public T? TryGet<T>() where T : class
    {
        return TryGet(typeof(T)) as T;
    }

    /// <inheritdoc/>
    public object Get(Type serviceType)
    {
        var result = TryGet(serviceType);
        return result ?? throw new InvalidOperationException("No service matches the given type.");
    }

    /// <inheritdoc/>
    public T Get<T>() where T : class
    {
        var result = TryGet(typeof(T)) as T;
        return result ?? throw new InvalidOperationException("No service matches the given type.");
    }
}
