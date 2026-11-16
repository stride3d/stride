// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Quantum;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The Add component button of the components of the selected entities in the property grid: it lists the components the
/// entities can all take by category, with a search, and a pick adds the component to each of them in one undo step.
/// </summary>
public sealed class AddEntityComponentTests
{
    /// <summary>
    /// A component of a third party, in a category of its own.
    /// </summary>
    [DataContract]
    [Display("Probe")]
    [ComponentCategory("Probe plugin")]
    public sealed class ProbeComponent : EntityComponent
    {
    }

    [AvaloniaFact]
    public async Task TheButtonOpensTheCategoriesAndAPickedCategoryListsItsComponents()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var view = await SelectAsync(scene, "Box");

        await scene.Window.ClickAsync(view.AddButton);
        await WindowTestHelper.WaitUntilAsync(() => view.IsOpen, "the list of components");
        Assert.True(view.AddButton.IsChecked);
        Assert.True(view.SearchBox.IsFocused);

        // The categories are listed by name; no component is listed until a category is picked
        var groups = view.Picker.Groups.Select(x => x.GroupName).ToList();
        Assert.Equal(groups.Order(StringComparer.Ordinal), groups);
        Assert.Contains("Lights", groups);
        Assert.Contains("Model", groups);
        Assert.Empty(view.Picker.ShownTypes);
        Assert.False(view.TypeList.IsVisible);

        view.Groups.SelectedItem = view.Picker.Groups.Single(x => x.GroupName == "Lights");
        await WindowTestHelper.PumpAsync();
        Assert.True(view.TypeList.IsVisible);
        Assert.Contains(typeof(LightComponent), view.TypeList.Items.OfType<AbstractNodeType>().Select(x => x.Type));
        Assert.All(view.Picker.ShownTypes, x => Assert.Equal("Lights", ComponentCategoryAttribute.GetCategory(x.Type)));

