// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Presentation;

public abstract class AssetsPlugin
{
    private static readonly List<AssetsPlugin> registeredPlugins = [];

    public static IReadOnlyList<AssetsPlugin> RegisteredPlugins => registeredPlugins;

    public abstract void InitializePlugin(ILogger logger);

    public abstract void InitializeSession(ISessionViewModel session);

    public static void RegisterPlugin(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) == null)
            throw new ArgumentException("The given type does not have a parameterless constructor.");

        if (!typeof(AssetsPlugin).IsAssignableFrom(type))
            throw new ArgumentException("The given type does not inherit from AssetsPlugin.");

        if (RegisteredPlugins.Any(x => x.GetType() == type))
            throw new InvalidOperationException("The plugin type is already registered.");

        var plugin = (AssetsPlugin)Activator.CreateInstance(type)!;
        registeredPlugins.Add(plugin);
    }

    public void RegisterAssetViewModelTypes(IDictionary<Type, Type> assetViewModelTypes)
    {
        var pluginAssembly = GetType().Assembly;
        foreach (var type in pluginAssembly.GetTypes())
        {
            if (typeof(AssetViewModel).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetViewModelAttribute>() is { } attribute)
            {
                assetViewModelTypes.Add(attribute.AssetType, type);
            }
        }
    }
}
