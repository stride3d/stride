// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using IServiceProviderIsService = Microsoft.Extensions.DependencyInjection.IServiceProviderIsService;

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// A service provider class for view model objects: the services registered as instances or with a factory, and those of
/// its parent, looked up when they are asked for.
/// </summary>
/// <remarks>
/// <para>
/// A lookup of a type gets, in this order: the service registered with a factory under exactly that type; the one instance
/// assignable to it (two make the lookup throw); what the parent gives. A child provider made before its parent gets a new
/// service sees that service, and a service of the child hides one of the parent.
/// </para>
/// <para>
/// Disposing the provider removes its registrations, disposes the instances its factories made, and leaves the instances
/// registered as such to their owners.
/// </para>
/// </remarks>
public sealed class ViewModelServiceProvider : IViewModelServiceProvider, IServiceProviderIsService, IDisposable
{
    private readonly IViewModelServiceProvider? parentProvider;
    private readonly List<object> services = [];
    private readonly Dictionary<Type, FactoryRegistration> factories = [];
    // The instances made for this provider by per-scope registrations, of this provider or of a parent
    private readonly Dictionary<FactoryRegistration, object> scopedInstances = [];
    // The instances made by factories for this provider, disposed with it, newest last
    private readonly List<object> madeInstances = [];
    private readonly Lock syncRoot = new();
    private EventHandler<ServiceRegistrationEventArgs>? serviceRegistered;
    private EventHandler<ServiceRegistrationEventArgs>? serviceUnregistered;
    private bool listensToParent;
    private bool disposed;

    /// <summary>
    /// An empty service provider.
    /// </summary>
    public static readonly IViewModelServiceProvider NullServiceProvider = new NullServiceProvider();

    public ViewModelServiceProvider(IEnumerable<object>? services = null)
        : this(null, services)
    {
    }

    /// <summary>
    /// Initializes a new provider whose lookups fall back to <paramref name="parentProvider"/> at the time they are made.
    /// </summary>
    /// <param name="parentProvider">The parent provider, or null.</param>
    /// <param name="services">The instances to register in the new provider, or null.</param>
    public ViewModelServiceProvider(IViewModelServiceProvider? parentProvider, IEnumerable<object>? services = null)
    {
        this.parentProvider = parentProvider;

        if (services != null)
        {
            foreach (var service in services)
            {
                RegisterService(service);
            }
        }
    }

    /// <summary>
    /// The provider this one falls back to, or null.
    /// </summary>
    public IViewModelServiceProvider? Parent => parentProvider;

    /// <inheritdoc/>
    /// <remarks>Raised for a registration in this provider or, while a handler is attached, in one of its parents.</remarks>
    public event EventHandler<ServiceRegistrationEventArgs>? ServiceRegistered
    {
        add { lock (syncRoot) { serviceRegistered += value; UpdateParentListening(); } }
        remove { lock (syncRoot) { serviceRegistered -= value; UpdateParentListening(); } }
    }

    /// <inheritdoc/>
    /// <remarks>Raised for a registration removed from this provider or, while a handler is attached, from one of its parents.</remarks>
    public event EventHandler<ServiceRegistrationEventArgs>? ServiceUnregistered
    {
        add { lock (syncRoot) { serviceUnregistered += value; UpdateParentListening(); } }
        remove { lock (syncRoot) { serviceUnregistered -= value; UpdateParentListening(); } }
    }

