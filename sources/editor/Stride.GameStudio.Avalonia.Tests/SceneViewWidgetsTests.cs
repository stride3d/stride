// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Editor;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Widgets;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.GameStudio.Avalonia.Tests.Helpers;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The widgets of the plugins in the view of the scene editor: docked on their edge over the game, shown and hidden from the
/// Widgets list, remembered per scene, and gone with their plugin.
/// </summary>
public sealed class SceneViewWidgetsTests
{
    [AvaloniaFact]
    public async Task AWidgetDocksOnItsEdgeTogglesFromTheListAndLeavesWithItsPlugin()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var created = new List<TestSceneWidgetViewModel>();
        var plugin = WidgetsPlugin(created);
        var toolbar = SceneViewContextMenuTests.AttachToolbar(scene, new SceneViewContextMenuTests.FakeToolService());
        try
        {
            var button = scene.View.FindControl<Button>("WidgetsButton")!;
            Assert.False(button.IsVisible);

            scene.Fixture.Plugins.PluginsDiscovered([plugin]);
            var top = scene.View.WidgetPanels[SceneViewEdge.Top];
            await WindowTestHelper.WaitUntilAsync(() => top.Children.OfType<TestSceneWidgetView>().Any(), "the widget on the top edge");

            // On its edge, with the view its plugin registers; the hidden one has no view model yet
            var view = Assert.Single(top.Children.OfType<TestSceneWidgetView>());
            Assert.Same(Assert.Single(created), view.DataContext);
            Assert.Empty(scene.View.WidgetPanels[SceneViewEdge.Left].Children);
            Assert.True(view.IsEffectivelyVisible);
            // Over the game, which keeps the pointer everywhere else
            var layers = Assert.IsAssignableFrom<Panel>(scene.View.WidgetLayer.Parent);
            Assert.True(layers.Children.IndexOf(scene.View.WidgetLayer) > layers.Children.IndexOf(scene.View.GameViewHost));
            Assert.Null(scene.View.WidgetLayer.Background);

            // The Widgets list, next to the gizmo options
            Assert.True(button.IsVisible);
            var checkBoxes = await OpenWidgetsListAsync(button);
            Assert.Equal(["Stats", "Hidden"], checkBoxes.Select(x => x.Content));
            Assert.Equal([true, false], checkBoxes.Select(x => x.IsChecked == true));

            // Hiding one, showing the other
            checkBoxes[0].IsChecked = false;
            checkBoxes[1].IsChecked = true;
            await WindowTestHelper.PumpAsync();
            Assert.Empty(top.Children);
            var left = Assert.Single(scene.View.WidgetPanels[SceneViewEdge.Left].Children.OfType<TestSceneWidgetView>());
            Assert.Equal("Hidden", ((TestSceneWidgetViewModel)left.DataContext!).Title);
            button.Flyout!.Hide();

            // The plugin leaves: the widgets go, with their view models
            scene.Fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.WaitUntilAsync(() => scene.View.WidgetPanels.Values.All(x => x.Children.Count == 0), "the widgets to leave");
            Assert.All(created, x => Assert.True(x.IsDestroyed));
            Assert.False(button.IsVisible);
        }
        finally
        {
            scene.Fixture.Plugins.PluginsRemoved([plugin]);
            SceneViewContextMenuTests.DetachToolbar(scene, toolbar);
            await WindowTestHelper.PumpAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheWidgetsShowAsTheUserLeftThemInThisScene()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var created = new List<TestSceneWidgetViewModel>();
        var plugin = WidgetsPlugin(created);
        scene.Fixture.Plugins.PluginsDiscovered([plugin]);
        try
        {
            var widgets = scene.Editor.SceneViewWidgets;
            Assert.NotNull(widgets);
            await WindowTestHelper.WaitUntilAsync(() => widgets.HasWidgets, "the widgets");
            widgets.Widgets.Single(x => x.Id == "Tests.Stats").IsVisible = false;
            widgets.Widgets.Single(x => x.Id == "Tests.Hidden").IsVisible = true;
            var sceneAsset = (SceneViewModel)scene.Fixture.Asset("MainScene");
            var saved = sceneAsset.Directory.Package.UserSettings.GetValue(PackageSceneViewWidgets.Visibility);
            Assert.Equal(new Dictionary<string, bool> { ["Tests.Stats"] = false, ["Tests.Hidden"] = true }, saved[sceneAsset.Id]);

            // The scene closes and opens again
            Assert.True(await scene.Fixture.Session.EditorCollection.CloseAssetEditorAsync(sceneAsset, false));
            await WindowTestHelper.PumpAsync();
            Assert.All(created, x => Assert.True(x.IsDestroyed));
            scene.Fixture.Session.AssetCollection.SelectAssets([sceneAsset]);
            await WindowTestHelper.PumpAsync();
            scene.Fixture.Session.EditSelectedContentCommand.Execute();
            EntityHierarchyEditorView? view = null;
            await WindowTestHelper.WaitUntilAsync(() => (view = scene.Window.GetVisualDescendants().OfType<EntityHierarchyEditorView>().FirstOrDefault(x => x.DataContext is SceneEditorViewModel other && other != scene.Editor)) is not null, "the scene editor");

            var reopened = ((SceneEditorViewModel)view!.DataContext!).SceneViewWidgets;
            Assert.NotNull(reopened);
            Assert.False(reopened.Widgets.Single(x => x.Id == "Tests.Stats").IsVisible);
            Assert.True(reopened.Widgets.Single(x => x.Id == "Tests.Hidden").IsVisible);
            await WindowTestHelper.WaitUntilAsync(() => view.WidgetPanels[SceneViewEdge.Left].Children.Count == 1, "the widget shown");
            Assert.Empty(view.WidgetPanels[SceneViewEdge.Top].Children);
        }
        finally
        {
            scene.Fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
        }
    }

