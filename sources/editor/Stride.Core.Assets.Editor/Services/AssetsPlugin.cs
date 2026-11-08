// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

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
    /// The assemblies whose declarations the editor registers in this plugin's name: the types they declare with attributes
    /// (<see cref="AssetViewModelAttribute"/> and the other view and view model attributes), their
    /// <see cref="EditorSettingsAttribute"/> keys and their declared images. By default, the plugin's own assembly.
    /// </summary>
    /// <remarks>
    /// A plugin overrides it to cover an assembly that cannot hold a plugin itself, such as an assembly of asset view models
    /// that does not reference the editor (<c>Stride.Assets.Presentation</c>, covered by the default assets plugin of
    /// <c>Stride.Assets.Editor</c>). Each assembly should be covered by one plugin only: two plugins that scan the same
    /// assembly register its types twice.
    /// </remarks>
    public virtual IReadOnlyList<Assembly> ScannedAssemblies => [GetType().Assembly];

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
    /// Called when the plugin joins the session, after <see cref="InitializePlugin"/>: registers what the plugin adds to
    /// the editor in <paramref name="context"/>, which removes it when the plugin's assembly is unloaded.
    /// </summary>
    /// <remarks>
    /// The editor itself registers what the plugin's assembly declares with attributes (<see cref="AssetViewModelAttribute"/>
    /// and the other view and view model attributes, <see cref="EditorSettingsAttribute"/>, and the declared images).
    /// The default implementation passes the contributions of the <c>Register*</c> methods to the context.
    /// </remarks>
    public virtual void Initialize(IEditorPluginContext context)
    {
        var assetViewModelTypes = new Dictionary<Type, Type>();
        RegisterAssetViewModelTypes(assetViewModelTypes);
        foreach (var (assetType, viewModelType) in assetViewModelTypes)
            context.AssetViewModelTypes.Register(assetType, viewModelType);

        var primitiveTypes = new List<Type>();
        RegisterPrimitiveTypes(primitiveTypes);
        foreach (var type in primitiveTypes)
            context.PrimitiveTypes.Register(type);
    }

    /// <summary>
    /// Called when the session's editor initializes: the place for the property grid commands and updaters.
    /// </summary>
    public virtual void InitializeSession(ISessionViewModel session)
    {
    }

    /// <summary>
    /// Called when the session's editor initializes: registers the property grid commands and updaters and the session
    /// services of the plugin in <paramref name="context"/>, which removes them when the plugin leaves the session.
    /// </summary>
    /// <remarks>
    /// The default implementation calls <see cref="InitializeSession(ISessionViewModel)"/>; the editor removes the property
    /// grid commands and updaters that it adds directly.
    /// </remarks>
    public virtual void InitializeSession(ISessionPluginContext context)
    {
        InitializeSession(context.Session);
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
    /// Registers every concrete plugin type of <paramref name="assembly"/> that is not registered yet, except the default
    /// plugins of an editor (<see cref="EditorDefaultPluginAttribute"/>), which their host registers.
    /// </summary>
    /// <returns>The new plugins.</returns>
    public static IReadOnlyList<AssetsPlugin> RegisterPlugins(Assembly assembly)
    {
        var plugins = new List<AssetsPlugin>();
        lock (RegisteredPluginsLock)
        {
            foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(AssetsPlugin)))
            {
                if (!IsDiscoverable(type))
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) is null || registeredPlugins.Any(x => x.GetType() == type))
                    continue;

                // A plugin whose constructor fails is reported and left out; the assembly's other plugins still register
                try
                {
                    plugins.Add(RegisterPlugin(type));
                }
                catch (Exception e)
                {
                    GlobalLogger.GetLogger(nameof(AssetsPlugin)).Error($"The plugin [{type.FullName}] could not be created.", e);
                }
            }
        }
        return plugins;
    }

    /// <summary>
    /// Gets whether the plugin discovery registers <paramref name="type"/>: a concrete plugin type that is not the default
    /// plugin of an editor (<see cref="EditorDefaultPluginAttribute"/>).
    /// </summary>
    public static bool IsDiscoverable(Type type)
    {
        return typeof(AssetsPlugin).IsAssignableFrom(type) && !type.IsAbstract && !type.IsGenericTypeDefinition
            && !type.IsDefined(typeof(EditorDefaultPluginAttribute), inherit: false);
    }

    /// <summary>
    /// Registers the view model types of asset types, besides the ones the assembly declares with <see cref="AssetViewModelAttribute"/>,
    /// which the editor registers itself.
    /// </summary>
    public virtual void RegisterAssetViewModelTypes(IDictionary<Type, Type> assetViewModelTypes)
    {
    }

    public virtual void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
    }
}
