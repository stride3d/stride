// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Views;
using Stride.Core.Reflection;
using Stride.Core.Settings;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Registers the plugins with the session, including the plugins of assemblies loaded after it opened, and removes what
/// a plugin registered when its assembly is unloaded. Shared by the editors: each one gives it the few things that depend
/// on its toolkit (where the template providers go, its editor view interface, how images are decoded).
/// </summary>
/// <remarks>
/// Every registration a plugin makes, through its <see cref="IEditorPluginContext"/> and <see cref="ISessionPluginContext"/>
/// or through the attributes of its assembly, is recorded under the plugin; unloading the plugin undoes its records newest first.
/// </remarks>
public abstract partial class EditorPluginHost : IAssetsPluginService, IDisposable
{
    /// <summary>
    /// The module of the global logger the plugins log to once the session is open.
    /// </summary>
    public const string PluginsLoggerModule = "Plugins";

    private readonly IReadOnlyList<AssetsPlugin>? fixedPlugins;
    private readonly IDispatcherService? dispatcher;
    private readonly HashSet<AssetsPlugin> sessionPlugins = [];
    private readonly Dictionary<AssetsPlugin, PluginRecord> records = [];
    // The assemblies whose declared images are registered (once per assembly, under its first plugin)
    private readonly Dictionary<Assembly, PluginRecord> declaredImageAssemblies = [];

    private readonly TypeMap assetViewModelTypes;
    private readonly TypeMap editorViewModelTypes;
    private readonly TypeMap editorViewTypes;
    private readonly TypeMap previewViewModelTypes;
    private readonly TypeMap previewViewTypes;
    private readonly PluginMap<object, object> enumImages;
    private readonly PluginMap<Type, object> typeImages;
    private readonly PluginMap<object, object> declaredEnumImages;
    private readonly PluginMap<Type, object> declaredTypeImages;
    private readonly List<Type> primitiveTypes = [];
    private readonly List<PackageSettingsEntry> profileSettings = [];
    private readonly Dictionary<SettingsKey, int> settingsKeys = [];

    private ISessionViewModel? session;
    private readonly PluginLogger pluginLogger;
    private ILogger? logger;
    private bool followsRegistry;
    private bool sessionInitialized;
    private bool sessionOpened;