        // Closing clears the list for the next time
        view.Close();
        await WindowTestHelper.PumpAsync();
        Assert.False(view.AddButton.IsChecked);
        Assert.Null(view.Picker.SelectedGroup);
        Assert.Empty(view.Picker.ShownTypes);
    }

    [AvaloniaFact]
    public async Task TheCategoryUnderThePointerListsItsComponentsAndStaysWhileThePointerIsOnThem()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var view = await SelectAsync(scene, "Box");
        await OpenAsync(view);
        var lights = view.Picker.Groups.Single(x => x.GroupName == "Lights");
        var model = view.Picker.Groups.Single(x => x.GroupName == "Model");

        // The category is shown once the pointer rests on it
        var root = TopLevel.GetTopLevel(view.Groups)!;
        // The hit test needs a rendered scene
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await WindowTestHelper.PumpAsync();
        var position = view.Groups.TranslatePoint(default, root);
        root.MouseMove(Center(GroupRow(view, lights), root));
        await WindowTestHelper.WaitUntilAsync(() => view.Picker.SelectedGroup == lights, "the hovered category");
        Assert.Null(view.Groups.SelectedItem);
        // The list keeps its place and size when the components show
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await WindowTestHelper.PumpAsync();
        Assert.Equal(position, view.Groups.TranslatePoint(default, root));

        // On its components, it is selected; crossing another category on the way is too short to show it
        root.MouseMove(Center(view.TypeList, root));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        await WindowTestHelper.PumpAsync();
        Assert.Same(lights, view.Groups.SelectedItem);
        root.MouseMove(Center(GroupRow(view, model), root));
        root.MouseMove(Center(view.TypeList, root));
        await Task.Delay(AddEntityComponentView.HoverDelay * 2);
        await WindowTestHelper.PumpAsync();
        Assert.Same(lights, view.Picker.SelectedGroup);

        // Resting on another category shows it
        root.MouseMove(Center(GroupRow(view, model), root));
        await WindowTestHelper.WaitUntilAsync(() => view.Picker.SelectedGroup == model, "the other category");
        Assert.Equal([typeof(ModelComponent)], view.Picker.ShownTypes.Select(x => x.Type).Where(x => x == typeof(ModelComponent)));
        view.Close();
    }

    [AvaloniaFact]
    public async Task TheSearchNarrowsTheListToTheMatchingComponentsOfEveryCategory()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var view = await SelectAsync(scene, "Box");
        await OpenAsync(view);

        scene.Window.KeyTextInput("light");
        await WindowTestHelper.PumpAsync();

        Assert.Equal("light", view.SearchBox.Text);
        Assert.False(view.Groups.IsEnabled);
        var shown = view.TypeList.Items.OfType<AbstractNodeType>().ToList();
        Assert.Contains(shown, x => x.Type == typeof(LightComponent));
        Assert.All(shown, x => Assert.Contains("light", x.DisplayValue, StringComparison.OrdinalIgnoreCase));
        // Every match, whatever its category
        Assert.Equal(view.Picker.Types.Where(x => x.DisplayValue.Contains("light", StringComparison.OrdinalIgnoreCase)), shown);
        Assert.Null(view.Picker.SelectedGroup);
        // The first match is selected, for Enter
        Assert.Same(shown[0], view.TypeList.SelectedItem);

        // The arrows move the selection from the search box
        scene.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        await WindowTestHelper.PumpAsync();
        Assert.Same(shown[1], view.Picker.SelectedType);

        // Escape closes the list, which is cleared
        scene.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        await WindowTestHelper.PumpAsync();
        Assert.False(view.IsOpen);
        Assert.Equal(string.Empty, view.Picker.SearchToken);
    }

    [AvaloniaFact]
    public async Task APickAddsTheComponentToEverySelectedEntityInOneUndoStep()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var box = scene.Entity("Box");
        var parent = scene.Entity("Parent");
        var view = await SelectAsync(scene, "Box", "Parent");
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        await OpenAsync(view);

        scene.Window.KeyTextInput("Model");
        await WindowTestHelper.PumpAsync();
        Assert.Equal(typeof(ModelComponent), view.Picker.SelectedType?.Type);
        scene.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        await WindowTestHelper.WaitUntilAsync(() => box.AssetSideEntity.Get<ModelComponent>() is not null && parent.AssetSideEntity.Get<ModelComponent>() is not null, "the added components");
        Assert.False(view.IsOpen);
        Assert.NotSame(box.AssetSideEntity.Get<ModelComponent>(), parent.AssetSideEntity.Get<ModelComponent>());
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");

        history.UndoCommand.Execute();
        await WindowTestHelper.PumpAsync();
        Assert.Null(box.AssetSideEntity.Get<ModelComponent>());
        Assert.Null(parent.AssetSideEntity.Get<ModelComponent>());
    }

    [AvaloniaFact]
    public async Task AClickOnAComponentAddsIt()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var box = scene.Entity("Box");
        var view = await SelectAsync(scene, "Box");
        await OpenAsync(view);
        view.Groups.SelectedItem = view.Picker.Groups.Single(x => x.GroupName == "Lights");
        await WindowTestHelper.PumpAsync();

        var light = view.TypeList.GetVisualDescendants().OfType<ListBoxItem>().Single(x => x.DataContext is AbstractNodeType { Type: var type } && type == typeof(LightComponent));
        var root = TopLevel.GetTopLevel(light)!;
        var center = light.TranslatePoint(new(light.Bounds.Width / 2, light.Bounds.Height / 2), root)!.Value;
        root.MouseDown(center, MouseButton.Left);
        root.MouseUp(center, MouseButton.Left);

        await WindowTestHelper.WaitUntilAsync(() => box.AssetSideEntity.Get<LightComponent>() is not null, "the added light");
        Assert.False(view.IsOpen);
    }

    [AvaloniaFact]
    public async Task AComponentThatCannotBeAddedTwiceIsNotOffered()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var box = scene.Entity("Box");
        AddComponents(scene, box, new ModelComponent(), new LightComponent());

        var view = await SelectAsync(scene, "Box");
        var types = view.Picker.Types.Select(x => x.Type).ToList();

        // A second model or transform is refused; a second light is allowed
        Assert.DoesNotContain(typeof(ModelComponent), types);
        Assert.DoesNotContain(typeof(TransformComponent), types);
        Assert.Contains(typeof(LightComponent), types);
        Assert.DoesNotContain(view.Picker.Groups.SelectMany(x => x.Types), x => x.Type == typeof(ModelComponent));
    }

    [AvaloniaFact]
    public async Task SeveralEntitiesAreOfferedTheComponentsTheyCanAllTake()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        AddComponents(scene, scene.Entity("Box"), new ModelComponent());

        var view = await SelectAsync(scene, "Box", "Parent");

        Assert.DoesNotContain(view.Picker.Types, x => x.Type == typeof(ModelComponent));
        Assert.Contains(view.Picker.Types, x => x.Type == typeof(LightComponent));
        // A category whose components differ between the entities is left out of the categories, as before: its other
        // components are found by a search
        Assert.DoesNotContain(view.Picker.Groups, x => x.GroupName == "Model");
        Assert.Contains(view.Picker.Groups, x => x.GroupName == "Lights");
    }

    [AvaloniaFact]
    public async Task APluginComponentIsListedInItsCategory()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var view = await SelectAsync(scene, "Box");
        await OpenAsync(view);

        var group = view.Picker.Groups.Single(x => x.GroupName == "Probe plugin");
        Assert.Equal([typeof(ProbeComponent)], group.Types.Select(x => x.Type));
        view.Groups.SelectedItem = group;
        await WindowTestHelper.PumpAsync();
        var item = Assert.Single(view.TypeList.GetVisualDescendants().OfType<ListBoxItem>());
        Assert.Contains(item.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == "Probe");

        view.Validate(group.Types.Single());
        await WindowTestHelper.WaitUntilAsync(() => scene.Entity("Box").AssetSideEntity.Get<ProbeComponent>() is not null, "the added probe");
    }

    /// <summary>
    /// Selects entities, and returns the Add component button of the property grid once it shows them.
    /// </summary>
    private static async Task<AddEntityComponentView> SelectAsync(SceneTreeDropTests.SceneFixture scene, params string[] names)
    {
        scene.Editor.SelectedContent.Clear();
        foreach (var name in names)
            scene.Editor.SelectedContent.Add(scene.Entity(name));

        AddEntityComponentView? view = null;
        await WindowTestHelper.WaitUntilAsync(() => (view = Buttons(scene.Window).FirstOrDefault()) is not null
                                                    && view.Picker.HasTypes, "the Add component button");
        // and settle: a second generation would replace the button
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return Buttons(scene.Window).Single();
    }

    private static async Task OpenAsync(AddEntityComponentView view)
    {
        view.Open();
        await WindowTestHelper.WaitUntilAsync(() => view.IsOpen && view.SearchBox.IsFocused, "the list of components");
    }

    private static void AddComponents(SceneTreeDropTests.SceneFixture scene, EntityViewModel entity, params EntityComponent[] components)
    {
        var session = scene.Fixture.Session;
        using var transaction = session.UndoRedoService.CreateTransaction();
        var collection = session.AssetNodeContainer.GetNode(entity.AssetSideEntity)[nameof(Entity.Components)].Target!;
        foreach (var component in components)
            collection.Add(component, new NodeIndex(entity.AssetSideEntity.Components.Count));
        session.UndoRedoService.SetName(transaction, "Add components");
    }

    private static ListBoxItem GroupRow(AddEntityComponentView view, AbstractNodeTypeGroup group)
    {
        return view.Groups.GetVisualDescendants().OfType<ListBoxItem>().Single(x => ReferenceEquals(x.DataContext, group));
    }

    private static Point Center(Control control, TopLevel root)
    {
        return control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), root) ?? throw new InvalidOperationException("The control is not shown.");
    }

    private static IEnumerable<AddEntityComponentView> Buttons(MainWindow window)
    {
        return window.GetVisualDescendants().OfType<PropertyGridView>().SelectMany(x => x.GetVisualDescendants().OfType<AddEntityComponentView>());
    }
}
