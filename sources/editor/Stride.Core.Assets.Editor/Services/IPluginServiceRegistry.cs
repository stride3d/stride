// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The services a plugin adds to the session's scope (<see cref="ISessionPluginContext.Services"/>): instances, and services
/// made by a factory under an explicit service type. They go when the plugin leaves the session.
/// </summary>
[Experimental("STREXP006")]
public interface IPluginServiceRegistry : IPluginRegistry<object>
{
    /// <summary>
    /// Adds a service made by <paramref name="factory"/>, which a lookup of exactly <typeparamref name="TService"/> gets.
    /// </summary>
    /// <typeparam name="TService">The type the service is registered as, and looked up by.</typeparam>
    /// <param name="factory">Makes the service, with the services of the scope it is made for.</param>
    /// <param name="lifetime">One instance for the session, or one per scope that asks for it (the session, each editor).</param>
    void Register<TService>(Func<IServiceProvider, TService> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class;
}

/// <summary>
/// The factory registrations of <see cref="ISessionPluginContext.Services"/>.
/// </summary>
[Experimental("STREXP006")]
public static class PluginServiceRegistryExtensions
{
    /// <summary>
    /// Adds a service made by <paramref name="factory"/> to the session's scope, which a lookup of exactly
    /// <typeparamref name="TService"/> gets. It goes when the plugin leaves the session, and so does what it made.
    /// </summary>
    /// <example><c>context.Services.Register&lt;IMyService&gt;(services => new MyService(services.GetService(typeof(IUndoRedoService)) ...))</c></example>
    /// <typeparam name="TService">The type the service is registered as, and looked up by.</typeparam>
    /// <param name="services">The services of the plugin context.</param>
    /// <param name="factory">Makes the service, with the services of the scope it is made for.</param>
    /// <param name="lifetime">One instance for the session, or one per scope that asks for it (the session, each editor).</param>
    /// <exception cref="NotSupportedException">The registry takes instances only.</exception>
    public static void Register<TService>(this IPluginRegistry<object> services, Func<IServiceProvider, TService> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services is not IPluginServiceRegistry registry)
            throw new NotSupportedException("This registry takes service instances only.");
        registry.Register(factory, lifetime);
    }
}