    /// <summary>
    /// Creates a host for the plugins of every asset assembly (<see cref="AssetsPlugin.RegisteredPlugins"/>), including the
    /// ones registered later, or for the given plugins only.
    /// </summary>
    /// <param name="plugins">The plugins of the host, or null for every registered plugin.</param>
    /// <param name="dispatcher">The dispatcher of the editor, or null for the one of the session.</param>
    protected EditorPluginHost(IReadOnlyList<AssetsPlugin>? plugins = null, IDispatcherService? dispatcher = null)
    {
        fixedPlugins = plugins;
        this.dispatcher = dispatcher;
        pluginLogger = new PluginLogger(this);

        assetViewModelTypes = new TypeMap(typeof(Asset), typeof(AssetViewModel));
        editorViewModelTypes = new TypeMap(typeof(AssetViewModel), typeof(IAssetEditorViewModel));
        editorViewTypes = new TypeMap(null, null);
        previewViewModelTypes = new TypeMap(null, null);
        previewViewTypes = new TypeMap(null, null);
        enumImages = new PluginMap<object, object>((key, _) => AssertType(typeof(Enum), key.GetType()));
        typeImages = new PluginMap<Type, object>(null);
        declaredEnumImages = new PluginMap<object, object>(null);
        declaredTypeImages = new PluginMap<Type, object>(null);

        if (plugins is null)
        {
            followsRegistry = true;
            AssetsPlugin.PluginsDiscovered += PluginsDiscovered;
            AssetsPlugin.PluginsRemoved += PluginsRemoved;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AssetsPlugin> Plugins => fixedPlugins ?? AssetsPlugin.RegisteredPlugins;

    /// <summary>
    /// The plugins that joined the current session.
    /// </summary>
    public IReadOnlyCollection<AssetsPlugin> SessionPlugins => sessionPlugins;

    /// <summary>
    /// The view model type of each asset type.
    /// </summary>
    public IReadOnlyDictionary<Type, Type> AssetViewModelTypes => assetViewModelTypes.Entries;

    /// <summary>
    /// The editor view model type of each asset view model type.
    /// </summary>
    public IReadOnlyDictionary<Type, Type> AssetEditorViewModelTypes => editorViewModelTypes.Entries;

    /// <summary>
    /// The editor view type of each editor view model type.
    /// </summary>
    public IReadOnlyDictionary<Type, Type> AssetEditorViewTypes => editorViewTypes.Entries;

    /// <summary>
    /// The view model type of each asset preview type.
    /// </summary>
    public IReadOnlyDictionary<Type, Type> AssetPreviewViewModelTypes => previewViewModelTypes.Entries;

    /// <summary>
    /// The view type of each asset preview type.
    /// </summary>
    public IReadOnlyDictionary<Type, Type> AssetPreviewViewTypes => previewViewTypes.Entries;

    /// <summary>
    /// The images of enum values registered by the plugins (the declared images aside).
    /// </summary>
    public IReadOnlyDictionary<object, object> EnumImages => enumImages.Entries;

    /// <summary>
    /// The images of types registered by the plugins (the declared images aside).
    /// </summary>
    public IReadOnlyDictionary<Type, object> TypeImages => typeImages.Entries;

    /// <summary>
    /// The primitive types registered by the plugins, in registration order (a type registered twice is listed twice).
    /// </summary>
    public IReadOnlyList<Type> PrimitiveTypes => primitiveTypes;

    /// <summary>
    /// The settings keys registered by the plugins.
    /// </summary>
    public IReadOnlyCollection<SettingsKey> SettingsKeys => settingsKeys.Keys;

    /// <summary>
    /// The base type of the keys of the editor views: the editor view model type of the editor's toolkit.
    /// </summary>
    protected abstract Type AssetEditorViewModelType { get; }

    /// <summary>
    /// The interface of the editor views of the editor's toolkit; the editor views of the plugin assemblies are found by it.
    /// </summary>
    protected abstract Type AssetEditorViewType { get; }

    /// <summary>
    /// The base type of the asset previews, or null to accept any type.
    /// </summary>
    protected virtual Type? AssetPreviewType => null;

    /// <summary>
    /// The base type of the preview view models, or null to accept any type.
    /// </summary>
    protected virtual Type? AssetPreviewViewModelType => null;

    /// <summary>
    /// The base type of the preview views, or null to accept any type.
    /// </summary>
    protected virtual Type? AssetPreviewViewType => null;

    /// <summary>
    /// Gives a template provider of a plugin to the editor.
    /// </summary>
    /// <returns>True when the editor took the provider; false when it is not for this editor's toolkit.</returns>
    protected abstract bool AddTemplateProvider(ISessionViewModel session, ITemplateProvider provider);

    /// <summary>
    /// Takes back a template provider that <see cref="AddTemplateProvider"/> took.
    /// </summary>
    protected abstract void RemoveTemplateProvider(ISessionViewModel session, ITemplateProvider provider);

    /// <summary>
    /// Decodes an image that a plugin assembly declares (<see cref="TypeImageAttribute"/>, <see cref="EnumImageAttribute"/>)
    /// into an image of the editor's toolkit, usable from any thread.
    /// </summary>
    /// <returns>The image, or null when the editor shows no declared images.</returns>
    protected virtual object? DecodeImage(byte[] data) => null;

    /// <summary>
    /// Whether the editor of <paramref name="session"/> is initialized: a plugin that joins after that is initialized for
    /// the session right away. By default, whether <see cref="InitializeSession"/> ran.
    /// </summary>
    protected virtual bool IsEditorInitialized(ISessionViewModel session) => sessionInitialized;

    /// <summary>
    /// The property grid service of the session, where the plugins' node presenter commands and updaters go.
    /// </summary>
    protected virtual GraphViewModelService? GetGraphViewModelService(ISessionViewModel session)
    {
        return (session.AssetViewProperties as ViewModelBase)?.ServiceProvider.TryGet<GraphViewModelService>();
    }

    /// <summary>
    /// Registers what the plugin's assembly declares with attributes, in the plugin's name.
    /// </summary>
    protected virtual void ScanPlugin(AssetsPlugin plugin, IEditorPluginContext context)
    {
        var assembly = plugin.GetType().Assembly;

        foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(AssetViewModel)))
        {
            if (typeof(AssetViewModel).IsAssignableFrom(type) && type.GetCustomAttribute<AssetViewModelAttribute>() is { } attribute)
                context.AssetViewModelTypes.Register(attribute.AssetType, type);
        }

        // Registered again when an undo brings back an assembly whose keys were removed
        foreach (var settingsKey in EditorSettingsAttribute.GetDeclaredKeys(assembly))
            context.SettingsKeys.Register(settingsKey);

        if (plugin is not AssetsEditorPlugin)
            return;

        foreach (var type in AssemblyRegistry.GetScanTypes(assembly, typeof(IAssetEditorViewModel)))
        {
            if (typeof(IAssetEditorViewModel).IsAssignableFrom(type) && type.GetCustomAttribute<AssetEditorViewModelAttribute>() is { } attribute)
                context.AssetEditorViewModelTypes.Register(attribute.ViewModelType, type);
        }

        foreach (var type in AssemblyRegistry.GetScanTypes(assembly, AssetEditorViewType))
        {
            if (AssetEditorViewType.IsAssignableFrom(type) && type.GetCustomAttribute<AssetEditorViewAttribute>() is { } attribute)
                context.AssetEditorViewTypes.Register(attribute.EditorViewModelType, type);
        }
    }

