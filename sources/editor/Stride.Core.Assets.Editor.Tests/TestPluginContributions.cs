// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The second version of the plugin context: the plugin's panels and views, the registries that other layers declare, the
/// state of each plugin, and the guard that turns off a contribution that fails while the rest of its plugin stays.
/// </summary>
public sealed class TestPluginContributions
{
    public const string PanelId = "Tests.Contributions.Panel";

    public sealed class Widget(string name)
    {
        public override string ToString() => name;
    }

    public sealed class OtherWidget;

    public class PanelViewModelBase;

    public sealed class DerivedPanelViewModel : PanelViewModelBase;

    public sealed class PanelView;

    /// <summary>
    /// A plugin that registers a panel, a view and a widget through the context.
    /// </summary>
    public sealed class PanelPlugin : AssetsPlugin
    {
        public Widget Widget { get; } = new("widget");

        public PanelDescriptor Panel { get; } = CreatePanel(PanelId);

        public Type ViewType { get; init; } = typeof(PanelView);

        public override IReadOnlyList<System.Reflection.Assembly> ScannedAssemblies => [];

        public override void Initialize(IEditorPluginContext context)
        {
            context.Panels.Register(Panel);
            context.ViewTypes.Register(typeof(PanelViewModelBase), ViewType);
            context.GetRegistry<Widget>().Register(Widget);
        }
    }

    /// <summary>
    /// A plugin that registers into a registry nobody declared.
    /// </summary>
    public sealed class UndeclaredRegistryPlugin : AssetsPlugin
    {
        public override IReadOnlyList<System.Reflection.Assembly> ScannedAssemblies => [];

        public override void Initialize(IEditorPluginContext context)
        {
            context.PrimitiveTypes.Register(typeof(DateTime));
            context.GetRegistry<OtherWidget>().Register(new OtherWidget());
        }
    }

    private static PanelDescriptor CreatePanel(string id) => new()
    {
        Id = id,
        Title = () => id,
        CreateViewModel = _ => throw new NotSupportedException(),
    };

    private static (RecordingEditorPluginHost Host, FakeSession Session, LoggerResult Log) Open(Action<RecordingEditorPluginHost>? declare, params AssetsPlugin[] plugins)
    {
        var host = new RecordingEditorPluginHost(plugins) { PluginViewBaseType = typeof(object) };
        declare?.Invoke(host);
        var session = new FakeSession();
        session.ServiceProvider.RegisterService(host);
        var log = new LoggerResult();
        host.RegisterSession(session, log);
        return (host, session, log);
    }

    private static void DeclareWidgets(RecordingEditorPluginHost host) => host.DeclareRegistry<Widget>();

    [Fact]
    public void ThePanelsViewsAndDeclaredContributionsOfAPluginGoWithIt()
    {
        var plugin = new PanelPlugin();
        var (host, _, _) = Open(DeclareWidgets, plugin);
        var widgets = host.GetDeclaredRegistry<Widget>()!;
        try
        {
            // Registered with the plugin as their owner
            Assert.Same(plugin, host.Panels.GetOwner(PanelId));
            Assert.Equal([plugin.Widget], widgets.Items);
            Assert.Same(plugin, widgets.GetOwner(plugin.Widget));
            // A view registered for a base type covers the derived ones, and names its plugin
            Assert.Equal(typeof(PanelView), host.GetViewType(typeof(DerivedPanelViewModel), out var owner));
            Assert.Same(plugin, owner);
            Assert.Equal(typeof(PanelView), ((IAssetsPluginService)host).GetViewType(typeof(PanelViewModelBase)));
            Assert.Null(host.GetViewType(typeof(object)));

            // The plugin leaves: everything goes
            host.PluginsRemoved([plugin]);
            Assert.False(host.Panels.Contains(PanelId));
            Assert.Empty(widgets.Items);
            Assert.Empty(host.ViewTypes);
            Assert.Null(host.GetViewType(typeof(DerivedPanelViewModel), out owner));
            Assert.Null(owner);

            // It comes back
            host.PluginsDiscovered([plugin]);
            Assert.Same(plugin, host.Panels.GetOwner(PanelId));
            Assert.Equal([plugin.Widget], widgets.Items);
        }
        finally
        {
            host.Dispose();
        }

        // Disposing the host removes them too, as when the session closes
        Assert.False(host.Panels.Contains(PanelId));
        Assert.Empty(widgets.Items);
        Assert.Empty(host.ViewTypes);
    }

