// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Views;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModels;
using Stride.Editor.Preview.Views;
using Stride.GameStudio.Avalonia.Internal;

namespace Stride.GameStudio.Avalonia.Services;

internal sealed class PluginService : IAssetsPluginService
{
    private readonly IDispatcherService dispatcherService;

    private readonly Dictionary<Type, Type> assetViewModelTypes = new();
    private readonly Dictionary<Type, Type> editorViewModelTypes = new();
    private readonly Dictionary<Type, Type> editorViewTypes = new();
    private readonly Dictionary<Type, Type> previewViewModelTypes = new();
    private readonly Dictionary<Type, Type> previewViewViewTypes = new();
    private readonly List<Type> primitiveTypes = [];

    public PluginService(IDispatcherService dispatcherService)
    {
        this.dispatcherService = dispatcherService;
    }

    public IReadOnlyCollection<AssetsPlugin> Plugins => AssetsPlugin.RegisteredPlugins;

    public void EnsureInitialized(ILogger logger)
    {
        foreach (var plugin in AssetsPlugin.RegisteredPlugins)
        {
            plugin.InitializePlugin(logger);

            // Asset view models types
            var registeredAssetViewModelsTypes = new Dictionary<Type, Type>();
            plugin.RegisterAssetViewModelTypes(registeredAssetViewModelsTypes);
            AssertType(typeof(Asset), registeredAssetViewModelsTypes.Select(x => x.Key));
            AssertType(typeof(AssetViewModel), registeredAssetViewModelsTypes.Select(x => x.Value));
            assetViewModelTypes.AddRange(registeredAssetViewModelsTypes);

            if (plugin is AssetsEditorPlugin editorPlugin)
            {
                // Asset editor view model types
                var registeredAssetEditorViewModelsTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetEditorViewModelTypes(registeredAssetEditorViewModelsTypes);
                AssertType(typeof(AssetViewModel), registeredAssetEditorViewModelsTypes.Select(x => x.Key));
                AssertType(typeof(AssetEditorViewModel), registeredAssetEditorViewModelsTypes.Select(x => x.Value));
                editorViewModelTypes.AddRange(registeredAssetEditorViewModelsTypes);

                // Asset editor view types
                var registeredAssetEditorViewTypes = new Dictionary<Type, Type>();
                editorPlugin.RegisterAssetEditorViewTypes(registeredAssetEditorViewTypes);
                AssertType(typeof(AssetEditorViewModel), registeredAssetEditorViewTypes.Select(x => x.Key));
                AssertType(typeof(IAssetEditorView), registeredAssetEditorViewTypes.Select(x => x.Value));
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

                // Primitive types
                var registeredPrimitiveTypes = new List<Type>();
                editorPlugin.RegisterPrimitiveTypes(registeredPrimitiveTypes);
                primitiveTypes.AddRange(registeredPrimitiveTypes);

                // Template providers
                dispatcherService.Invoke(() =>
                {
                    var templateProviders = new List<ITemplateProvider>();
                    editorPlugin.RegisterTemplateProviders(templateProviders);
                    templateProviders.ForEach(RegisterTemplateProvider);
                });
            }
        }

        return;

        void RegisterTemplateProvider(ITemplateProvider provider)
        {
            if (provider is not TemplateProviderBase avaloniaProvider)
                return;

            var category = PropertyViewHelper.GetTemplateCategory(avaloniaProvider);
            switch (category)
            {
                case PropertyViewHelper.Category.PropertyHeader:
                    PropertyViewHelper.HeaderProviders.RegisterTemplateProvider(avaloniaProvider);
                    break;
                case PropertyViewHelper.Category.PropertyFooter:
                    PropertyViewHelper.FooterProviders.RegisterTemplateProvider(avaloniaProvider);
                    break;
                case PropertyViewHelper.Category.PropertyEditor:
                    PropertyViewHelper.EditorProviders.RegisterTemplateProvider(avaloniaProvider);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public Type? GetAssetViewModelType(Type assetType) => TypeHelpers.TryGetTypeOrBase(assetType, assetViewModelTypes);

    public Type? GetEditorViewModelType(Type viewModelType) => TypeHelpers.TryGetTypeOrBase(viewModelType, editorViewModelTypes);

    public Type? GetEditorViewType(Type editorViewModelType) => TypeHelpers.TryGetTypeOrBase(editorViewModelType, editorViewTypes);

    public Type? GetPreviewViewModelType(Type previewType) => TypeHelpers.TryGetTypeOrBase(previewType, previewViewModelTypes);

    public Type? GetPreviewViewType(Type previewType) => TypeHelpers.TryGetTypeOrBase(previewType, previewViewViewTypes);

    public IReadOnlyList<Type> GetPrimitiveTypes()
    {
        return primitiveTypes.AsReadOnly();
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