    /// <inheritdoc />
    public void RegisterSession(ISessionViewModel session, ILogger logger)
    {
        this.session = session;
        this.logger = logger;
        sessionOpened = false;

        editorViewTypes.SetBaseTypes(AssetEditorViewModelType, AssetEditorViewType);
        previewViewModelTypes.SetBaseTypes(AssetPreviewType, AssetPreviewViewModelType);
        previewViewTypes.SetBaseTypes(AssetPreviewType, AssetPreviewViewType);

        // The plugins of the asset assemblies loaded so far, and of each one registered later
        if (followsRegistry)
            AssetsPlugin.DiscoverPlugins();
        foreach (var plugin in Plugins.ToList())
            TryJoin(plugin, session);
    }

    /// <inheritdoc />
    public void InitializeSession(ISessionViewModel session)
    {
        sessionInitialized = true;
        foreach (var plugin in Plugins.ToList())
            TryInitializeSession(plugin, session);
        sessionOpened = true;
    }

    /// <summary>
    /// Stops following the plugin registry and removes what the plugins registered, as when their assemblies are unloaded:
    /// the editor closes the session, and the plugins join the next editor's host afresh.
    /// </summary>
    public void Dispose()
    {
        if (followsRegistry)
        {
            followsRegistry = false;
            AssetsPlugin.PluginsDiscovered -= PluginsDiscovered;
            AssetsPlugin.PluginsRemoved -= PluginsRemoved;
        }
        session = null;
        foreach (var plugin in sessionPlugins.Concat(records.Keys).Distinct().ToList())
            TryLeave(plugin);
        GC.SuppressFinalize(this);
    }

    internal void PluginsDiscovered(IReadOnlyList<AssetsPlugin> plugins)
    {
        if (session is null)
            return;

        // A plugin arriving after the session opened joins it right away: inline on the UI thread, queued there from
        // another one (this runs in an assembly's registration, under the loader's lock: waiting for the UI thread,
        // which may be waiting for that lock, could deadlock)
        var currentSession = session;
        var currentDispatcher = dispatcher ?? currentSession.Dispatcher;
        if (currentDispatcher.CheckAccess())
            JoinSession();
        else
            _ = currentDispatcher.InvokeAsync(JoinSession);

        void JoinSession()
        {
            // The editor was disposed in the meantime
            if (session != currentSession)
                return;

            foreach (var plugin in plugins)
            {
                if (TryJoin(plugin, currentSession) && IsEditorInitialized(currentSession))
                    TryInitializeSession(plugin, currentSession);
            }
        }
    }

    internal void PluginsRemoved(IReadOnlyList<AssetsPlugin> plugins)
    {
        if (session is null)
            return;

        // Inline on the UI thread, queued there from another one, as for a plugin joining (see PluginsDiscovered)
        var currentSession = session;
        var currentDispatcher = dispatcher ?? currentSession.Dispatcher;
        if (currentDispatcher.CheckAccess())
            LeaveSession();
        else
            _ = currentDispatcher.InvokeAsync(LeaveSession);

        void LeaveSession()
        {
            foreach (var plugin in plugins)
                TryLeave(plugin);
        }
    }