    private static CommandsTestPlugin WidgetsPlugin(List<TestSceneWidgetViewModel> created)
    {
        return new CommandsTestPlugin(context =>
        {
            context.SceneViewWidgets().Register(Widget("Tests.Stats", "Stats", SceneViewEdge.Top, true, created));
            context.SceneViewWidgets().Register(Widget("Tests.Hidden", "Hidden", SceneViewEdge.Left, false, created));
            context.ViewTypes.Register(typeof(TestSceneWidgetViewModel), typeof(TestSceneWidgetView));
        });
    }

    private static SceneViewWidgetDescriptor Widget(string id, string title, SceneViewEdge edge, bool visibleByDefault, List<TestSceneWidgetViewModel> created)
    {
        return new SceneViewWidgetDescriptor
        {
            Id = id,
            Title = () => title,
            Edge = edge,
            VisibleByDefault = visibleByDefault,
            CreateViewModel = _ =>
            {
                var widget = new TestSceneWidgetViewModel(title);
                created.Add(widget);
                return widget;
            },
        };
    }

    // Opens the Widgets list as a click does, and gives its check boxes
    private static async Task<List<CheckBox>> OpenWidgetsListAsync(Button button)
    {
        button.Flyout!.ShowAt(button);
        await WindowTestHelper.PumpAsync();
        var list = (ItemsControl)((Flyout)button.Flyout).Content!;
        List<CheckBox> checkBoxes = [];
        await WindowTestHelper.WaitUntilAsync(() => (checkBoxes = list.GetVisualDescendants().OfType<CheckBox>().ToList()).Count == list.ItemCount, "the check boxes of the list");
        return checkBoxes;
    }
}

/// <summary>
/// The view model of the scene view widgets of <see cref="SceneViewWidgetsTests"/>.
/// </summary>
public sealed class TestSceneWidgetViewModel(string title) : IDestroyable
{
    public string Title { get; } = title;

    public bool IsDestroyed { get; private set; }

    public void Destroy() => IsDestroyed = true;
}

/// <summary>
/// The view of <see cref="TestSceneWidgetViewModel"/>.
/// </summary>
public sealed class TestSceneWidgetView : TextBlock
{
    public TestSceneWidgetView()
    {
        Text = "Widget";
        Background = global::Avalonia.Media.Brushes.DarkSlateGray;
        Padding = new global::Avalonia.Thickness(6);
    }
}
