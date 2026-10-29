// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Views;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Common.Tests;

/// <summary>
/// The plugin host shared by the editors: what a plugin registers, through its contexts or the attributes of its assembly,
/// is recorded under it and removed when it leaves.
/// </summary>
public class TestEditorPluginHost
{
    private static readonly SettingsContainer ContextSettings = new();
    private static readonly SettingsKey<int> ContextKey = new("Test/PluginHost/ContextKey", ContextSettings, 1);

    /// <summary>
    /// A plugin that registers one of each contribution through the contexts.
    /// </summary>
    public sealed class ContextPlugin : AssetsEditorPlugin
    {
        public TestTemplateProvider Provider { get; } = new();

        public TestCopyProcessor CopyProcessor { get; } = new();

        public PackageSettingsEntry ProfileEntry { get; } = new(ContextKey);

        public TestSessionService Service { get; } = new();

        public TestNodeCommand Command { get; } = new();

        public TestNodeUpdater Updater { get; } = new();

        public int Closed { get; private set; }

        public override void Initialize(IEditorPluginContext context)
        {
            context.AssetPreviewViewModelTypes.Register(typeof(Uri), typeof(Version));
            context.AssetPreviewViewTypes.Register(typeof(Uri), typeof(Guid));
            context.PrimitiveTypes.Register(typeof(Uri));
            context.EnumImages.Register(PluginHostImageEnum.Registered, "registered-image");
            context.EnumImages.Register(PluginHostImageEnum.Both, "plugin-image");
            context.TypeImages.Register(typeof(Uri), "uri-image");
            context.TemplateProviders.Register(Provider);
            context.CopyProcessors.Register(CopyProcessor);
            context.SettingsKeys.Register(ContextKey);
            context.ProfileSettings.Register(ProfileEntry);
        }

        public override void InitializeSession(ISessionPluginContext context)
        {
            context.Services.Register(Service);
            context.NodePresenterCommands.Register(Command);
            context.NodePresenterUpdaters.Register(Updater);
            context.OnClose(() => Closed++);
        }
    }

    /// <summary>
    /// A plugin written for the old API: it only overrides the <c>Register*</c> methods and the session initialization.
    /// </summary>
    public sealed class LegacyPlugin : AssetsEditorPlugin
    {
        public TestTemplateProvider Provider { get; } = new();

        public TestCopyProcessor CopyProcessor { get; } = new();

        public TestNodeCommand Command { get; } = new();

        public override void RegisterAssetViewModelTypes(IDictionary<Type, Type> assetViewModelTypes)
        {
            // Also declared by the attribute: the same registration twice is not an error
            assetViewModelTypes.Add(typeof(PluginHostAsset), typeof(PluginHostAssetViewModel));
        }

        public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes) => primitiveTypes.Add(typeof(Version));

