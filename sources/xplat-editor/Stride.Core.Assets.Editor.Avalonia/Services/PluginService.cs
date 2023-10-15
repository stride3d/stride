// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Avalonia.Services;

// FIXME: should the service by UI-agnostic and moved to Stride.Core.Assets.Editor?
// Maybe when SessionViewModel is also moved there...

public sealed class PluginService
{
    internal void RegisterSession(SessionViewModel session, ILogger logger)
    {
        foreach (var plugin in AssetsPlugin.RegisteredPlugins)
        {
            plugin.InitializePlugin(logger);

            // Asset view models types
            var assetViewModelsTypes = new Dictionary<Type, Type>();
            plugin.RegisterAssetViewModelTypes(assetViewModelsTypes);
            AssertType(typeof(Asset), assetViewModelsTypes.Select(x => x.Key));
            AssertType(typeof(AssetViewModel), assetViewModelsTypes.Select(x => x.Value));
            session.AssetViewModelTypes.AddRange(assetViewModelsTypes);
        }
    }

    private static void AssertType(Type baseType, Type specificType)
    {
        if (!baseType.IsAssignableFrom(specificType))
            throw new ArgumentException($"Type [{specificType.FullName}] must be assignable to {baseType.FullName}", nameof(specificType));
    }

    private static void AssertType(Type baseType, IEnumerable<Type> specificTypes)
    {
        specificTypes.ForEach(x => AssertType(baseType, x));
    }
}
