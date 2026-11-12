// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// Constructor injection over a service provider.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates an instance of <typeparamref name="T"/> with the longest public constructor whose parameters are either given in
    /// <paramref name="arguments"/> (in any order) or found in <paramref name="provider"/>.
    /// </summary>
    /// <typeparam name="T">The type to create.</typeparam>
    /// <param name="provider">The services the constructor can ask for.</param>
    /// <param name="arguments">The arguments that are not services, such as the asset of an editor.</param>
    /// <returns>The new instance.</returns>
    /// <exception cref="InvalidOperationException">No constructor can be called with these arguments and services.</exception>
    public static T CreateInstance<T>(this IServiceProvider provider, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return ActivatorUtilities.CreateInstance<T>(provider, arguments);
    }

    /// <summary>
    /// Creates an instance of <paramref name="type"/> with the longest public constructor whose parameters are either given in
    /// <paramref name="arguments"/> (in any order) or found in <paramref name="provider"/>.
    /// </summary>
    /// <param name="provider">The services the constructor can ask for.</param>
    /// <param name="type">The type to create.</param>
    /// <param name="arguments">The arguments that are not services, such as the asset of an editor.</param>
    /// <returns>The new instance.</returns>
    /// <exception cref="InvalidOperationException">No constructor can be called with these arguments and services.</exception>
    public static object CreateInstance(this IServiceProvider provider, Type type, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(type);
        return ActivatorUtilities.CreateInstance(provider, type, arguments);
    }
}
