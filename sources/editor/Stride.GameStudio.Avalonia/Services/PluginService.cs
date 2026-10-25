// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Views;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;
using Stride.GameStudio.Avalonia.Internal;

namespace Stride.GameStudio.Avalonia.Services;

/// <summary>
/// Registers the plugins of all asset assemblies with the session, including assemblies loaded after it opened, and
/// removes what a plugin registered when its assembly is unloaded. The Avalonia counterpart of the WPF Game Studio's
/// <c>PluginService</c>: the template providers go to the property grid's <see cref="PropertyViewHelper"/> selectors.
/// </summary>
internal sealed class PluginService : IAssetsPluginService, IDisposable
{
    private readonly IDispatcherService dispatcherService;

    private readonly Dictionary<Type, Type> assetViewModelTypes = [];
    private readonly Dictionary<Type, Type> editorViewModelTypes = [];
    private readonly Dictionary<Type, Type> editorViewTypes = [];
    private readonly Dictionary<Type, Type> previewViewModelTypes = [];
    private readonly Dictionary<Type, Type> previewViewTypes = [];
    private readonly Dictionary<object, object> enumImages = [];
    private readonly HashSet<Type> enumTypesWithImages = [];
    private readonly List<Type> primitiveTypes = [];
    private readonly HashSet<AssetsPlugin> sessionPlugins = [];

    // Registrations to remove one by one when a plugin's assembly is unloaded (they can be keyed by types of other assemblies)
    private readonly Dictionary<AssetsPlugin, List<object>> pluginRegistrations = [];

    private sealed record PrimitiveTypeRegistration(Type Type);

    private sealed record EnumImageRegistration(object Value);

    private ISessionViewModel? session;
    private ILogger? logger;

    public PluginService(IDispatcherService dispatcherService)
    {
        this.dispatcherService = dispatcherService;
        AssetsPlugin.PluginsDiscovered += PluginsDiscovered;
        AssetsPlugin.PluginsRemoved += PluginsRemoved;
    }

    public IReadOnlyList<AssetsPlugin> Plugins => AssetsPlugin.RegisteredPlugins;

    /// <summary>
    /// Stops following the plugin registry and takes back what the plugins registered outside this service (the template
    /// providers of the property grid are process-wide): the plugins join the next editor's service afresh.
    /// </summary>
    public void Dispose()
    {
        AssetsPlugin.PluginsDiscovered -= PluginsDiscovered;
        AssetsPlugin.PluginsRemoved -= PluginsRemoved;
        var currentSession = session;
        session = null;
        foreach (var plugin in sessionPlugins.ToList())
        {
            try
            {
                UnregisterPlugin(plugin, currentSession);
            }
            catch (Exception e)
            {
                logger?.Error($"The plugin [{plugin.GetType().FullName}] could not leave the session.", e);
            }
        }
    }

    /// <summary>
    /// The plugins that joined the current session.
    /// </summary>
    public IReadOnlyCollection<AssetsPlugin> SessionPlugins => sessionPlugins;

    public void RegisterSession(ISessionViewModel session, ILogger logger)
    {
        this.session = session;
        this.logger = logger;

        // The plugins of the asset assemblies loaded so far (the editor's own and the companion packages of the session),
        // and of each one registered later
        AssetsPlugin.DiscoverPlugins();
        foreach (var plugin in Plugins.ToList())
            TryRegisterPlugin(plugin, session, logger);
    }

    private void PluginsDiscovered(IReadOnlyList<AssetsPlugin> plugins)
    {
        if (session is null)
            return;

        // A plugin arriving after the session opened joins it right away: inline on the UI thread, queued there from
        // another one (this runs in an assembly's registration, under the loader's lock: waiting for the UI thread,
        // which may be waiting for that lock, could deadlock)
        var currentSession = session;
        if (dispatcherService.CheckAccess())
            JoinSession();
        else
            _ = dispatcherService.InvokeAsync(JoinSession);

        void JoinSession()
        {
            foreach (var plugin in plugins)
            {
                TryRegisterPlugin(plugin, currentSession, logger!, () =>
                {
                    if (currentSession is SessionViewModel { IsEditorInitialized: true })
                        InitializePluginSession(plugin, currentSession);
                });
            }
        }
    }

