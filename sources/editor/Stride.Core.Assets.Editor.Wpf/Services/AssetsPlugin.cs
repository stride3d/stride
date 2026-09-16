// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Entry point of an editor extension assembly; one instance per concrete subclass is created when the assembly is registered.
/// </summary>
[AssemblyScan]
public abstract class AssetsPlugin
{
    private static readonly object RegisteredPluginsLock = new();
    // Copy-on-write: plugins register on any thread while the UI thread lists them
    private static volatile IReadOnlyList<AssetsPlugin> registeredPlugins = [];
    private static bool discovering;

    public static IReadOnlyList<AssetsPlugin> RegisteredPlugins => registeredPlugins;

    /// <summary>
    /// Raised with the plugins that <see cref="DiscoverPlugins"/> registers from an asset assembly.
    /// </summary>
    public static event Action<IReadOnlyList<AssetsPlugin>>? PluginsDiscovered;

    /// <summary>
    /// Raised with the plugins removed when their asset assembly is unregistered.
    /// </summary>
    public static event Action<IReadOnlyList<AssetsPlugin>>? PluginsRemoved;

    /// <summary>
    /// Called when the plugin joins the session, before its Register* methods.
    /// </summary>
    public virtual void InitializePlugin(ILogger logger)
    {
    }

    /// <summary>
    /// Called when the session's editor initializes: the place for the property grid commands and updaters.
    /// </summary>
    public virtual void InitializeSession(SessionViewModel session)
    {
    }

    public static AssetsPlugin RegisterPlugin(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) is null)
            throw new ArgumentException("The given type does not have a parameterless constructor.");

        if (!typeof(AssetsPlugin).IsAssignableFrom(type))
            throw new ArgumentException($"The given type does not inherit from {nameof(AssetsPlugin)}.");

        lock (RegisteredPluginsLock)
        {
            if (registeredPlugins.Any(x => x.GetType() == type))
                throw new InvalidOperationException("The plugin type is already registered.");

            var plugin = (AssetsPlugin)Activator.CreateInstance(type)!;
            registeredPlugins = [.. registeredPlugins, plugin];
            return plugin;
        }
    }

    /// <summary>
    /// Registers the plugins of every asset assembly loaded so far, then of each one registered later.
    /// </summary>
    public static void DiscoverPlugins()
    {
        if (discovering)
            return;
        discovering = true;

        AssemblyRegistry.AssemblyRegistered += (_, e) =>
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                Discover(e.Assembly);
        };
        AssemblyRegistry.AssemblyUnregistered += (_, e) =>
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                Undiscover(e.Assembly);
        };
        foreach (var assembly in AssemblyRegistry.Find(AssemblyCommonCategories.Assets))
            Discover(assembly);

        static void Discover(Assembly assembly)
        {
            var plugins = RegisterPlugins(assembly);
            if (plugins.Count > 0)
                PluginsDiscovered?.Invoke(plugins);
        }

        static void Undiscover(Assembly assembly)
        {
            var plugins = UnregisterPlugins(assembly);
            if (plugins.Count > 0)
                PluginsRemoved?.Invoke(plugins);
        }
    }

    /// <summary>
    /// Removes the registered plugins of <paramref name="assembly"/>.
    /// </summary>
    /// <returns>The removed plugins.</returns>
    public static IReadOnlyList<AssetsPlugin> UnregisterPlugins(Assembly assembly)
    {
        lock (RegisteredPluginsLock)
        {
            var plugins = registeredPlugins.Where(x => x.GetType().Assembly == assembly).ToList();
            if (plugins.Count > 0)
                registeredPlugins = registeredPlugins.Except(plugins).ToList();
            return plugins;
        }
    }

    /// <summary>
    /// Registers every concrete plugin type of <paramref name="assembly"/> that is not registered yet.
    /// </summary>
    /// <returns>The new plugins.</returns>
    public static IReadOnlyList<AssetsPlugin> RegisterPlugins(Assembly assembly)
    {
        var plugins = new List<AssetsPlugin>();
        lock (RegisteredPluginsLock)
        {
            foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(AssetsPlugin)))
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) is null || registeredPlugins.Any(x => x.GetType() == type))
                    continue;
                plugins.Add(RegisterPlugin(type));
            }
        }
        return plugins;
    }

    public virtual void RegisterAssetViewModelTypes(IDictionary<Type, Type> assetViewModelTypes)
    {
        foreach (var type in AssemblyRegistry.GetScanTypes(GetType().Assembly, typeof(AssetViewModel)))
        {
            if (typeof(AssetViewModel).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetViewModelAttribute>() is { } attribute)
            {
                assetViewModelTypes.Add(attribute.AssetType, type);
            }
        }
    }

    public virtual void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
    }

    protected internal virtual void SessionLoaded(SessionViewModel session)
    {
        // Intentionally does nothing
    }

    protected internal virtual void SessionDisposed(SessionViewModel session)
    {
        // Intentionally does nothing
    }

}
