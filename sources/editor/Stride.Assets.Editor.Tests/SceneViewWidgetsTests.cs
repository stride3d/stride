// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Widgets;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The widgets of the view of a scene or prefab editor (<see cref="SceneViewWidgetDescriptor"/>): one set per editor, which
/// follows the plugins, creates a widget's view model when it shows, and remembers per scene which widgets the user hid.
/// </summary>
public sealed class SceneViewWidgetsTests
{
    [Fact]
    public async Task TheWidgetsOfAPluginComeAndGoWithThePlugin()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var created = new List<TestWidgetViewModel>();
        var plugin = new WidgetsPlugin(
            Widget("Tests.Compass", SceneViewEdge.Right, created),
            Widget("Tests.Stats", SceneViewEdge.Top, created, order: 1),
            Widget("Tests.Hidden", SceneViewEdge.Top, created, visibleByDefault: false),
            Widget("Tests.Ruler", SceneViewEdge.Top, created, order: 0));
        try
        {
            var widgets = editor.GetOrCreateSceneViewWidgets()!;
            Assert.Same(widgets, editor.GetOrCreateSceneViewWidgets());
            Assert.Same(widgets, editor.SceneViewWidgets);
            Assert.False(widgets.HasWidgets);
            var changes = 0;
            widgets.WidgetsChanged += (_, _) => changes++;

            fixture.Plugins.PluginsDiscovered([plugin]);
            await ExclusiveDispatcherService.WaitUntilAsync(() => widgets.HasWidgets, "the widgets of the plugin");

            // By edge, then by order
            Assert.Equal(["Tests.Hidden", "Tests.Ruler", "Tests.Stats", "Tests.Compass"], widgets.Widgets.Select(x => x.Id));
            Assert.Equal(["Tests.Ruler", "Tests.Stats"], widgets.GetShownWidgets(SceneViewEdge.Top).Select(x => x.Id));
            Assert.Equal(["Tests.Compass"], widgets.GetShownWidgets(SceneViewEdge.Right).Select(x => x.Id));
            Assert.Equal("Title of Tests.Ruler", widgets.Widgets[1].Title);
            // A hidden widget has no view model until it shows
            Assert.Equal(3, created.Count);
            Assert.Null(widgets.Widgets[0].Content);
            Assert.All(created, x => Assert.Same(editor, x.Context.Editor));
            Assert.True(changes > 0);

            // The plugin leaves: its widgets go, and their view models are destroyed
            fixture.Plugins.PluginsRemoved([plugin]);
            await ExclusiveDispatcherService.WaitUntilAsync(() => !widgets.HasWidgets, "the widgets to go");
            Assert.All(created, x => Assert.True(x.IsDestroyed));

            // It comes back: new view models
            fixture.Plugins.PluginsDiscovered([plugin]);
            await ExclusiveDispatcherService.WaitUntilAsync(() => widgets.HasWidgets, "the widgets to come back");
            Assert.Equal(6, created.Count);
        }
        finally
        {
            fixture.Plugins.PluginsRemoved([plugin]);
            editor.Destroy();
        }
        Assert.Null(editor.SceneViewWidgets);
        Assert.All(created, x => Assert.True(x.IsDestroyed));
    }

    [Fact]
    public async Task TheVisibilityOfTheWidgetsIsSavedPerScene()
    {
        await using var fixture = await OpenSceneAsync();
        var created = new List<TestWidgetViewModel>();
        var plugin = new WidgetsPlugin(Widget("Tests.Stats", SceneViewEdge.Top, created), Widget("Tests.Hidden", SceneViewEdge.Bottom, created, visibleByDefault: false));
        fixture.Plugins.PluginsDiscovered([plugin]);
        try
        {
            var editor = await OpenEditorAsync(fixture);
            var widgets = editor.GetOrCreateSceneViewWidgets()!;
            var stats = widgets.Widgets.Single(x => x.Id == "Tests.Stats");
            var hidden = widgets.Widgets.Single(x => x.Id == "Tests.Hidden");
            Assert.True(stats.IsVisible);
            Assert.False(hidden.IsVisible);

            // The user hides one and shows the other
            stats.IsVisible = false;
            hidden.IsVisible = true;

            Assert.Empty(widgets.GetShownWidgets(SceneViewEdge.Top));
            var shown = Assert.Single(widgets.GetShownWidgets(SceneViewEdge.Bottom));
            Assert.NotNull(shown.Content);
            // A hidden widget keeps its view model for when it shows again
            Assert.NotNull(stats.Content);
            var scene = editor.Asset;
            var saved = scene.Directory.Package.UserSettings.GetValue(PackageSceneViewWidgets.Visibility)[scene.Asset.Id];
            Assert.Equal(new Dictionary<string, bool> { ["Tests.Stats"] = false, ["Tests.Hidden"] = true }, saved);
            editor.Destroy();

            // The scene opens again: as the user left it
            editor = await OpenEditorAsync(fixture);
            widgets = editor.GetOrCreateSceneViewWidgets()!;
            Assert.False(widgets.Widgets.Single(x => x.Id == "Tests.Stats").IsVisible);
            Assert.True(widgets.Widgets.Single(x => x.Id == "Tests.Hidden").IsVisible);
            editor.Destroy();
        }
        finally
        {
            fixture.Plugins.PluginsRemoved([plugin]);
        }
    }

    [Fact]
    public async Task AWidgetThatFailsIsTurnedOffAloneAndRecordedForItsPlugin()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var created = new List<TestWidgetViewModel>();
        var failing = new SceneViewWidgetDescriptor
        {
            Id = "Tests.Failing",
            Title = () => "Failing",
            CreateViewModel = _ => throw new InvalidOperationException("No data."),
        };
        var plugin = new WidgetsPlugin(failing, Widget("Tests.Stats", SceneViewEdge.Top, created));
        fixture.Plugins.PluginsDiscovered([plugin]);
        try
        {
            var widgets = editor.GetOrCreateSceneViewWidgets()!;

            Assert.Equal(["Tests.Stats"], widgets.Widgets.Select(x => x.Id));
            var status = fixture.Plugins.GetPluginStatus(plugin);
            Assert.NotNull(status);
            Assert.Equal(PluginState.Joined, status.State);
            Assert.Equal(ScenePluginContributions.SceneViewWidget("Tests.Failing"), Assert.Single(status.Errors).Contribution);
            // It stays off in the editors that open next
            var other = await OpenEditorAsync(fixture);
            Assert.Equal(["Tests.Stats"], other.GetOrCreateSceneViewWidgets()!.Widgets.Select(x => x.Id));
            other.Destroy();
        }
        finally
        {
            fixture.Plugins.PluginsRemoved([plugin]);
            editor.Destroy();
        }
    }

    [Fact]
    public async Task AWidgetNeedsAnIdentifierOfItsOwn()
    {
        await using var fixture = await OpenSceneAsync();
        var created = new List<TestWidgetViewModel>();
        var first = new WidgetsPlugin(Widget("Tests.Stats", SceneViewEdge.Top, created));
        var second = new WidgetsPlugin(Widget("Tests.Stats", SceneViewEdge.Bottom, created));
        var unnamed = new WidgetsPlugin(Widget(" ", SceneViewEdge.Top, created));
        fixture.Plugins.PluginsDiscovered([first, second, unnamed]);
        try
        {
            // The registry refuses the second widget with the same identifier, and one without: their plugins don't join
            Assert.Contains(first, fixture.Plugins.SessionPlugins);
            Assert.DoesNotContain(second, fixture.Plugins.SessionPlugins);
            Assert.DoesNotContain(unnamed, fixture.Plugins.SessionPlugins);
            Assert.Equal(SceneViewEdge.Top, Assert.Single(fixture.Plugins.SceneViewWidgets.Items).Edge);
        }
        finally
        {
            fixture.Plugins.PluginsRemoved([first, second, unnamed]);
        }
    }

    private static SceneViewWidgetDescriptor Widget(string id, SceneViewEdge edge, List<TestWidgetViewModel> created, int order = 0, bool visibleByDefault = true)
    {
        return new SceneViewWidgetDescriptor
        {
            Id = id,
            Title = () => $"Title of {id}",
            Edge = edge,
            Order = order,
            VisibleByDefault = visibleByDefault,
            CreateViewModel = context =>
            {
                var widget = new TestWidgetViewModel(context);
                created.Add(widget);
                return widget;
            },
        };
    }

    private sealed class WidgetsPlugin(params SceneViewWidgetDescriptor[] widgets) : AssetsPlugin
    {
        public override void Initialize(IEditorPluginContext context)
        {
            base.Initialize(context);
            foreach (var widget in widgets)
                context.SceneViewWidgets().Register(widget);
        }
    }

    private sealed class TestWidgetViewModel(SceneViewWidgetContext context) : IDestroyable
    {
        public SceneViewWidgetContext Context { get; } = context;

        public bool IsDestroyed { get; private set; }

        public void Destroy() => IsDestroyed = true;
    }
}