    // A plugin that fails to join the session is reported and left out; the other plugins still join
    private void TryRegisterPlugin(AssetsPlugin plugin, ISessionViewModel session, ILogger logger, Action? then = null)
    {
        try
        {
            RegisterPlugin(plugin, session, logger);
            then?.Invoke();
        }
        catch (Exception e)
        {
            sessionPlugins.Remove(plugin);
            logger.Error($"The plugin [{plugin.GetType().FullName}] could not join the session.", e);
        }
    }

    public void InitializeSession(ISessionViewModel session)
    {
        foreach (var plugin in Plugins.ToList())
        {
            // A plugin that fails to initialize the session is reported; the editor goes on without what it did not register
            try
            {
                InitializePluginSession(plugin, session);
            }
            catch (Exception e)
            {
                logger?.Error($"The plugin [{plugin.GetType().FullName}] could not initialize the session.", e);
            }
        }
    }

    // Records the property grid commands and updaters the plugin adds, to remove them on unload
    private void InitializePluginSession(AssetsPlugin plugin, ISessionViewModel session)
    {
        var graphService = ((SessionObjectPropertiesViewModel)session.AssetViewProperties).ServiceProvider.Get<GraphViewModelService>();
        var commands = graphService.AvailableCommands.ToList();
        var updaters = graphService.AvailableUpdaters.ToList();
        plugin.InitializeSession(session);

        var added = graphService.AvailableCommands.Except(commands).Cast<object>()
            .Concat(graphService.AvailableUpdaters.Except(updaters))
            .ToList();
        if (added.Count == 0)
            return;
        if (!pluginRegistrations.TryGetValue(plugin, out var registrations))
            pluginRegistrations[plugin] = registrations = [];
        registrations.AddRange(added);
    }

    private void RegisterPlugin(AssetsPlugin plugin, ISessionViewModel session, ILogger logger)
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
            // Asset editor view models types
            var registeredAssetEditorViewModelTypes = new Dictionary<Type, Type>();
            editorPlugin.RegisterAssetEditorViewModelTypes(registeredAssetEditorViewModelTypes);
            AssertType(typeof(AssetViewModel), registeredAssetEditorViewModelTypes.Select(x => x.Key));
            AssertType(typeof(IAssetEditorViewModel), registeredAssetEditorViewModelTypes.Select(x => x.Value));
            editorViewModelTypes.AddRange(registeredAssetEditorViewModelTypes);

            // Asset editor view types (Avalonia editor views implement IAssetEditorView)
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
            previewViewTypes.AddRange(registeredAssetPreviewViewTypes);

            // Enum images
            var images = new Dictionary<object, object>();
            editorPlugin.RegisterEnumImages(images);
            AssertType(typeof(Enum), images.Select(x => x.Key.GetType()));
            enumImages.AddRange(images);
            enumTypesWithImages.AddRange(images.Select(x => x.Key.GetType()));
            registrations.AddRange(images.Keys.Select(x => new EnumImageRegistration(x)));

            // Editor and property item template providers, into the selectors of the property grid (Avalonia providers only)
            var providers = new List<ITemplateProvider>();
            editorPlugin.RegisterTemplateProviders(providers);
            dispatcherService.Invoke(() =>
            {
                foreach (var provider in providers.OfType<TemplateProviderBase>())
                {
                    GetSelector(provider).RegisterTemplateProvider(provider);
                    registrations.Add(provider);
                }
            });

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

        // Inline on the UI thread, queued there from another one, as for a plugin joining (see PluginsDiscovered)
        var currentSession = session;
        if (dispatcherService.CheckAccess())
            LeaveSession();
        else
            _ = dispatcherService.InvokeAsync(LeaveSession);