    // A plugin that fails to join the session is reported and left out, without what it registered before failing; the
    // other plugins still join
    private bool TryJoin(AssetsPlugin plugin, ISessionViewModel session)
    {
        if (!sessionPlugins.Add(plugin))
            return false;

        var record = GetRecord(plugin);
        var undoCount = record.Count;
        try
        {
            var context = new EditorContext(this, plugin, record, session, pluginLogger);
            plugin.InitializePlugin(pluginLogger);
            ScanPlugin(plugin, context);
            ScanDeclaredImages(plugin, record);
            plugin.Initialize(context);
            return true;
        }
        catch (Exception e)
        {
            sessionPlugins.Remove(plugin);
            record.UndoFrom(undoCount, pluginLogger);
            pluginLogger.Error($"The plugin [{plugin.GetType().FullName}] could not join the session.", e);
            return false;
        }
    }

    // A plugin that fails to initialize the session is reported; the editor goes on without what it did not register
    private void TryInitializeSession(AssetsPlugin plugin, ISessionViewModel session)
    {
        var record = GetRecord(plugin);
        try
        {
            // The commands and updaters a plugin adds directly to the property grid are recorded too, by comparing the
            // grid before and after its initialization
            var graphService = GetGraphViewModelService(session);
            var commands = graphService?.AvailableCommands.ToList() ?? [];
            var updaters = graphService?.AvailableUpdaters.ToList() ?? [];
            var context = new SessionContext(this, plugin, record, session, pluginLogger);
            try
            {
                plugin.InitializeSession(context);
            }
            finally
            {
                if (graphService is not null)
                {
                    foreach (var command in graphService.AvailableCommands.Except(commands).Except(context.RegisteredCommands).ToList())
                        record.Add(() => graphService.AvailableCommands.Remove(command));
                    foreach (var updater in graphService.AvailableUpdaters.Except(updaters).Except(context.RegisteredUpdaters).ToList())
                        record.Add(() => graphService.AvailableUpdaters.Remove(updater));
                }
            }
        }
        catch (Exception e)
        {
            pluginLogger.Error($"The plugin [{plugin.GetType().FullName}] could not initialize the session.", e);
        }
    }

    // A plugin that fails to leave is reported; the other plugins still leave
    private void TryLeave(AssetsPlugin plugin)
    {
        try
        {
            Leave(plugin);
        }
        catch (Exception e)
        {
            pluginLogger.Error($"The plugin [{plugin.GetType().FullName}] could not leave the session.", e);
        }
    }

    /// <summary>
    /// Removes what a plugin registered, newest first.
    /// </summary>
    private void Leave(AssetsPlugin plugin)
    {
        var joined = sessionPlugins.Remove(plugin);
        if (records.Remove(plugin, out var record))
        {
            record.UndoFrom(0, pluginLogger);
            foreach (var assembly in declaredImageAssemblies.Where(x => x.Value == record).Select(x => x.Key).ToList())
                declaredImageAssemblies.Remove(assembly);
        }
        else if (!joined)
        {
            return;
        }

        // The types of the unloaded assembly go, whichever plugin registered them
        var unloaded = plugin.GetType().Assembly;
        assetViewModelTypes.RemoveTypesOf(unloaded);
        editorViewModelTypes.RemoveTypesOf(unloaded);
        editorViewTypes.RemoveTypesOf(unloaded);
        previewViewModelTypes.RemoveTypesOf(unloaded);
        previewViewTypes.RemoveTypesOf(unloaded);
    }

    private PluginRecord GetRecord(AssetsPlugin plugin)
    {
        if (!records.TryGetValue(plugin, out var record))
            records[plugin] = record = new PluginRecord();
        return record;
    }

    // The images the plugin's assembly declares, once per assembly; they come after the ones the plugins register
    private void ScanDeclaredImages(AssetsPlugin plugin, PluginRecord record)
    {
        var assembly = plugin.GetType().Assembly;
        if (declaredImageAssemblies.ContainsKey(assembly))
            return;

        declaredImageAssemblies[assembly] = record;
        record.Add(() => declaredImageAssemblies.Remove(assembly));
        foreach (var declared in DeclaredImageResources.Read(assembly))
        {
            if (DecodeImage(declared.Data) is not { } image)
                continue;

            switch (declared.Target)
            {
                case Type type:
                    record.Add(declaredTypeImages.Set(type, image));
                    break;
                case Enum value:
                    record.Add(declaredEnumImages.Set(value, image));
                    break;
            }
        }
    }

