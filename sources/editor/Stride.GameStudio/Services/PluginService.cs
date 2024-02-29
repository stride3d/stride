// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.View;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;
using Stride.Editor.Preview;

namespace Stride.GameStudio.Services;

public class PluginService : IAssetsPluginService
{
    private readonly Dictionary<Type, Type> assetViewModelTypes = [];
    private readonly Dictionary<Type, Type> editorViewModelTypes = [];
    private readonly Dictionary<Type, Type> editorViewTypes = [];
    private readonly Dictionary<Type, Type> previewViewModelTypes = [];
    private readonly Dictionary<Type, Type> previewViewViewTypes = [];

    public IReadOnlyList<AssetsPlugin> Plugins => AssetsPlugin.RegisteredPlugins;

    private readonly Dictionary<object, object> enumImages = [];

    private readonly HashSet<Type> enumTypesWithImages = [];

    private readonly List<Type> primitiveTypes = [];

    public void RegisterSession(SessionViewModel session, ILogger logger)
    {
        foreach (var plugin in Plugins)
        {
            plugin.InitializePlugin(logger);

            // Asset view models types
            var assetViewModelsTypes = new Dictionary<Type, Type>();
            plugin.RegisterAssetViewModelTypes(assetViewModelsTypes);
            AssertType(typeof(Asset), assetViewModelsTypes.Select(x => x.Key));
            AssertType(typeof(AssetViewModel), assetViewModelsTypes.Select(x => x.Value));
            assetViewModelTypes.AddRange(assetViewModelsTypes);

            // Primitive types
            var registeredPrimitiveTypes = new List<Type>();
            plugin.RegisterPrimitiveTypes(registeredPrimitiveTypes);
            primitiveTypes.AddRange(registeredPrimitiveTypes);

            if (plugin is AssetsEditorPlugin editorPlugin)
            {
                // Asset editor view models types
                var registeredAssetEditorViewModelTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetEditorViewModelTypes(registeredAssetEditorViewModelTypes);
                AssertType(typeof(AssetViewModel), registeredAssetEditorViewModelTypes.Select(x => x.Key));
                AssertType(typeof(IAssetEditorViewModel), registeredAssetEditorViewModelTypes.Select(x => x.Value));
                editorViewModelTypes.AddRange(registeredAssetEditorViewModelTypes);

                // Asset editor view types
                var registeredAssetEditorViewTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetEditorViewTypes(registeredAssetEditorViewTypes);
                AssertType(typeof(AssetEditorViewModel), registeredAssetEditorViewTypes.Select(x => x.Key));
                AssertType(typeof(IEditorView), registeredAssetEditorViewTypes.Select(x => x.Value));
                editorViewTypes.AddRange(registeredAssetEditorViewTypes);

                // Asset preview view model types
                var registeredAssetPreviewViewModelTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetPreviewViewModelTypes(registeredAssetPreviewViewModelTypes);
                AssertType(typeof(IAssetPreview), registeredAssetPreviewViewModelTypes.Select(x => x.Key));
                AssertType(typeof(IAssetPreviewViewModel), registeredAssetPreviewViewModelTypes.Select(x => x.Value));
                previewViewModelTypes.AddRange(registeredAssetPreviewViewModelTypes);

                // Asset preview view types
                var registeredAssetPreviewViewTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetPreviewViewTypes(registeredAssetPreviewViewTypes);
                AssertType(typeof(IAssetPreview), registeredAssetPreviewViewTypes.Select(x => x.Key));
                AssertType(typeof(IPreviewView), registeredAssetPreviewViewTypes.Select(x => x.Value));
                previewViewViewTypes.AddRange(registeredAssetPreviewViewTypes);

                // Enum images
                var images = new Dictionary<object, object>();
                editorPlugin.RegisterEnumImages(images);
                AssertType(typeof(Enum), images.Select(x => x.Key.GetType()));
                enumImages.AddRange(images);
                enumTypesWithImages.AddRange(images.Select(x => x.Key.GetType()));

                // Editor and property item template providers
                var providers = new List<ITemplateProvider>();
                editorPlugin.RegisterTemplateProviders(providers);
                var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
                foreach (var provider in providers)
                {
                    dialogService.RegisterAdditionalTemplateProvider(provider);
                }

                if (session.ServiceProvider.TryGet<ICopyPasteService>() is { } copyPasteService)
                {
                    // Copy processors
                    var copyProcessors = new List<ICopyProcessor>();
                    editorPlugin.RegisterCopyProcessors(copyProcessors, session);
                    foreach (var processor in copyProcessors)
                    {
                        copyPasteService.RegisterProcessor(processor);
                    }
                    // Paste processors
                    var pasteProcessors = new List<IPasteProcessor>();
                    editorPlugin.RegisterPasteProcessors(pasteProcessors, session);
                    foreach (var processor in pasteProcessors)
                    {
                        copyPasteService.RegisterProcessor(processor);
                    }
                    // Post paste processors
                    var postPasteProcessors = new List<IAssetPostPasteProcessor>();
                    editorPlugin.RegisterPostPasteProcessors(postPasteProcessors, session);
                    foreach (var processor in postPasteProcessors)
                    {
                        copyPasteService.RegisterProcessor(processor);
                    }
                }
            }
        }
    }

    public bool HasImagesForEnum(SessionViewModel? session, Type enumType)
    {
        return session != null && enumTypesWithImages.Contains(enumType);
    }

    public object? GetImageForEnum(SessionViewModel? session, object value)
    {
        if (session == null)
            return null;

        enumImages.TryGetValue(value, out var image);
        return image;
    }

    public IEnumerable<Type> GetPrimitiveTypes(SessionViewModel session)
    {
        return primitiveTypes;
    }

    public bool HasEditorView(SessionViewModel session, Type viewModelType)
    {
        return editorViewModelTypes.Any(x => x.Key.IsAssignableFrom(viewModelType));
    }

    public Type? GetAssetViewModelType(Type assetType) => TypeHelpers.TryGetTypeOrBase(assetType, assetViewModelTypes);

    public Type? GetEditorViewModelType(Type viewModelType) => TypeHelpers.TryGetTypeOrBase(viewModelType, editorViewModelTypes);

    public Type? GetEditorViewType(Type editorViewModelType) => TypeHelpers.TryGetTypeOrBase(editorViewModelType, editorViewTypes);

    public Type? GetPreviewViewModelType(Type previewType) => TypeHelpers.TryGetTypeOrBase(previewType, previewViewModelTypes);

    public Type? GetPreviewViewType(Type previewType) => TypeHelpers.TryGetTypeOrBase(previewType, previewViewViewTypes);

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