        void LeaveSession()
        {
            foreach (var plugin in plugins)
            {
                // A plugin that fails to leave is reported; the other plugins still leave
                try
                {
                    UnregisterPlugin(plugin, currentSession);
                }
                catch (Exception e)
                {
                    logger?.Error($"The plugin [{plugin.GetType().FullName}] could not leave the session.", e);
                }
            }
        }
    }

    /// <summary>
    /// Removes what a plugin of an unloaded assembly registered. Without a session (the service is disposed), what was
    /// registered into the session's own services goes with it.
    /// </summary>
    private void UnregisterPlugin(AssetsPlugin plugin, ISessionViewModel? session)
    {
        if (!sessionPlugins.Remove(plugin))
            return;

        var assembly = plugin.GetType().Assembly;
        RemoveTypesOf(assetViewModelTypes, assembly);
        RemoveTypesOf(editorViewModelTypes, assembly);
        RemoveTypesOf(editorViewTypes, assembly);
        RemoveTypesOf(previewViewModelTypes, assembly);
        RemoveTypesOf(previewViewTypes, assembly);

        if (pluginRegistrations.Remove(plugin, out var registrations))
        {
            var properties = session?.AssetViewProperties as SessionObjectPropertiesViewModel;
            var copyPasteService = session?.ServiceProvider.TryGet<ICopyPasteService>();
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
                        properties?.UnregisterNodePresenterCommand(command);
                        break;
                    case INodePresenterUpdater updater:
                        properties?.UnregisterNodePresenterUpdater(updater);
                        break;
                    case TemplateProviderBase provider:
                        GetSelector(provider).UnregisterTemplateProvider(provider);
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

        static void RemoveTypesOf(Dictionary<Type, Type> types, Assembly assembly)
        {
            foreach (var entry in types.Where(x => x.Key.Assembly == assembly || x.Value.Assembly == assembly).ToList())
                types.Remove(entry.Key);
        }
    }

    public bool HasImagesForEnum(ISessionViewModel? session, Type enumType)
    {
        return session != null && enumTypesWithImages.Contains(enumType);
    }

    public object? GetImageForEnum(ISessionViewModel? session, object value)
    {
        if (session == null)
            return null;

        return enumImages.TryGetValue(value, out var image) ? image : null;
    }

    public IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session)
    {
        return primitiveTypes;
    }

    public IEnumerable<PackageSettingsEntry> GetProfileSettings()
    {
        return Plugins.OfType<AssetsEditorPlugin>().SelectMany(x => x.ProfileSettings);
    }

    public bool HasEditorView(ISessionViewModel session, Type viewModelType)
    {
        return editorViewModelTypes.Any(x => x.Key.IsAssignableFrom(viewModelType));
    }

    public Type? GetAssetViewModelType(Type assetType) => TypeHelpers.TryGetTypeOrBase(assetType, assetViewModelTypes);

    public Type? GetEditorViewModelType(Type viewModelType) => TypeHelpers.TryGetTypeOrBase(viewModelType, editorViewModelTypes);

    public Type? GetEditorViewType(Type editorViewModelType) => TypeHelpers.TryGetTypeOrBase(editorViewModelType, editorViewTypes);

    public Type? GetPreviewViewModelType(Type previewType) => TypeHelpers.TryGetTypeOrBase(previewType, previewViewModelTypes);

    public Type? GetPreviewViewType(Type previewType) => TypeHelpers.TryGetTypeOrBase(previewType, previewViewTypes);

    // The property grid selector a provider belongs to, by its template category
    private static TemplateProviderSelector GetSelector(TemplateProviderBase provider)
    {
        return PropertyViewHelper.GetTemplateCategory(provider) switch
        {
            PropertyViewHelper.Category.PropertyHeader => PropertyViewHelper.HeaderProviders,
            PropertyViewHelper.Category.PropertyFooter => PropertyViewHelper.FooterProviders,
            PropertyViewHelper.Category.PropertyEditor => PropertyViewHelper.EditorProviders,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), "Unknown template category"),
        };
    }

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