    [Fact]
    public void APanelUnregisteredByItsOwnerIsNotRemovedTwice()
    {
        var plugin = new PanelPlugin();
        var (host, _, log) = Open(DeclareWidgets, plugin);
        try
        {
            // The shell turned it off meanwhile
            Assert.True(host.Panels.Unregister(PanelId));
            host.PluginsRemoved([plugin]);
            Assert.DoesNotContain(log.Messages, x => x.Type >= LogMessageType.Error);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AViewOfAnotherToolkitMakesThePluginFail()
    {
        var plugin = new PanelPlugin { ViewType = typeof(string) };
        var host = new RecordingEditorPluginHost([plugin]) { PluginViewBaseType = typeof(PanelView) };
        host.DeclareRegistry<Widget>();
        var session = new FakeSession();
        var log = new LoggerResult();
        host.RegisterSession(session, log);
        try
        {
            Assert.Empty(host.SessionPlugins);
            // Rolled back: its panel registered before the view went too
            Assert.False(host.Panels.Contains(PanelId));
            Assert.Equal(PluginState.Failed, host.GetPluginStatus(plugin)?.State);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void ARegistryNobodyDeclaredMakesThePluginFail()
    {
        var plugin = new UndeclaredRegistryPlugin();
        var (host, session, log) = Open(null, plugin);
        try
        {
            Assert.Empty(host.SessionPlugins);
            Assert.DoesNotContain(typeof(DateTime), host.GetPrimitiveTypes(session));
            var status = host.GetPluginStatus(plugin)!;
            Assert.Equal(PluginState.Failed, status.State);
            var error = Assert.Single(status.Errors);
            Assert.Equal(PluginContributions.Join, error.Contribution);
            Assert.IsType<InvalidOperationException>(error.Exception);
            Assert.Contains(typeof(OtherWidget).FullName!, error.Exception.Message);
        }
        finally
        {
            host.Dispose();
        }
        Assert.Empty(host.PluginStatuses);
    }

    [Fact]
    public void ARegistryIsDeclaredOnceAndCanRefuseAContribution()
    {
        var host = new RecordingEditorPluginHost([]);
        try
        {
            var registry = host.DeclareRegistry<Widget>(widget =>
            {
                if (widget.ToString() == "bad")
                    throw new ArgumentException("refused");
            });
            Assert.Same(registry, host.GetDeclaredRegistry<Widget>());
            Assert.Null(host.GetDeclaredRegistry<OtherWidget>());
            Assert.Throws<InvalidOperationException>(() => host.DeclareRegistry<Widget>());
            Assert.Throws<ArgumentException>(() => registry.Register(new Widget("bad")));
            Assert.Empty(registry.Items);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AContributionRegistryKeepsTheOwnersAndRaisesItsChanges()
    {
        var registry = new ContributionRegistry<Widget>();
        var changes = new List<(NotifyCollectionChangedAction Action, object? Item, int Index)>();
        registry.CollectionChanged += (_, e) => changes.Add((e.Action, (e.NewItems ?? e.OldItems)![0], e.Action == NotifyCollectionChangedAction.Add ? e.NewStartingIndex : e.OldStartingIndex));
        var owner = new object();
        var first = new Widget("first");
        var second = new Widget("second");
        var editors = new Widget("editor's");

        var registration = registry.Register(first, owner);
        registry.Register(second, owner);
        registry.Register(editors);
        Assert.Throws<ArgumentException>(() => registry.Register(first));
        Assert.Equal([first, second, editors], registry.Items);
        Assert.Same(owner, registry.GetOwner(first));
        Assert.Null(registry.GetOwner(editors));

        registration.Dispose();
        registration.Dispose();
        Assert.Equal(1, registry.UnregisterOwner(owner));
        Assert.Equal([editors], registry.Items);
        Assert.True(registry.Unregister(editors));
        Assert.False(registry.Unregister(editors));
        Assert.Equal(
            [
                (NotifyCollectionChangedAction.Add, first, 0), (NotifyCollectionChangedAction.Add, second, 1), (NotifyCollectionChangedAction.Add, editors, 2),
                (NotifyCollectionChangedAction.Remove, first, 0), (NotifyCollectionChangedAction.Remove, second, 0), (NotifyCollectionChangedAction.Remove, editors, 0),
            ],
            changes);
    }

    [Fact]
    public void TheHostKeepsTheStateAndTheErrorsOfEachPlugin()
    {
        var plugin = new PanelPlugin();
        var failing = new TestEditorPluginHost.FailingPlugin();
        var host = new RecordingEditorPluginHost([plugin, failing]);
        host.DeclareRegistry<Widget>();
        var changes = new List<(AssetsPlugin Plugin, PluginState? State)>();
        host.PluginStatusChanged += (_, e) => changes.Add((e.Plugin, e.Status?.State));
        var session = new FakeSession();
        session.ServiceProvider.RegisterService(host);
        host.RegisterSession(session, new LoggerResult());
        try
        {
            Assert.Equal([(plugin, PluginState.Joined), (failing, PluginState.Failed)], host.PluginStatuses.Select(x => (x.Plugin, x.State)));
            Assert.Empty(host.GetPluginStatus(plugin)!.Errors);
            Assert.Equal([PluginContributions.Join], host.GetPluginStatus(failing)!.Errors.Select(x => x.Contribution));
            Assert.Equal([(plugin, PluginState.Joined), (failing, PluginState.Failed)], changes.Select(x => (x.Plugin, x.State)));

            // A session initialization that fails is recorded; a plugin that joined stays
            var joining = new TestEditorPluginHost.FailingPlugin { FailInitialize = false };
            host.InitializeSession(session);
            host.PluginsDiscovered([joining]);
            var status = host.GetPluginStatus(joining)!;
            Assert.Equal(PluginState.Joined, status.State);
            Assert.Equal([PluginContributions.InitializeSession], status.Errors.Select(x => x.Contribution));
            Assert.Contains(joining, host.SessionPlugins);

            // A plugin that leaves has no state any more
            changes.Clear();
            host.PluginsRemoved([plugin]);
            Assert.Null(host.GetPluginStatus(plugin));
            Assert.Equal([(plugin, (PluginState?)null)], changes);
        }
        finally
        {
            host.Dispose();
        }
        Assert.Empty(host.PluginStatuses);
    }

    [Fact]
    public void AFailingContributionIsTurnedOffAloneAndRecorded()
    {
        var plugin = new PanelPlugin();
        var (host, _, log) = Open(DeclareWidgets, plugin);
        IPluginContributionGuard guard = host;
        try
        {
            // A contribution that works
            Assert.True(guard.TryRun(plugin, PluginContributions.Panel(PanelId), () => 42, out var value));
            Assert.Equal(42, value);

            // One that fails: logged with the plugin's name, recorded, and turned off
            var runs = 0;
            var view = PluginContributions.View(typeof(PanelViewModelBase));
            Assert.False(guard.TryRun(plugin, view, () =>
            {
                ++runs;
                throw new InvalidOperationException("The view failed");
            }));
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains(typeof(PanelPlugin).FullName!) && x.Text.Contains(view));
            Assert.True(guard.IsTurnedOff(plugin, view));
            var status = host.GetPluginStatus(plugin)!;
            Assert.Equal(PluginState.Joined, status.State);
            Assert.Equal([view], status.TurnedOffContributions);
            Assert.Equal("The view failed", Assert.Single(status.Errors).Exception.Message);

            // It no longer runs this session
            Assert.False(guard.TryRun(plugin, view, () => ++runs));
            Assert.Equal(1, runs);

            // The rest of the plugin stays
            Assert.Contains(plugin, host.SessionPlugins);
            Assert.True(host.Panels.Contains(PanelId));
            Assert.False(guard.IsTurnedOff(plugin, PluginContributions.Panel(PanelId)));
            Assert.True(guard.TryRun(plugin, PluginContributions.Panel(PanelId), () => { }));

            // A plugin that joins again starts with nothing turned off
            host.PluginsRemoved([plugin]);
            host.PluginsDiscovered([plugin]);
            Assert.False(guard.IsTurnedOff(plugin, view));
            Assert.Empty(host.GetPluginStatus(plugin)!.Errors);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void TheCodeOfTheEditorIsOnlyLogged()
    {
        var (host, _, log) = Open(null);
        IPluginContributionGuard guard = host;
        try
        {
            const string owner = "Stride.GameStudio";
            Assert.False(guard.TryRun(owner, PluginContributions.Panel("Stride.Output"), () => throw new InvalidOperationException("Output failed")));
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("Stride.Output"));
            Assert.False(guard.IsTurnedOff(owner, PluginContributions.Panel("Stride.Output")));
            Assert.True(guard.TryRun(owner, PluginContributions.Panel("Stride.Output"), () => { }));
            Assert.Empty(host.PluginStatuses);
        }
        finally
        {
            host.Dispose();
        }
    }
}