    /// <inheritdoc/>
    public void RegisterService(object service)
    {
        ArgumentNullException.ThrowIfNull(service);

        lock (syncRoot)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (services.Any(x => x.GetType() == service.GetType()))
                throw new InvalidOperationException("A service of the same type has already been registered.");

            services.Add(service);
        }
        serviceRegistered?.Invoke(this, new ServiceRegistrationEventArgs(service));
    }

    /// <inheritdoc/>
    /// <remarks>Does nothing once the provider is disposed: its registrations are gone already.</remarks>
    public void UnregisterService(object service)
    {
        ArgumentNullException.ThrowIfNull(service);

        bool removed;
        lock (syncRoot)
        {
            removed = services.Remove(service);
        }
        if (removed)
        {
            serviceUnregistered?.Invoke(this, new ServiceRegistrationEventArgs(service));
        }
    }

    /// <inheritdoc/>
    public IDisposable Register<TService>(Func<IServiceProvider, TService> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        var registration = new FactoryRegistration(typeof(TService), factory, lifetime);
        lock (syncRoot)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!factories.TryAdd(typeof(TService), registration))
                throw new InvalidOperationException($"A factory is already registered for {typeof(TService)}.");
        }
        serviceRegistered?.Invoke(this, new ServiceRegistrationEventArgs(typeof(TService), null, () => TryGet(typeof(TService))));
        return new Registration(() => Unregister(registration));
    }

    /// <inheritdoc/>
    public object? TryGet(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return Find(serviceType, this);
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
        return TryGet<T>() ?? throw new InvalidOperationException("No service matches the given type.");
    }

    /// <summary>
    /// Gets a service of the given type, as <see cref="TryGet(Type)"/> does. Asked for <see cref="IServiceProvider"/>,
    /// <see cref="IViewModelServiceProvider"/> or the check of services that constructor injection uses, it gives this provider.
    /// </summary>
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return IsProviderType(serviceType) ? this : TryGet(serviceType);
    }

    /// <summary>
    /// Whether a lookup of <paramref name="serviceType"/> would find a service, without making it. Constructor injection uses
    /// it to pick the constructor it can call.
    /// </summary>
    public bool IsService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return IsProviderType(serviceType) || Has(serviceType);
    }

    /// <summary>
    /// Removes the registrations of this provider and disposes the instances its factories made, newest first. The instances
    /// registered as such are not disposed: they belong to whoever registered them. Lookups then go to the parent only.
    /// </summary>
    public void Dispose()
    {
        List<object> removedServices;
        List<FactoryRegistration> removedFactories;
        List<object> toDispose;
        lock (syncRoot)
        {
            if (disposed)
                return;
            disposed = true;
            removedServices = [.. services];
            removedFactories = [.. factories.Values];
            toDispose = [.. madeInstances];
            services.Clear();
            factories.Clear();
            scopedInstances.Clear();
            madeInstances.Clear();
        }

        foreach (var service in removedServices)
            serviceUnregistered?.Invoke(this, new ServiceRegistrationEventArgs(service));
        foreach (var registration in removedFactories)
            serviceUnregistered?.Invoke(this, new ServiceRegistrationEventArgs(registration.ServiceType, registration.SingletonInstance, null));

        lock (syncRoot)
        {
            serviceRegistered = null;
            serviceUnregistered = null;
            UpdateParentListening();
        }

        for (var i = toDispose.Count - 1; i >= 0; --i)
            (toDispose[i] as IDisposable)?.Dispose();
    }

    private static bool IsProviderType(Type serviceType)
    {
        return serviceType == typeof(IServiceProvider) || serviceType == typeof(IViewModelServiceProvider) || serviceType == typeof(IServiceProviderIsService);
    }

    // The lookup of serviceType for the provider that asked, which gets the instances of per-scope registrations
    private object? Find(Type serviceType, ViewModelServiceProvider requester)
    {
        FactoryRegistration? registration;
        object? serviceFound = null;
        lock (syncRoot)
        {
            if (!disposed && factories.TryGetValue(serviceType, out registration))
            {
                // Resolved out of the loop below: a factory can ask for other services
            }
            else
            {
                registration = null;
                foreach (var service in services)
                {
                    if (!serviceType.IsInstanceOfType(service))
                        continue;
                    if (serviceFound != null)
                        throw new InvalidOperationException("Multiple services match the given type.");
                    serviceFound = service;
                }
            }
        }

        if (registration is not null)
            return registration.Lifetime == ServiceLifetime.Singleton ? GetSingleton(registration) : requester.GetScoped(registration);
        if (serviceFound is not null)
            return serviceFound;

        return parentProvider switch
        {
            ViewModelServiceProvider parent => parent.Find(serviceType, requester),
            null => null,
            _ => parentProvider.TryGet(serviceType),
        };
    }

    private bool Has(Type serviceType)
    {
        lock (syncRoot)
        {
            if (!disposed && (factories.ContainsKey(serviceType) || services.Any(serviceType.IsInstanceOfType)))
                return true;
        }
        return parentProvider switch
        {
            ViewModelServiceProvider parent => parent.Has(serviceType),
            IServiceProviderIsService parent => parent.IsService(serviceType),
            null => false,
            _ => parentProvider.TryGet(serviceType) is not null,
        };
    }

    private object GetSingleton(FactoryRegistration registration)
    {
        lock (registration)
        {
            if (registration.SingletonInstance is { } instance)
                return instance;
            instance = registration.Create(this);
            bool orphan;
            lock (syncRoot)
            {
                // The registration went while the instance was made: it is disposed with this provider, or now if it is gone
                orphan = disposed;
                if (!orphan)
                    madeInstances.Add(instance);
                if (factories.TryGetValue(registration.ServiceType, out var current) && current == registration)
                    registration.SingletonInstance = instance;
            }
            if (orphan)
                (instance as IDisposable)?.Dispose();
            return instance;
        }
    }

    // A scope that is disposed has no instances of its own anymore: a lookup from it finds nothing
    private object? GetScoped(FactoryRegistration registration)
    {
        lock (registration)
        {
            lock (syncRoot)
            {
                if (disposed)
                    return null;
                if (scopedInstances.TryGetValue(registration, out var existing))
                    return existing;
            }
            var instance = registration.Create(this);
            bool orphan;
            lock (syncRoot)
            {
                orphan = disposed;
                if (!orphan)
                {
                    madeInstances.Add(instance);
                    scopedInstances[registration] = instance;
                }
            }
            if (!orphan)
                return instance;
            (instance as IDisposable)?.Dispose();
            return null;
        }
    }

    private void Unregister(FactoryRegistration registration)
    {
        object? instance;
        object? scoped;
        lock (syncRoot)
        {
            if (!factories.TryGetValue(registration.ServiceType, out var current) || current != registration)
                return;
            factories.Remove(registration.ServiceType);
            instance = registration.SingletonInstance;
            registration.SingletonInstance = null;
            if (instance is not null)
                madeInstances.Remove(instance);
            if (scopedInstances.Remove(registration, out scoped))
                madeInstances.Remove(scoped);
        }
        serviceUnregistered?.Invoke(this, new ServiceRegistrationEventArgs(registration.ServiceType, instance ?? scoped, null));
        // The instances that child providers made for themselves go with those providers
        (instance as IDisposable)?.Dispose();
        (scoped as IDisposable)?.Dispose();
    }

    // A child forwards the events of its parent while it has handlers of its own: a child nobody listens to is not kept alive
    // by its parent
    private void UpdateParentListening()
    {
        var listen = !disposed && (serviceRegistered is not null || serviceUnregistered is not null) && parentProvider is not null;
        if (listen == listensToParent)
            return;
        listensToParent = listen;
        if (listen)
        {
            parentProvider!.ServiceRegistered += ParentServiceRegistered;
            parentProvider.ServiceUnregistered += ParentServiceUnregistered;
        }
        else
        {
            parentProvider!.ServiceRegistered -= ParentServiceRegistered;
            parentProvider.ServiceUnregistered -= ParentServiceUnregistered;
        }
    }

    private void ParentServiceRegistered(object? sender, ServiceRegistrationEventArgs e)
    {
        serviceRegistered?.Invoke(sender, e);
    }

    private void ParentServiceUnregistered(object? sender, ServiceRegistrationEventArgs e)
    {
        serviceUnregistered?.Invoke(sender, e);
    }

    private sealed class FactoryRegistration(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
    {
        [ThreadStatic]
        private static HashSet<FactoryRegistration>? creating;

        public Type ServiceType { get; } = serviceType;

        public ServiceLifetime Lifetime { get; } = lifetime;

        public object? SingletonInstance { get; set; }

        public object Create(ViewModelServiceProvider provider)
        {
            creating ??= [];
            if (!creating.Add(this))
                throw new InvalidOperationException($"The factory of {ServiceType} asks for {ServiceType} itself.");
            try
            {
                return factory(provider) ?? throw new InvalidOperationException($"The factory of {ServiceType} returned null.");
            }
            finally
            {
                creating.Remove(this);
            }
        }
    }

    private sealed class Registration(Action remove) : IDisposable
    {
        private Action? remove = remove;

        public void Dispose() => Interlocked.Exchange(ref remove, null)?.Invoke();
    }
}