        public override void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewModelTypes)
            => assetEditorViewModelTypes.Add(typeof(PluginHostAssetViewModel), typeof(PluginHostAssetEditorViewModel));

        public override void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
            => assetEditorViewTypes.Add(typeof(PluginHostAssetEditorViewModel), typeof(PluginHostEditorView));

        public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
            => assetPreviewViewModelTypes.Add(typeof(TimeSpan), typeof(Version));

        public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
            => assetPreviewViewTypes.Add(typeof(TimeSpan), typeof(Guid));

        public override void RegisterEnumImages(IDictionary<object, object> enumImages) => enumImages.Add(PluginHostImageEnum.None, "legacy-image");

        public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders) => templateProviders.Add(Provider);

        public override void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, ISessionViewModel session) => copyProcessors.Add(CopyProcessor);

        public override void InitializeSession(ISessionViewModel session)
        {
            // Added directly to the property grid, as the built-in plugins do
            session.ServiceProvider.Get<RecordingEditorPluginHost>().GraphService.AvailableCommands.Add(Command);
        }
    }

    public sealed class FailingPlugin : AssetsEditorPlugin
    {
        public bool FailInitialize { get; init; } = true;

        public override void Initialize(IEditorPluginContext context)
        {
            context.PrimitiveTypes.Register(typeof(DateTime));
            if (FailInitialize)
                throw new InvalidOperationException("Initialize failed");
        }

        public override void InitializeSession(ISessionPluginContext context)
        {
            context.Services.Register(this);
            throw new InvalidOperationException("Session failed");
        }
    }

    /// <summary>
    /// A plugin that logs through its session context when it joins, and when it leaves.
    /// </summary>
    public sealed class LoggingPlugin(string name) : AssetsPlugin
    {
        public override void InitializeSession(ISessionPluginContext context)
        {
            context.Logger.Warning($"{name} joined");
            context.OnClose(() => context.Logger.Warning($"{name} left"));
        }
    }

    public sealed class ConflictingPlugin : AssetsPlugin
    {
        public override void Initialize(IEditorPluginContext context)
        {
            // The attribute scan already gave the asset another view model
            context.AssetViewModelTypes.Register(typeof(PluginHostAsset), typeof(PluginHostAssetViewModel2));
        }
    }

    public sealed class PluginHostAssetViewModel2(AssetViewModelConstructionParameters parameters) : AssetViewModel<PluginHostAsset>(parameters);

    private static (RecordingEditorPluginHost Host, FakeSession Session, RecordingCopyPasteService CopyPaste, LoggerResult Log) Open(params AssetsPlugin[] plugins)
        => Open(null, plugins);

    private static (RecordingEditorPluginHost Host, FakeSession Session, RecordingCopyPasteService CopyPaste, LoggerResult Log) Open(QueueDispatcherService? dispatcher, params AssetsPlugin[] plugins)
    {
        var host = new RecordingEditorPluginHost(plugins, dispatcher);
        var session = new FakeSession();
        var copyPaste = new RecordingCopyPasteService();
        session.ServiceProvider.RegisterService(copyPaste);
        session.ServiceProvider.RegisterService(host);
        var log = new LoggerResult();
        host.RegisterSession(session, log);
        return (host, session, copyPaste, log);
    }

    [Fact]
    public void EveryContributionOfTheContextsIsRemovedWithThePlugin()
    {
        var plugin = new ContextPlugin();
        var (host, session, copyPaste, log) = Open(plugin);
        host.InitializeSession(session);

        Assert.Equal(typeof(Version), host.GetPreviewViewModelType(typeof(Uri)));
        Assert.Equal(typeof(Guid), host.GetPreviewViewType(typeof(Uri)));
        Assert.Contains(typeof(Uri), host.GetPrimitiveTypes(session));
        Assert.Equal("registered-image", host.GetImageForEnum(session, PluginHostImageEnum.Registered));
        Assert.Equal("uri-image", host.GetImageForType(typeof(Uri)));
        Assert.Contains(plugin.Provider, host.TemplateProviders);
        Assert.Contains(plugin.CopyProcessor, copyPaste.Processors);
        Assert.Same(ContextKey, ContextSettings.GetSettingsKey(ContextKey.Name));
        Assert.Contains(plugin.ProfileEntry, host.GetProfileSettings());
        Assert.Same(plugin.Service, session.ServiceProvider.TryGet<TestSessionService>());
        Assert.Contains(plugin.Command, host.GraphService.AvailableCommands);
        Assert.Contains(plugin.Updater, host.GraphService.AvailableUpdaters);
        Assert.Empty(log.Messages.Where(x => x.IsAtLeast(LogMessageType.Warning)));

        host.PluginsRemoved([plugin]);

        Assert.Empty(host.SessionPlugins);
        Assert.Null(host.GetPreviewViewModelType(typeof(Uri)));
        Assert.Null(host.GetPreviewViewType(typeof(Uri)));
        Assert.Empty(host.GetPrimitiveTypes(session));
        Assert.Null(host.GetImageForEnum(session, PluginHostImageEnum.Registered));
        Assert.Null(host.GetImageForType(typeof(Uri)));
        Assert.Empty(host.TemplateProviders);
        Assert.Empty(copyPaste.Processors);
        Assert.Null(ContextSettings.GetSettingsKey(ContextKey.Name));
        Assert.DoesNotContain(plugin.ProfileEntry, host.GetProfileSettings());
        Assert.Null(session.ServiceProvider.TryGet<TestSessionService>());
        Assert.DoesNotContain(plugin.Command, host.GraphService.AvailableCommands);
        Assert.DoesNotContain(plugin.Updater, host.GraphService.AvailableUpdaters);
        Assert.Equal(1, plugin.Closed);
        // What the assembly declares goes too
        Assert.Empty(host.AssetViewModelTypes);
        Assert.Empty(host.AssetEditorViewModelTypes);
        Assert.Empty(host.AssetEditorViewTypes);
        Assert.Null(host.GetImageForType(typeof(PluginHostDeclaredType)));
        Assert.False(host.HasImagesForEnum(session, typeof(PluginHostImageEnum)));
        Assert.Null(PluginHostSettings.Container.GetSettingsKey(PluginHostSettings.Key.Name));

        // Registered again when the plugin comes back (an undo brings its assembly back)
        host.PluginsDiscovered([plugin]);
        Assert.Same(ContextKey, ContextSettings.GetSettingsKey(ContextKey.Name));
        Assert.Same(PluginHostSettings.Key, PluginHostSettings.Container.GetSettingsKey(PluginHostSettings.Key.Name));
        // The editor was initialized: the plugin initializes the session right away
        Assert.Contains(plugin.Command, host.GraphService.AvailableCommands);
        host.Dispose();
    }

    [Fact]
    public void TheHostRegistersWhatTheAssemblyDeclares()
    {
        var plugin = new ContextPlugin();
        var (host, session, _, _) = Open(plugin);

        Assert.Equal(typeof(PluginHostAssetViewModel), host.GetAssetViewModelType(typeof(PluginHostAsset)));
        Assert.Equal(typeof(PluginHostAssetEditorViewModel), host.GetEditorViewModelType(typeof(PluginHostAssetViewModel)));
        Assert.Equal(typeof(PluginHostEditorView), host.GetEditorViewType(typeof(PluginHostAssetEditorViewModel)));
        Assert.True(host.HasEditorView(session, typeof(PluginHostAssetViewModel)));
        Assert.Same(PluginHostSettings.Key, PluginHostSettings.Container.GetSettingsKey(PluginHostSettings.Key.Name));
        Assert.Contains(PluginHostSettings.Key, host.SettingsKeys);

        // Declared images, after the ones the plugins register
        Assert.IsType<byte[]>(host.GetImageForType(typeof(PluginHostDeclaredType)));
        Assert.IsType<byte[]>(host.GetImageForEnum(session, PluginHostImageEnum.Declared));
        Assert.Equal("plugin-image", host.GetImageForEnum(session, PluginHostImageEnum.Both));
        Assert.True(host.HasImagesForEnum(session, typeof(PluginHostImageEnum)));
        Assert.False(host.HasImagesForEnum(null, typeof(PluginHostImageEnum)));
        Assert.Null(host.GetImageForEnum(null, PluginHostImageEnum.Declared));
        host.Dispose();
    }

    [Fact]
    public void APluginForTheOldApiWorksUnchanged()
    {
        var plugin = new LegacyPlugin();
        var (host, session, copyPaste, log) = Open(plugin);
        host.InitializeSession(session);

        Assert.Empty(log.Messages.Where(x => x.IsAtLeast(LogMessageType.Warning)));
        Assert.Contains(plugin, host.SessionPlugins);
        Assert.Equal(typeof(PluginHostAssetViewModel), host.GetAssetViewModelType(typeof(PluginHostAsset)));
        Assert.Contains(typeof(Version), host.GetPrimitiveTypes(session));
        Assert.Equal(typeof(PluginHostAssetEditorViewModel), host.GetEditorViewModelType(typeof(PluginHostAssetViewModel)));
        Assert.Equal(typeof(PluginHostEditorView), host.GetEditorViewType(typeof(PluginHostAssetEditorViewModel)));
        Assert.Equal(typeof(Version), host.GetPreviewViewModelType(typeof(TimeSpan)));
        Assert.Equal(typeof(Guid), host.GetPreviewViewType(typeof(TimeSpan)));
        Assert.Equal("legacy-image", host.GetImageForEnum(session, PluginHostImageEnum.None));
        Assert.Contains(plugin.Provider, host.TemplateProviders);
        Assert.Contains(plugin.CopyProcessor, copyPaste.Processors);
        Assert.Contains(plugin.Command, host.GraphService.AvailableCommands);

        host.PluginsRemoved([plugin]);

        Assert.Empty(host.AssetViewModelTypes);
        Assert.Empty(host.GetPrimitiveTypes(session));
        Assert.Empty(host.AssetPreviewViewModelTypes);
        Assert.Empty(host.AssetPreviewViewTypes);
        Assert.Empty(host.EnumImages);
        Assert.Empty(host.TemplateProviders);
        Assert.Empty(copyPaste.Processors);
        // Found by comparing the property grid before and after the old session initialization
        Assert.DoesNotContain(plugin.Command, host.GraphService.AvailableCommands);
    }

    [Fact]
    public void AFailingPluginIsReportedAndTheOthersGoOn()
    {
        var failing = new FailingPlugin();
        var plugin = new ContextPlugin();
        var (host, session, _, log) = Open(failing, plugin);

        // Left out of the session, without what it registered before failing
        Assert.DoesNotContain(failing, host.SessionPlugins);
        Assert.Contains(plugin, host.SessionPlugins);
        Assert.DoesNotContain(typeof(DateTime), host.GetPrimitiveTypes(session));
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(typeof(FailingPlugin).FullName!) && x.Text.Contains("could not join"));

        host.InitializeSession(session);

        // The session initialization of the other plugins still runs
        Assert.Contains(plugin.Command, host.GraphService.AvailableCommands);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(typeof(FailingPlugin).FullName!) && x.Text.Contains("could not initialize the session"));

        // What the failing plugin registered before failing is removed with it
        Assert.Same(failing, session.ServiceProvider.TryGet<FailingPlugin>());
        host.PluginsRemoved([failing, plugin]);
        Assert.Null(session.ServiceProvider.TryGet<FailingPlugin>());
        Assert.Empty(host.GraphService.AvailableCommands);
    }

    [Fact]
    public void ARegistrationThatConflictsMakesThePluginFail()
    {
        var conflicting = new ConflictingPlugin();
        var (host, _, _, log) = Open(conflicting);

        Assert.Empty(host.SessionPlugins);
        Assert.Empty(host.AssetViewModelTypes);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(typeof(ConflictingPlugin).FullName!));
    }

    [Fact]
    public void ALatePluginInitializesTheSessionOnlyOnceTheEditorIs()
    {
        var first = new ContextPlugin();
        var (host, session, _, _) = Open(first);

        var beforeEditor = new LegacyPlugin();
        host.PluginsDiscovered([beforeEditor]);
        Assert.Contains(beforeEditor, host.SessionPlugins);
        Assert.Empty(host.GraphService.AvailableCommands);

        host.InitializeSession(session);
        var afterEditor = new LegacyPlugin();
        host.PluginsDiscovered([afterEditor]);

        Assert.Contains(afterEditor, host.SessionPlugins);
        Assert.Contains(afterEditor.Command, host.GraphService.AvailableCommands);
        host.Dispose();
    }

    [Fact]
    public void ThePluginsLogToTheSessionWhileItOpensThenToTheGlobalLog()
    {
        var id = Guid.NewGuid();
        var atOpening = new LoggingPlugin($"Opening {id}");
        var (host, session, _, log) = Open(atOpening);
        var global = new List<ILogMessage>();
        void OnGlobalMessage(ILogMessage message)
        {
            if (message.Text.Contains(id.ToString()))
            {
                lock (global)
                    global.Add(message);
            }
        }
        GlobalLogger.GlobalMessageLogged += OnGlobalMessage;
        try
        {
            host.InitializeSession(session);
            Assert.Contains(log.Messages, x => x.Text == $"Opening {id} joined");

            // Once the session is open, nothing reads its logger: a plugin loaded later logs to the global log, and so
            // does a plugin that kept its context's logger
            var late = new LoggingPlugin($"Late {id}");
            host.PluginsDiscovered([late]);
            host.PluginsRemoved([late]);
            host.Dispose();

            Assert.Equal([$"Late {id} joined", $"Late {id} left", $"Opening {id} left"], global.Select(x => x.Text));
            Assert.All(global, x => Assert.Equal(EditorPluginHost.PluginsLoggerModule, x.Module));
            Assert.DoesNotContain(log.Messages, x => x.Text.StartsWith($"Late {id}", StringComparison.Ordinal) || x.Text == $"Opening {id} left");
        }
        finally
        {
            GlobalLogger.GlobalMessageLogged -= OnGlobalMessage;
        }
    }

    [Fact]
    public void APluginFoundOnAnotherThreadJoinsThroughTheDispatcherWithoutWaiting()
    {
        var dispatcher = new QueueDispatcherService();
        var (host, _, _, _) = Open(dispatcher);

        var plugin = new ContextPlugin();
        host.PluginsDiscovered([plugin]);

        // Queued: the loader's thread does not wait for the UI thread
        Assert.Equal(1, dispatcher.Pending);
        Assert.Empty(host.SessionPlugins);

        dispatcher.RunPending();
        Assert.Contains(plugin, host.SessionPlugins);

        host.PluginsRemoved([plugin]);
        Assert.Contains(plugin, host.SessionPlugins);
        dispatcher.RunPending();
        Assert.Empty(host.SessionPlugins);
    }

    [Fact]
    public void DisposingTheHostRemovesEverythingAsTheSessionCloses()
    {
        var plugin = new ContextPlugin();
        var (host, session, copyPaste, _) = Open(plugin);
        host.InitializeSession(session);

        host.Dispose();

        Assert.Empty(host.SessionPlugins);
        Assert.Empty(host.TemplateProviders);
        Assert.Empty(copyPaste.Processors);
        Assert.Empty(host.GraphService.AvailableCommands);
        Assert.Equal(1, plugin.Closed);
        Assert.Null(ContextSettings.GetSettingsKey(ContextKey.Name));
    }

    [Fact]
    public void TwoPluginsOfOneAssemblyShareItsDeclarations()
    {
        var first = new ContextPlugin();
        var second = new LegacyPlugin();
        var (host, session, _, _) = Open(first, second);

        Assert.Equal(2, host.SessionPlugins.Count);
        Assert.Equal(typeof(PluginHostAssetViewModel), host.GetAssetViewModelType(typeof(PluginHostAsset)));

        // The keys stay registered while one plugin of the assembly is there
        host.PluginsRemoved([second]);
        Assert.Same(PluginHostSettings.Key, PluginHostSettings.Container.GetSettingsKey(PluginHostSettings.Key.Name));
        host.PluginsRemoved([first]);
        Assert.Null(PluginHostSettings.Container.GetSettingsKey(PluginHostSettings.Key.Name));
        Assert.False(host.HasImagesForEnum(session, typeof(PluginHostImageEnum)));
    }
}
