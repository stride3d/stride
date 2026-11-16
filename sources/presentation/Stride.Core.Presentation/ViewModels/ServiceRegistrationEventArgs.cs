// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// Arguments of the events raised by <see cref="IViewModelServiceProvider"/> implementations.
/// </summary>
public sealed class ServiceRegistrationEventArgs : EventArgs
{
    private readonly Func<object?>? resolve;
    private object? service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationEventArgs"/> class.
    /// </summary>
    /// <param name="service">The service related to this event.</param>
    internal ServiceRegistrationEventArgs(object service)
    {
        this.service = service;
        ServiceType = service.GetType();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationEventArgs"/> class for a service registered with a factory.
    /// </summary>
    /// <param name="serviceType">The type the service is registered as.</param>
    /// <param name="service">The instance, or null when it has not been made (yet).</param>
    /// <param name="resolve">Makes or gets the instance when <see cref="Service"/> is read, or null.</param>
    internal ServiceRegistrationEventArgs(Type serviceType, object? service, Func<object?>? resolve)
    {
        ServiceType = serviceType;
        this.service = service;
        this.resolve = resolve;
    }

    /// <summary>
    /// Gets the type of the service related to this event: the type of the instance for a service registered as an instance,
    /// the type it is registered as for a service registered with a factory.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the service related to this event.
    /// </summary>
    /// <remarks>
    /// For a service registered with a factory, reading it when the service is registered makes the instance, and it is null
    /// when the service is removed before an instance was made. Check <see cref="ServiceType"/> first.
    /// </remarks>
    public object? Service => service ??= resolve?.Invoke();
}
