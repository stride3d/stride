// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Services;

public sealed class PluginService
{
    private readonly Dictionary<Type, Type> assetEditorViewModelTypes = [];
    private readonly Dictionary<Type, Type> assetEditorViewTypes = [];

    internal void RegisterSession(SessionViewModel session, ILogger logger)
    {
        foreach (var plugin in AssetsPlugin.RegisteredPlugins)
        {
            plugin.InitializePlugin(logger);

            // Asset view models types
            var registeredAssetViewModelsTypes = new Dictionary<Type, Type>();
            plugin.RegisterAssetViewModelTypes(registeredAssetViewModelsTypes);
            AssertType(typeof(Asset), registeredAssetViewModelsTypes.Select(x => x.Key));
            AssertType(typeof(AssetViewModel), registeredAssetViewModelsTypes.Select(x => x.Value));
            session.AssetViewModelTypes.AddRange(registeredAssetViewModelsTypes);

            if (plugin is AssetsEditorPlugin editorPlugin)
            {
                // Asset editor view model types
                var registeredAssetEditorViewModelsTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetEditorViewModelTypes(registeredAssetEditorViewModelsTypes);
                AssertType(typeof(AssetViewModel), registeredAssetEditorViewModelsTypes.Select(x => x.Key));
                AssertType(typeof(AssetEditorViewModel), registeredAssetEditorViewModelsTypes.Select(x => x.Value));
                assetEditorViewModelTypes.AddRange(registeredAssetEditorViewModelsTypes);

                // Asset editor view types
                var registeredAssetEditorViewTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetEditorViewTypes(registeredAssetEditorViewTypes);
                AssertType(typeof(AssetEditorViewModel), registeredAssetEditorViewTypes.Select(x => x.Key));
                AssertType(typeof(IAssetEditorView), registeredAssetEditorViewTypes.Select(x => x.Value));
                assetEditorViewTypes.AddRange(registeredAssetEditorViewTypes);
            }
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
