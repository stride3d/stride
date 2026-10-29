// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Views;
using Stride.Core.Settings;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract partial class EditorPluginHost
{
    /// <summary>
    /// What a plugin registered: one action per registration, which undoes it.
    /// </summary>
    private sealed class PluginRecord
    {
        private readonly List<Action> undoActions = [];

        public int Count => undoActions.Count;

        public void Add(Action undo) => undoActions.Add(undo);

        /// <summary>
        /// Undoes the registrations from <paramref name="index"/> on, newest first; one that fails is reported and the others still run.
        /// </summary>
        public void UndoFrom(int index, ILogger? logger)
        {
            for (var i = undoActions.Count - 1; i >= index; --i)
            {
                var undo = undoActions[i];
                undoActions.RemoveAt(i);
                try
                {
                    undo();
                }
                catch (Exception e)
                {
                    logger?.Error("A registration of a plugin could not be removed.", e);
                }
            }
        }
    }

    /// <summary>
    /// A map whose entries several plugins can register with the same value; an entry goes with its last registration.
    /// </summary>
    private class PluginMap<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> values = [];
        private readonly Dictionary<TKey, int> counts = [];
        private readonly Action<TKey, TValue>? validate;

        public PluginMap(Action<TKey, TValue>? validate)
        {
            this.validate = validate;
        }

        public IReadOnlyDictionary<TKey, TValue> Entries => values;

        /// <summary>
        /// Adds an entry, or a registration of an existing one with the same value.
        /// </summary>
        /// <returns>The action that undoes the registration.</returns>
        public Action Register(TKey key, TValue value)
        {
            validate?.Invoke(key, value);
            if (values.TryGetValue(key, out var existing))
            {
                if (!EqualityComparer<TValue>.Default.Equals(existing, value))
                    throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
                counts[key]++;
            }
            else
            {
                values.Add(key, value);
                counts[key] = 1;
            }
            return () => Unregister(key, value);
        }

        /// <summary>
        /// Sets an entry, replacing the one the key has.
        /// </summary>
        /// <returns>The action that removes the entry, if it was not replaced since.</returns>
        public Action Set(TKey key, TValue value)
        {
            values[key] = value;
            counts[key] = 1;
            return () => Unregister(key, value);
        }

        public void Remove(TKey key)
        {
            values.Remove(key);
            counts.Remove(key);
        }

        private void Unregister(TKey key, TValue value)
        {
            // Removed or replaced in the meantime
            if (!values.TryGetValue(key, out var existing) || !EqualityComparer<TValue>.Default.Equals(existing, value))
                return;

            if (--counts[key] == 0)
                Remove(key);
        }
    }

    /// <summary>
    /// A map of types, whose keys and values are checked against base types.
    /// </summary>
    private sealed class TypeMap : PluginMap<Type, Type>
    {
        private Type? keyBaseType;
        private Type? valueBaseType;

        public TypeMap(Type? keyBaseType, Type? valueBaseType)
            : base(null)
        {
            this.keyBaseType = keyBaseType;
            this.valueBaseType = valueBaseType;
        }

        public void SetBaseTypes(Type? keyBaseType, Type? valueBaseType)
        {
            this.keyBaseType = keyBaseType;
            this.valueBaseType = valueBaseType;
        }

        public new Action Register(Type key, Type value)
        {
            if (keyBaseType is not null)
                AssertType(keyBaseType, key);
            if (valueBaseType is not null)
                AssertType(valueBaseType, value);
            return base.Register(key, value);
        }

        /// <summary>
        /// Removes the entries whose key or value is a type of <paramref name="assembly"/>.
        /// </summary>
        public void RemoveTypesOf(Assembly assembly)
        {
            foreach (var entry in Entries.Where(x => x.Key.Assembly == assembly || x.Value.Assembly == assembly).ToList())
                Remove(entry.Key);
        }
    }

    private sealed class Registry<T>(Action<T> register) : IPluginRegistry<T>
        where T : notnull
    {
        public void Register(T item) => register(item);
    }

    private sealed class Map<TKey, TValue>(Action<TKey, TValue> register) : IPluginMap<TKey, TValue>
        where TKey : notnull
        where TValue : notnull
    {
        public void Register(TKey key, TValue value) => register(key, value);
    }

    /// <summary>
    /// The context of a plugin joining the session: each registration goes to the host and to the plugin's record.
    /// </summary>
    private sealed class EditorContext : IEditorPluginContext
    {
        public EditorContext(EditorPluginHost host, AssetsPlugin plugin, PluginRecord record, ISessionViewModel session, ILogger logger)
        {
            Plugin = plugin;
            Session = session;
            Logger = logger;
            AssetViewModelTypes = new Map<Type, Type>((key, value) => record.Add(host.assetViewModelTypes.Register(key, value)));
            AssetEditorViewModelTypes = new Map<Type, Type>((key, value) => record.Add(host.editorViewModelTypes.Register(key, value)));
            AssetEditorViewTypes = new Map<Type, Type>((key, value) => record.Add(host.editorViewTypes.Register(key, value)));
            AssetPreviewViewModelTypes = new Map<Type, Type>((key, value) => record.Add(host.previewViewModelTypes.Register(key, value)));
            AssetPreviewViewTypes = new Map<Type, Type>((key, value) => record.Add(host.previewViewTypes.Register(key, value)));
            PrimitiveTypes = new Registry<Type>(type => RegisterInList(record, host.primitiveTypes, type));
            EnumImages = new Map<object, object>((value, image) => record.Add(host.enumImages.Register(value, image)));
            TypeImages = new Map<Type, object>((type, image) => record.Add(host.typeImages.Register(type, image)));
            TemplateProviders = new Registry<ITemplateProvider>(provider =>
            {
                if (host.AddTemplateProvider(session, provider))
                    record.Add(() => host.RemoveTemplateProvider(session, provider));
            });
            // Without a copy and paste service, the session has nothing to copy or paste with: the processors are left out
            var copyPasteService = session.ServiceProvider.TryGet<ICopyPasteService>();
            CopyProcessors = new Registry<ICopyProcessor>(processor =>
            {
                if (copyPasteService is null)
                    return;
                copyPasteService.RegisterProcessor(processor);
                record.Add(() => copyPasteService.UnregisterProcessor(processor));
            });
            PasteProcessors = new Registry<IPasteProcessor>(processor =>
            {
                if (copyPasteService is null)
                    return;
                copyPasteService.RegisterProcessor(processor);
                record.Add(() => copyPasteService.UnregisterProcessor(processor));
            });
            PostPasteProcessors = new Registry<IAssetPostPasteProcessor>(processor =>
            {
                if (copyPasteService is null)
                    return;
                copyPasteService.RegisterProcessor(processor);
                record.Add(() => copyPasteService.UnregisterProcessor(processor));
            });
            EditorViewToolTypes = new Registry<Type>(type => host.RegisterEditorViewTool(record, type));
            SettingsKeys = new Registry<SettingsKey>(key => host.RegisterSettingsKey(record, key));
            ProfileSettings = new Registry<PackageSettingsEntry>(entry => RegisterInList(record, host.profileSettings, entry));
        }

        public AssetsPlugin Plugin { get; }

        public ISessionViewModel Session { get; }

        public ILogger Logger { get; }

        public IPluginMap<Type, Type> AssetViewModelTypes { get; }

        public IPluginMap<Type, Type> AssetEditorViewModelTypes { get; }

        public IPluginMap<Type, Type> AssetEditorViewTypes { get; }

        public IPluginMap<Type, Type> AssetPreviewViewModelTypes { get; }

        public IPluginMap<Type, Type> AssetPreviewViewTypes { get; }

        public IPluginRegistry<Type> PrimitiveTypes { get; }

        public IPluginMap<object, object> EnumImages { get; }

        public IPluginMap<Type, object> TypeImages { get; }

        public IPluginRegistry<ITemplateProvider> TemplateProviders { get; }

        public IPluginRegistry<ICopyProcessor> CopyProcessors { get; }

        public IPluginRegistry<IPasteProcessor> PasteProcessors { get; }

        public IPluginRegistry<IAssetPostPasteProcessor> PostPasteProcessors { get; }

        public IPluginRegistry<Type> EditorViewToolTypes { get; }

        public IPluginRegistry<SettingsKey> SettingsKeys { get; }

        public IPluginRegistry<PackageSettingsEntry> ProfileSettings { get; }
    }

    /// <summary>
    /// The context of a plugin initializing the session: each registration goes to the session and to the plugin's record.
    /// </summary>
    private sealed class SessionContext : ISessionPluginContext
    {
        private readonly List<INodePresenterCommand> registeredCommands = [];
        private readonly List<INodePresenterUpdater> registeredUpdaters = [];

        public SessionContext(EditorPluginHost host, AssetsPlugin plugin, PluginRecord record, ISessionViewModel session, ILogger logger)
        {
            Plugin = plugin;
            Session = session;
            Logger = logger;
            Services = new Registry<object>(service =>
            {
                session.ServiceProvider.RegisterService(service);
                record.Add(() => session.ServiceProvider.UnregisterService(service));
            });
            NodePresenterCommands = new Registry<INodePresenterCommand>(command =>
            {
                var commands = GetGraphService().AvailableCommands;
                commands.Add(command);
                registeredCommands.Add(command);
                record.Add(() => commands.Remove(command));
            });
            NodePresenterUpdaters = new Registry<INodePresenterUpdater>(updater =>
            {
                var updaters = GetGraphService().AvailableUpdaters;
                updaters.Add(updater);
                registeredUpdaters.Add(updater);
                record.Add(() => updaters.Remove(updater));
            });
            OnCloseAction = record.Add;

            Presentation.Quantum.GraphViewModelService GetGraphService()
            {
                return host.GetGraphViewModelService(session) ?? throw new InvalidOperationException("The session has no property grid.");
            }
        }

        public AssetsPlugin Plugin { get; }

        public ISessionViewModel Session { get; }

        public ILogger Logger { get; }

        public IPluginRegistry<object> Services { get; }

        public IPluginRegistry<INodePresenterCommand> NodePresenterCommands { get; }

        public IPluginRegistry<INodePresenterUpdater> NodePresenterUpdaters { get; }

        /// <summary>
        /// The commands registered through this context, which the comparison of the property grid leaves out.
        /// </summary>
        public IReadOnlyList<INodePresenterCommand> RegisteredCommands => registeredCommands;

        /// <summary>
        /// The updaters registered through this context, which the comparison of the property grid leaves out.
        /// </summary>
        public IReadOnlyList<INodePresenterUpdater> RegisteredUpdaters => registeredUpdaters;

        private Action<Action> OnCloseAction { get; }

        public void OnClose(Action action) => OnCloseAction(action);
    }

    /// <summary>
    /// The logger of the plugins and of the host: the one of the session while it opens, then the global log (module
    /// <see cref="PluginsLoggerModule"/>), since nothing reads the session's logger once the session is open.
    /// </summary>
    private sealed class PluginLogger(EditorPluginHost host) : ILogger
    {
        private ILogger Target => (host.sessionOpened ? null : host.logger) ?? GlobalLogger.GetLogger(PluginsLoggerModule);

        public string? Module => Target.Module;

        public void Log(ILogMessage logMessage) => Target.Log(logMessage);
    }
}
