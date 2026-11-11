// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using static Stride.Core.Assets.Editor.Tests.Panels.PanelTestHelpers;

namespace Stride.Core.Assets.Editor.Tests.Panels;

public sealed class TestPanelRegistry
{
    [Fact]
    public void PanelsAreRegisteredAndRemovedPerOwner()
    {
        var registry = new PanelRegistry();
        var changes = new List<(NotifyCollectionChangedAction, string)>();
        registry.CollectionChanged += (_, e) => changes.Add((e.Action, ((PanelDescriptor)(e.NewItems ?? e.OldItems)![0]!).Id));
        var pluginA = new object();
        var pluginB = new object();

        registry.Register(Panel("Stride.Editor"));
        registry.Register(Panel("A.One"), pluginA);
        var two = registry.Register(Panel("A.Two"), pluginA);
        registry.Register(Panel("B.One"), pluginB);

        Assert.Equal(["Stride.Editor", "A.One", "A.Two", "B.One"], registry.Panels.Select(x => x.Id));
        Assert.Same(pluginA, registry.GetOwner("A.One"));
        Assert.Null(registry.GetOwner("Stride.Editor"));

        // Disposing the registration removes one panel
        two.Dispose();
        two.Dispose();
        Assert.False(registry.Contains("A.Two"));

        // A plugin leaves with its panels only
        Assert.Equal(1, registry.UnregisterOwner(pluginA));
        Assert.Equal(["Stride.Editor", "B.One"], registry.Panels.Select(x => x.Id));
        Assert.True(registry.Unregister("B.One"));
        Assert.False(registry.Unregister("B.One"));

        Assert.Equal(
        [
            (NotifyCollectionChangedAction.Add, "Stride.Editor"), (NotifyCollectionChangedAction.Add, "A.One"), (NotifyCollectionChangedAction.Add, "A.Two"),
            (NotifyCollectionChangedAction.Add, "B.One"), (NotifyCollectionChangedAction.Remove, "A.Two"), (NotifyCollectionChangedAction.Remove, "A.One"),
            (NotifyCollectionChangedAction.Remove, "B.One"),
        ], changes);
    }

    [Fact]
    public void AnIdentifierIsRegisteredOnce()
    {
        var registry = new PanelRegistry();
        registry.Register(Panel("A.One"));

        Assert.Throws<ArgumentException>(() => registry.Register(Panel("A.One"), new object()));
        Assert.Throws<ArgumentException>(() => registry.Register(Panel(" ")));
        Assert.Single(registry.Panels);
    }

    [Fact]
    public void ThePanelsOfAPluginGoWhenItLeaves()
    {
        var plugin = new TestEditorPluginHost.LoggingPlugin("Panels");
        var host = new RecordingEditorPluginHost([plugin]);
        var session = new FakeSession();
        host.RegisterSession(session, new LoggerResult());
        host.Panels.Register(Panel("Stride.Editor"));
        host.Panels.Register(Panel("Panels.Tool"), plugin);

        host.PluginsRemoved([plugin]);

        Assert.Equal(["Stride.Editor"], host.Panels.Panels.Select(x => x.Id));
        host.Dispose();
        // The panels of the editor stay with the host
        Assert.True(host.Panels.Contains("Stride.Editor"));
    }

    [Fact]
    public void TheBuiltInPanelsHaveTheIdentifiersOfWpfAndItsMenuSections()
    {
        var registry = CreateBuiltInRegistry();

        Assert.Equal(BuiltInIds.Order(), registry.Panels.Select(x => x.Id).Order());
        Assert.All(registry.Panels, x => Assert.False(x.AllowsMultipleInstances));
        // The 4 sections of WPF's View menu, in its order
        var sections = registry.Panels.OrderBy(x => x.Order).GroupBy(x => x.MenuGroup).Select(x => x.Select(p => p.Id).ToList()).ToList();
        Assert.Equal(
        [
            [BuiltInPanels.SolutionExplorer, BuiltInPanels.AssetView, BuiltInPanels.References],
            [BuiltInPanels.AssetPreview],
            [BuiltInPanels.PropertyGrid, BuiltInPanels.ActionHistory],
            [BuiltInPanels.AssetLog, BuiltInPanels.BuildLog],
        ], sections);
    }
}