    /// <inheritdoc />
    public bool HasImagesForEnum(ISessionViewModel? session, Type enumType)
    {
        return session != null && (enumImages.Entries.Keys.Any(x => x.GetType() == enumType) || declaredEnumImages.Entries.Keys.Any(x => x.GetType() == enumType));
    }

    /// <inheritdoc />
    public object? GetImageForEnum(ISessionViewModel? session, object value)
    {
        if (session == null)
            return null;

        // The plugins' own images come before the declared ones
        return enumImages.Entries.TryGetValue(value, out var image) ? image : declaredEnumImages.Entries.GetValueOrDefault(value);
    }

    /// <inheritdoc />
    public object? GetImageForType(Type type)
    {
        return typeImages.Entries.TryGetValue(type, out var image) ? image : declaredTypeImages.Entries.GetValueOrDefault(type);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session) => primitiveTypes;

    /// <inheritdoc />
    public IEnumerable<PackageSettingsEntry> GetProfileSettings()
    {
        return Plugins.OfType<AssetsEditorPlugin>().SelectMany(x => x.ProfileSettings).Concat(profileSettings);
    }

    /// <inheritdoc />
    public virtual bool HasEditorView(ISessionViewModel session, Type viewModelType)
    {
        return editorViewModelTypes.Entries.Any(x => x.Key.IsAssignableFrom(viewModelType));
    }

    /// <inheritdoc />
    public Type? GetAssetViewModelType(Type assetType) => TryGetTypeOrBase(assetType, assetViewModelTypes.Entries);

    /// <inheritdoc />
    public Type? GetEditorViewModelType(Type viewModelType) => TryGetTypeOrBase(viewModelType, editorViewModelTypes.Entries);

    /// <inheritdoc />
    public Type? GetEditorViewType(Type editorViewModelType) => TryGetTypeOrBase(editorViewModelType, editorViewTypes.Entries);

    /// <inheritdoc />
    public Type? GetPreviewViewModelType(Type previewType) => TryGetTypeOrBase(previewType, previewViewModelTypes.Entries);

    /// <inheritdoc />
    public Type? GetPreviewViewType(Type previewType) => TryGetTypeOrBase(previewType, previewViewTypes.Entries);

    /// <summary>
    /// The type registered for <paramref name="keyType"/>, for one of its base types, or for the generic definition of either.
    /// </summary>
    private static Type? TryGetTypeOrBase(Type keyType, IReadOnlyDictionary<Type, Type> typeMap)
    {
        for (var currentType = keyType; currentType != null; currentType = currentType.BaseType)
        {
            if (typeMap.TryGetValue(currentType, out var returnType))
                return returnType;

            // A registration on an open generic base (PreviewFromSpriteBatch<>) covers every closed one
            if (currentType.IsConstructedGenericType && typeMap.TryGetValue(currentType.GetGenericTypeDefinition(), out returnType))
                return returnType;
        }
        return null;
    }

    private static void AssertType(Type baseType, Type specificType)
    {
        // IsAssignableFrom is false for an open generic type, so check its interfaces too
        if (!baseType.IsAssignableFrom(specificType) && !specificType.GetInterfaces().Contains(baseType))
            throw new ArgumentException($"Type [{specificType.FullName}] must be assignable to {baseType.FullName}", nameof(specificType));
    }

    private void RegisterSettingsKey(PluginRecord record, SettingsKey settingsKey)
    {
        settingsKeys.TryGetValue(settingsKey, out var count);
        settingsKeys[settingsKey] = count + 1;
        if (count == 0)
            settingsKey.Container.RegisterSettingsKey(settingsKey);
        record.Add(() =>
        {
            if (!settingsKeys.TryGetValue(settingsKey, out var current))
                return;
            if (current > 1)
            {
                settingsKeys[settingsKey] = current - 1;
                return;
            }
            settingsKeys.Remove(settingsKey);
            settingsKey.Container.UnregisterSettingsKey(settingsKey);
        });
    }

    private static void RegisterInList<T>(PluginRecord record, List<T> list, T item)
    {
        list.Add(item);
        record.Add(() => list.Remove(item));
    }
}
