// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Views;
using Stride.Core.Reflection;
using Stride.Core.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;

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
public abstract partial class EditorPluginHost : IAssetsPluginService, IPluginContributionGuard, IDisposable
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
    // The registered assemblies, each with the record of what the host registers for it: its primitive types, and its
    // declared images when no plugin of the assembly registered them (the runtime and Assets assemblies of the packages)
    private readonly Dictionary<Assembly, PluginRecord> assemblyRecords = [];

    private readonly TypeMap assetViewModelTypes;
    private readonly TypeMap editorViewModelTypes;
    private readonly TypeMap editorViewTypes;
    private readonly TypeMap previewViewModelTypes;
    private readonly TypeMap previewViewTypes;
    private readonly TypeMap viewTypes;
    // The plugin that first registered each key of the view map, whose guard runs the view
    private readonly Dictionary<Type, AssetsPlugin> viewTypeOwners = [];
    // The registries that the layers of the editor declare, by type of contribution
    private readonly Dictionary<Type, IDeclaredRegistry> declaredRegistries = [];
    // The state and the errors of each plugin that joined or failed to join; read from any thread
    private readonly object statusLock = new();
    private readonly Dictionary<AssetsPlugin, StatusEntry> statuses = [];
    // The plugins whose assembly was not loaded (incompatible), by assembly path, and whose package was not loaded (off), by
    // package name
    private readonly Dictionary<string, (string Name, StatusEntry Entry)> unloadedStatuses = new(StringComparer.OrdinalIgnoreCase);
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
    private bool followsAssemblies;
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
        viewTypes = new TypeMap(null, null);
        enumImages = new PluginMap<object, object>((key, _) => AssertType(typeof(Enum), key.GetType()));
        typeImages = new PluginMap<Type, object>(null);
        declaredEnumImages = new PluginMap<object, object>(null);
        declaredTypeImages = new PluginMap<Type, object>(null);
        AssemblyLoadCheck = new EditorApiCheck(RecordIncompatible);
        Commands = new ContributionRegistry<CommandDescriptor>(ValidateCommand);
        ToolbarWidgets = new ContributionRegistry<ToolbarWidgetDescriptor>(ValidateToolbarWidget);

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
    /// The logger of the plugins (module <see cref="PluginsLoggerModule"/>), for the layers of the editor that refuse or
    /// report a contribution.
    /// </summary>
    protected ILogger PluginsLogger => pluginLogger;

    /// <summary>
    /// The primitive types registered by the plugins, in registration order (a type registered twice is listed twice).
    /// </summary>
    public IReadOnlyList<Type> PrimitiveTypes => primitiveTypes;

    /// <summary>
    /// The settings keys registered by the plugins.
    /// </summary>
    public IReadOnlyCollection<SettingsKey> SettingsKeys => settingsKeys.Keys;

    /// <summary>
    /// The panels of the main window: the editor's shell registers its own, and a plugin registers its panels with itself as
    /// their owner, so that they go when it leaves.
    /// </summary>
    public PanelRegistry Panels { get; } = new();

    /// <summary>
    /// The view type of each view model type registered by the plugins (<see cref="IEditorPluginContext.ViewTypes"/>).
    /// </summary>
    [Experimental("STREXP001")]
    public IReadOnlyDictionary<Type, Type> ViewTypes => viewTypes.Entries;

    /// <summary>
    /// The state and the errors of each plugin that joined the session or failed to join it, in the order they joined, then
    /// of each plugin whose assembly or package was not loaded (<see cref="PluginState.Incompatible"/>, <see cref="PluginState.Off"/>),
    /// in the order they were left out.
    /// </summary>
    [Experimental("STREXP002")]
    public IReadOnlyList<PluginStatus> PluginStatuses
    {
        get
        {
            lock (statusLock)
                return statuses.Select(x => x.Value.Snapshot(x.Key)).Concat(unloadedStatuses.Values.Select(x => x.Entry.Snapshot(x.Name))).ToList();
        }
    }

    /// <summary>
    /// The check that each assembly of the session's packages passes before it loads (<see cref="EditorApiCheck"/>): a plugin
    /// built for a newer editor is not loaded, and gets the <see cref="PluginState.Incompatible"/> state here. The editor gives
    /// it to the session it opens.
    /// </summary>
    [Experimental("STREXP002")]
    public IAssemblyLoadCheck AssemblyLoadCheck { get; }

    /// <summary>
    /// Raised when a plugin joins, fails to join or leaves the session, and when one of its contributions fails. Raised on
    /// the thread where it happened, the editor's thread except for a contribution run on another one.
    /// </summary>
    [Experimental("STREXP002")]
    public event EventHandler<PluginStatusChangedEventArgs>? PluginStatusChanged;

    /// <summary>
    /// The base type of the views of the editor's toolkit (<see cref="IEditorPluginContext.ViewTypes"/>), or null to accept any type.
    /// </summary>
    [Experimental("STREXP001")]
    protected virtual Type? ViewBaseType => null;

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
    /// Whether the editor shows the images that every registered assembly declares, as long as it is registered, and not
    /// only the ones of the plugin assemblies: the runtime and Assets assemblies of a package declare the images of their
    /// types too. By default, when the host follows the plugin registry.
    /// </summary>
    protected virtual bool ShowsImagesOfRegisteredAssemblies => fixedPlugins is null;

    /// <summary>
    /// The types of a registered assembly that the property grid shows as primitive types (not expanded), whatever plugin
    /// is loaded. Read once per assembly, while it is registered.
    /// </summary>
    protected virtual IEnumerable<Type> FindPrimitiveTypes(Assembly assembly) => [];

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
    /// Registers what the plugin's assemblies (<see cref="AssetsPlugin.ScannedAssemblies"/>) declare with attributes, in the
    /// plugin's name.
    /// </summary>
    protected virtual void ScanPlugin(AssetsPlugin plugin, IEditorPluginContext context)
    {
        foreach (var assembly in plugin.ScannedAssemblies)
            ScanPluginAssembly(plugin, assembly, context);
    }

    private void ScanPluginAssembly(AssetsPlugin plugin, Assembly assembly, IEditorPluginContext context)
    {
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
        viewTypes.SetBaseTypes(null, ViewBaseType);

        // The plugins of the asset assemblies loaded so far, and of each one registered later
        if (followsRegistry)
            AssetsPlugin.DiscoverPlugins();
        foreach (var plugin in Plugins.ToList())
            TryJoin(plugin, session);

        // What the registered assemblies declare by themselves, as long as they are registered
        if (!followsAssemblies)
        {
            followsAssemblies = true;
            AssemblyRegistry.AssemblyRegistered += AssemblyRegistered;
            AssemblyRegistry.AssemblyUnregistered += AssemblyUnregistered;
            foreach (var assembly in AssemblyRegistry.FindAll())
                ScanAssembly(assembly);
        }
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
        if (followsAssemblies)
        {
            followsAssemblies = false;
            AssemblyRegistry.AssemblyRegistered -= AssemblyRegistered;
            AssemblyRegistry.AssemblyUnregistered -= AssemblyUnregistered;
        }
        session = null;
        foreach (var plugin in sessionPlugins.Concat(records.Keys).Distinct().ToList())
            TryLeave(plugin);
        foreach (var assembly in assemblyRecords.Keys.ToList())
            RemoveAssembly(assembly);
        ClearUnloadedStatuses();
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

    private void AssemblyRegistered(object? sender, AssemblyRegisteredEventArgs e) => OnEditorThread(() => ScanAssembly(e.Assembly));

    private void AssemblyUnregistered(object? sender, AssemblyRegisteredEventArgs e) => OnEditorThread(() => RemoveAssembly(e.Assembly));

    // Inline on the UI thread, queued there from another one, as for a plugin joining (see PluginsDiscovered)
    private void OnEditorThread(Action action)
    {
        if (session is null)
            return;

        var currentSession = session;
        var currentDispatcher = dispatcher ?? currentSession.Dispatcher;
        if (currentDispatcher.CheckAccess())
            action();
        else
            _ = currentDispatcher.InvokeAsync(() =>
            {
                // The editor was disposed in the meantime
                if (session == currentSession)
                    action();
            });
    }

    // A plugin that fails to join the session is reported and left out, without what it registered before failing; the
    // other plugins still join
    private bool TryJoin(AssetsPlugin plugin, ISessionViewModel session)
    {
        if (!sessionPlugins.Add(plugin))
            return false;

        var record = GetRecord(plugin);
        var undoCount = record.Count;
        // Its status exists while it joins, for the contributions that fail meanwhile
        lock (statusLock)
            statuses[plugin] = new StatusEntry();
        try
        {
            var context = new EditorContext(this, plugin, record, session, pluginLogger);
            // The older API, still called for the plugins that use it
#pragma warning disable STRPLUGIN002
            plugin.InitializePlugin(pluginLogger);
#pragma warning restore STRPLUGIN002
            ScanPlugin(plugin, context);
            foreach (var assembly in plugin.ScannedAssemblies)
                ScanDeclaredImages(assembly, record);
            plugin.Initialize(context);
            UpdateStatus(plugin, _ => { });
            return true;
        }
        catch (Exception e)
        {
            sessionPlugins.Remove(plugin);
            record.UndoFrom(undoCount, pluginLogger);
            pluginLogger.Error($"The plugin [{plugin.GetType().FullName}] could not join the session.", e);
            UpdateStatus(plugin, x =>
            {
                x.State = PluginState.Failed;
                x.Errors.Add(new PluginError(PluginContributions.Join, e));
            });
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
            // It stays: what it registered while joining still works
            UpdateStatus(plugin, x => x.Errors.Add(new PluginError(PluginContributions.InitializeSession, e)));
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
        // Its panels and its contributions to the declared registries go, whoever registered them for it
        Panels.UnregisterOwner(plugin);
        Commands.UnregisterOwner(plugin);
        ToolbarWidgets.UnregisterOwner(plugin);
        foreach (var registry in declaredRegistries.Values)
            registry.UnregisterOwner(plugin);
        RemoveStatus(plugin);
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
        viewTypes.RemoveTypesOf(unloaded);
        foreach (var key in viewTypeOwners.Where(x => !viewTypes.Entries.ContainsKey(x.Key)).Select(x => x.Key).ToList())
            viewTypeOwners.Remove(key);
    }

    private PluginRecord GetRecord(AssetsPlugin plugin)
    {
        if (!records.TryGetValue(plugin, out var record))
            records[plugin] = record = new PluginRecord();
        return record;
    }

    // What a registered assembly declares by itself: its primitive types, and its images unless a plugin of the assembly
    // registered them
    private void ScanAssembly(Assembly assembly)
    {
        if (assemblyRecords.ContainsKey(assembly))
            return;

        var record = new PluginRecord();
        assemblyRecords[assembly] = record;
        try
        {
            foreach (var type in FindPrimitiveTypes(assembly))
                RegisterInList(record, primitiveTypes, type);
        }
        catch (Exception e)
        {
            pluginLogger.Warning($"Could not read the primitive types of [{assembly.GetName().Name}].", e);
        }

        if (ShowsImagesOfRegisteredAssemblies && !declaredImageAssemblies.ContainsKey(assembly))
            ScanDeclaredImages(assembly, record);
    }

    private void RemoveAssembly(Assembly assembly)
    {
        if (assemblyRecords.Remove(assembly, out var record))
            record.UndoFrom(0, pluginLogger);
    }

    // The images the assembly declares, once per assembly; they come after the ones the plugins register
    private void ScanDeclaredImages(Assembly assembly, PluginRecord record)
    {
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
        // The older API's list, still read for the plugins that use it
#pragma warning disable STRPLUGIN005
        return Plugins.OfType<AssetsEditorPlugin>().SelectMany(x => x.ProfileSettings).Concat(profileSettings);
#pragma warning restore STRPLUGIN005
    }

    /// <inheritdoc />
    /// <remarks>
    /// An editor counts only when this editor has its view: a plugin may declare an editor whose view exists in another
    /// editor only (e.g. a toolkit-neutral plugin with the views of one toolkit).
    /// </remarks>
    public virtual bool HasEditorView(ISessionViewModel session, Type viewModelType)
    {
        return GetEditorViewModelType(viewModelType) is { } editorViewModelType && GetEditorViewType(editorViewModelType) is not null;
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

    /// <inheritdoc />
    [Experimental("STREXP001")]
    public Type? GetViewType(Type viewModelType) => GetViewType(viewModelType, out _);

    /// <summary>
    /// Gets the view type that the plugins register for <paramref name="viewModelType"/> or one of its base types, and the
    /// plugin that registered it: the owner to give the guard when the view is created.
    /// </summary>
    [Experimental("STREXP001")]
    public Type? GetViewType(Type viewModelType, out AssetsPlugin? owner)
    {
        owner = null;
        for (var currentType = viewModelType; currentType != null; currentType = currentType.BaseType)
        {
            // A registration on an open generic base covers every closed one, as for the other maps
            var key = viewTypes.Entries.ContainsKey(currentType) ? currentType
                : currentType.IsConstructedGenericType ? currentType.GetGenericTypeDefinition() : null;
            if (key is not null && viewTypes.Entries.TryGetValue(key, out var viewType))
            {
                owner = viewTypeOwners.GetValueOrDefault(key);
                return viewType;
            }
        }
        return null;
    }

    /// <summary>
    /// Declares a registry that the plugins can register into with <see cref="IEditorPluginContext.GetRegistry{T}"/>: a layer
    /// of the editor declares the registries of its own contribution points, which the context, in a Core assembly, does not
    /// name. Declare it before the session opens, so that every plugin finds it when it joins.
    /// </summary>
    /// <typeparam name="T">The type of the contributions.</typeparam>
    /// <param name="validate">Checks a contribution before it is registered, and throws to refuse it; or null.</param>
    /// <returns>The registry, which the declaring layer reads and follows.</returns>
    /// <exception cref="InvalidOperationException">A registry of <typeparamref name="T"/> is already declared.</exception>
    [Experimental("STREXP001")]
    public ContributionRegistry<T> DeclareRegistry<T>(Action<T>? validate = null)
        where T : class
    {
        if (declaredRegistries.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"A registry of [{typeof(T).FullName}] is already declared.");

        var registry = new ContributionRegistry<T>(validate);
        declaredRegistries[typeof(T)] = new DeclaredRegistry<T>(registry);
        return registry;
    }

    /// <summary>
    /// The registry of <typeparamref name="T"/> that a layer of the editor declared (<see cref="DeclareRegistry{T}"/>), or null.
    /// </summary>
    [Experimental("STREXP001")]
    public ContributionRegistry<T>? GetDeclaredRegistry<T>()
        where T : class
    {
        return (declaredRegistries.GetValueOrDefault(typeof(T)) as DeclaredRegistry<T>)?.Registry;
    }

    /// <summary>
    /// The state and the errors of a plugin, or null when it has not joined the session (or has left it).
    /// </summary>
    [Experimental("STREXP002")]
    public PluginStatus? GetPluginStatus(AssetsPlugin plugin)
    {
        lock (statusLock)
            return statuses.TryGetValue(plugin, out var entry) ? entry.Snapshot(plugin) : null;
    }

    /// <inheritdoc />
    [Experimental("STREXP002")]
    public bool IsTurnedOff(object? owner, string contribution)
    {
        if (owner is not AssetsPlugin plugin)
            return false;
        lock (statusLock)
            return statuses.TryGetValue(plugin, out var entry) && entry.TurnedOff.Contains(contribution);
    }

    /// <inheritdoc />
    [Experimental("STREXP002")]
    public bool TryRun(object? owner, string contribution, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return TryRun(owner, contribution, () =>
        {
            action();
            return true;
        }, out _);
    }

    /// <inheritdoc />
    [Experimental("STREXP002")]
    public bool TryRun<T>(object? owner, string contribution, Func<T> function, [MaybeNullWhen(false)] out T result)
    {
        ArgumentNullException.ThrowIfNull(contribution);
        ArgumentNullException.ThrowIfNull(function);
        result = default;
        if (IsTurnedOff(owner, contribution))
            return false;

        try
        {
            result = function();
            return true;
        }
        catch (Exception e)
        {
            result = default;
            ReportFailure(owner, contribution, e);
            return false;
        }
    }

    // A contribution of a plugin that has a status is turned off and recorded; the code of the editor is only logged
    private void ReportFailure(object? owner, string contribution, Exception exception)
    {
        if (owner is AssetsPlugin plugin)
        {
            bool known;
            lock (statusLock)
                known = statuses.ContainsKey(plugin);
            if (known)
            {
                pluginLogger.Error($"[{contribution}] of the plugin [{plugin.GetType().FullName}] failed: it is turned off for this session.", exception);
                UpdateStatus(plugin, x =>
                {
                    x.TurnedOff.Add(contribution);
                    x.Errors.Add(new PluginError(contribution, exception));
                });
                return;
            }
            pluginLogger.Error($"[{contribution}] of the plugin [{plugin.GetType().FullName}], which is not in the session, failed.", exception);
            return;
        }
        pluginLogger.Error($"[{contribution}] of the editor failed.", exception);
    }

    private void UpdateStatus(AssetsPlugin plugin, Action<StatusEntry> update)
    {
        PluginStatus status;
        lock (statusLock)
        {
            if (!statuses.TryGetValue(plugin, out var entry))
                statuses[plugin] = entry = new StatusEntry();
            update(entry);
            status = entry.Snapshot(plugin);
        }
        PluginStatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(plugin, status.Name, status));
    }

    private void RemoveStatus(AssetsPlugin plugin)
    {
        bool removed;
        lock (statusLock)
            removed = statuses.Remove(plugin);
        if (removed)
            PluginStatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(plugin, plugin.GetType().FullName ?? plugin.GetType().Name, null));
    }

    // A plugin built for a newer editor: its assembly is not loaded, so it is known by its assembly only. Called on the
    // thread that loads the session, before the plugins join it.
    private void RecordIncompatible(IncompatiblePluginException error)
    {
        PluginStatus status;
        lock (statusLock)
        {
            var entry = new StatusEntry { State = PluginState.Incompatible };
            entry.Errors.Add(new PluginError(PluginContributions.Load, error));
            unloadedStatuses[error.AssemblyPath] = (error.AssemblyName, entry);
            status = entry.Snapshot(error.AssemblyName);
        }
        PluginStatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(null, status.Name, status));
    }

    private void ClearUnloadedStatuses()
    {
        List<string> names;
        lock (statusLock)
        {
            names = unloadedStatuses.Values.Select(x => x.Name).ToList();
            unloadedStatuses.Clear();
        }
        foreach (var name in names)
            PluginStatusChanged?.Invoke(this, new PluginStatusChangedEventArgs(null, name, null));
    }

    private void RegisterViewType(PluginRecord record, AssetsPlugin plugin, Type viewModelType, Type viewType)
    {
        record.Add(viewTypes.Register(viewModelType, viewType));
        if (viewTypeOwners.TryAdd(viewModelType, plugin))
        {
            record.Add(() =>
            {
                if (viewTypeOwners.TryGetValue(viewModelType, out var owner) && owner == plugin)
                    viewTypeOwners.Remove(viewModelType);
            });
        }
    }

    private IPluginRegistry<T> GetRegistry<T>(PluginRecord record, AssetsPlugin plugin)
        where T : class
    {
        var registry = GetDeclaredRegistry<T>()
            ?? throw new InvalidOperationException($"No registry of [{typeof(T).FullName}] is declared in this editor.");
        return new Registry<T>(item => record.Add(registry.Register(item, plugin).Dispose));
    }

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

    /// <summary>
    /// Registers a tool of the scene and prefab views that a plugin gives through the obsolete
    /// <see cref="IEditorPluginContext.EditorViewToolTypes"/>. The layer of the editor that has these views overrides it to
    /// put the tool in its typed registry; this host has no such view, so the tool is logged and left out.
    /// </summary>
    /// <param name="plugin">The plugin that registers the tool.</param>
    /// <param name="type">The type of the tool.</param>
    /// <returns>What unregisters the tool when the plugin leaves, or null when the tool is left out.</returns>
    [Experimental("STREXP004")]
    protected virtual IDisposable? RegisterEditorViewToolType(AssetsPlugin plugin, Type type)
    {
        pluginLogger.Warning($"The tool [{type.FullName}] of the plugin [{plugin.GetType().FullName}] is left out: this editor has no scene view.");
        return null;
    }

    private static void RegisterInList<T>(PluginRecord record, List<T> list, T item)
    {
        list.Add(item);
        record.Add(() => list.Remove(item));
    }
}
