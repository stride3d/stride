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
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.View;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;
using Stride.Editor.Preview;

namespace Stride.GameStudio.Services;

/// <summary>
/// Registers the plugins of all asset assemblies with the session, including assemblies loaded after it opened.
/// </summary>
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

    private readonly HashSet<AssetsPlugin> sessionPlugins = [];

    // What each plugin registered that its unloading takes back by value rather than by its assembly: primitive types and
    // enum images (keyed by types of other assemblies, such as the runtime's enums), template providers, copy/paste
    // processors, and the property grid commands and updaters of its session initialization
    private readonly Dictionary<AssetsPlugin, List<object>> pluginRegistrations = [];

    private sealed record PrimitiveTypeRegistration(Type Type);

    private sealed record EnumImageRegistration(object Value);

    private SessionViewModel? session;
    private ILogger? logger;

    public PluginService()
    {
        AssetsPlugin.PluginsDiscovered += PluginsDiscovered;
        AssetsPlugin.PluginsRemoved += PluginsRemoved;
    }

    public void RegisterSession(SessionViewModel session, ILogger logger)
    {
        this.session = session;
        this.logger = logger;

        AssetsPlugin.DiscoverPlugins();
        foreach (var plugin in Plugins.ToList())
            RegisterPlugin(plugin, session, logger);
    }

    private void PluginsDiscovered(IReadOnlyList<AssetsPlugin> plugins)
    {
        if (session is null)
            return;

        // A plugin arriving after the session opened joins it right away
        var currentSession = session;
        currentSession.Dispatcher.Invoke(() =>
        {
            foreach (var plugin in plugins)
            {
                RegisterPlugin(plugin, currentSession, logger!);
                if (currentSession.IsEditorInitialized)
                    InitializePluginSession(plugin, currentSession);
            }
        });
    }

    public void InitializeSession(SessionViewModel session)
    {
        foreach (var plugin in Plugins.ToList())
            InitializePluginSession(plugin, session);
    }

    // Records the property grid commands and updaters the plugin adds, to remove them on unload
    private void InitializePluginSession(AssetsPlugin plugin, SessionViewModel session)
    {
        var properties = session.AssetViewProperties;
        var commands = properties.NodePresenterCommands.ToList();
        var updaters = properties.NodePresenterUpdaters.ToList();
        plugin.InitializeSession(session);

        var added = properties.NodePresenterCommands.Except(commands).Cast<object>()
            .Concat(properties.NodePresenterUpdaters.Except(updaters))
            .ToList();
        if (added.Count == 0)
            return;
        if (!pluginRegistrations.TryGetValue(plugin, out var registrations))
            pluginRegistrations[plugin] = registrations = [];
        registrations.AddRange(added);
    }

    private void RegisterPlugin(AssetsPlugin plugin, SessionViewModel session, ILogger logger)
    {
        if (!sessionPlugins.Add(plugin))
            return;

        plugin.InitializePlugin(logger);

        // Asset view models types
        var assetViewModelsTypes = new Dictionary<Type, Type>();
        plugin.RegisterAssetViewModelTypes(assetViewModelsTypes);
        AssertType(typeof(Asset), assetViewModelsTypes.Select(x => x.Key));
        AssertType(typeof(AssetViewModel), assetViewModelsTypes.Select(x => x.Value));
        assetViewModelTypes.AddRange(assetViewModelsTypes);

        // Removed when the plugin's assembly is unloaded
        var registrations = new List<object>();

        // Primitive types
        var registeredPrimitiveTypes = new List<Type>();
        plugin.RegisterPrimitiveTypes(registeredPrimitiveTypes);
        primitiveTypes.AddRange(registeredPrimitiveTypes);
        registrations.AddRange(registeredPrimitiveTypes.Select(x => new PrimitiveTypeRegistration(x)));

        if (plugin is AssetsEditorPlugin editorPlugin)
        {
            editorPlugin.RegisterTypeImages(logger);

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
            registrations.AddRange(images.Keys.Select(x => new EnumImageRegistration(x)));

            // Editor and property item template providers
            var providers = new List<ITemplateProvider>();
            editorPlugin.RegisterTemplateProviders(providers);
            var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
            foreach (var provider in providers)
            {
                dialogService.RegisterAdditionalTemplateProvider(provider);
                registrations.Add(provider);
            }

            if (session.ServiceProvider.TryGet<ICopyPasteService>() is { } copyPasteService)
            {
                // Copy processors
                var copyProcessors = new List<ICopyProcessor>();
                editorPlugin.RegisterCopyProcessors(copyProcessors, session);
                foreach (var processor in copyProcessors)
                {
                    copyPasteService.RegisterProcessor(processor);
                    registrations.Add(processor);
                }
                // Paste processors
                var pasteProcessors = new List<IPasteProcessor>();
                editorPlugin.RegisterPasteProcessors(pasteProcessors, session);
                foreach (var processor in pasteProcessors)
                {
                    copyPasteService.RegisterProcessor(processor);
                    registrations.Add(processor);
                }
                // Post paste processors
                var postPasteProcessors = new List<IAssetPostPasteProcessor>();
                editorPlugin.RegisterPostPasteProcessors(postPasteProcessors, session);
                foreach (var processor in postPasteProcessors)
                {
                    copyPasteService.RegisterProcessor(processor);
                    registrations.Add(processor);
                }
            }

        }

        if (registrations.Count > 0)
            pluginRegistrations[plugin] = registrations;
    }

    private void PluginsRemoved(IReadOnlyList<AssetsPlugin> plugins)
    {
        if (session is null)
            return;

        var currentSession = session;
        currentSession.Dispatcher.Invoke(() =>
        {
            foreach (var plugin in plugins)
                UnregisterPlugin(plugin, currentSession);
        });
    }

    /// <summary>
    /// Removes what a plugin of an unloaded assembly registered.
    /// </summary>
    private void UnregisterPlugin(AssetsPlugin plugin, SessionViewModel session)
    {
        if (!sessionPlugins.Remove(plugin))
            return;

        var assembly = plugin.GetType().Assembly;
        RemoveTypesOf(assetViewModelTypes, assembly);
        RemoveTypesOf(editorViewModelTypes, assembly);
        RemoveTypesOf(editorViewTypes, assembly);
        RemoveTypesOf(previewViewModelTypes, assembly);
        RemoveTypesOf(previewViewViewTypes, assembly);

        if (pluginRegistrations.Remove(plugin, out var registrations))
        {
            var dialogService = session.ServiceProvider.Get<IEditorDialogService>();
            var copyPasteService = session.ServiceProvider.TryGet<ICopyPasteService>();
            foreach (var registration in registrations)
            {
                switch (registration)
                {
                    case PrimitiveTypeRegistration primitiveType:
                        primitiveTypes.Remove(primitiveType.Type);
                        break;
                    case EnumImageRegistration enumImage:
                        enumImages.Remove(enumImage.Value);
                        break;
                    case INodePresenterCommand command:
                        session.AssetViewProperties.UnregisterNodePresenterCommand(command);
                        break;
                    case INodePresenterUpdater updater:
                        session.AssetViewProperties.UnregisterNodePresenterUpdater(updater);
                        break;
                    case ITemplateProvider provider:
                        dialogService.UnregisterAdditionalTemplateProvider(provider);
                        break;
                    case ICopyProcessor processor:
                        copyPasteService?.UnregisterProcessor(processor);
                        break;
                    case IPasteProcessor processor:
                        copyPasteService?.UnregisterProcessor(processor);
                        break;
                    case IAssetPostPasteProcessor processor:
                        copyPasteService?.UnregisterProcessor(processor);
                        break;
                }
            }

            // An enum keeps its images while another plugin still gives one of its values an image
            enumTypesWithImages.Clear();
            enumTypesWithImages.AddRange(enumImages.Keys.Select(x => x.GetType()));
        }

        static void RemoveTypesOf(Dictionary<Type, Type> types, System.Reflection.Assembly assembly)
        {
            foreach (var entry in types.Where(x => x.Key.Assembly == assembly || x.Value.Assembly == assembly).ToList())
                types.Remove(entry.Key);
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
        // IsAssignableFrom is false for an open generic type, so check its interfaces too
        if (!baseType.IsAssignableFrom(specificType) && !specificType.GetInterfaces().Contains(baseType))
            throw new ArgumentException($"Type [{specificType.FullName}] must be assignable to {baseType.FullName}", nameof(specificType));
    }

    private static void AssertType(Type baseType, IEnumerable<Type> specificTypes)
    {
        specificTypes.ForEach(x => AssertType(baseType, x));
    }
}
