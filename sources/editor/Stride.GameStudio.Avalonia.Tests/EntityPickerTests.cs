// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Quantum;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The entity picker of the Pick buttons of the entity and component properties (parity study G2), and the entities of the
/// scene tree dropped on these properties.
/// </summary>
public sealed class EntityPickerTests
{
    [DataContract]
    public sealed class PickTargetComponent : EntityComponent
    {
        public Entity? Target { get; set; }
    }

    [AvaloniaFact]
    public async Task ThePickerWindowFiltersAndReturnsThePick()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var picker = new EntityPickerViewModel(scene.Editor, null);
        var window = new EntityPickerWindow { DataContext = picker };
        var result = window.ShowDialog<bool>(scene.Window);
        await WindowTestHelper.PumpAsync();
        var ok = window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "OkButton");
        var tree = window.GetVisualDescendants().OfType<TreeView>().Single(x => x.Name == "EntityTree");
        Assert.Equal(["Box", "Parent"], picker.Roots.Select(x => x.Name));
        Assert.False(ok.IsEnabled);

        // The search narrows the tree to the entities whose name contains it, with their parents
        picker.SearchText = "chi";
        await WindowTestHelper.PumpAsync();
        var parent = Assert.Single(picker.Roots);
        Assert.Equal(["Child"], parent.Children.Select(x => x.Name));
        tree.SelectedItem = parent.Children[0];
        await WindowTestHelper.PumpAsync();
        Assert.True(ok.IsEnabled);

        ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(await result);
        Assert.Same(scene.Entity("Child"), picker.PickedEntity!.Entity);
    }

    [AvaloniaFact]
    public async Task ThePickButtonOfAnEntityFieldPicksAnEntityAndAnEntityCanBeDroppedOnIt()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var box = scene.Entity("Box");
        var component = new PickTargetComponent();
        using (var transaction = scene.Fixture.Session.UndoRedoService.CreateTransaction())
        {
            var components = scene.Fixture.Session.AssetNodeContainer.GetNode(box.AssetSideEntity)[nameof(Entity.Components)].Target!;
            components.Add(component, new NodeIndex(box.AssetSideEntity.Components.Count));
            scene.Fixture.Session.UndoRedoService.SetName(transaction, "Add component");
        }
        scene.Editor.SelectedContent.Clear();
        scene.Editor.SelectedContent.Add(box);
        var field = await ShowPropertyAsync(scene.Window, nameof(PickTargetComponent.Target));

        // The Pick button shows, and opens the entity picker of the editor
        var pick = field.GetVisualDescendants().OfType<Button>().Single(x => x.Command is NodePresenterCommandWrapper { Name: PickupEntityCommand.CommandName });
        Assert.True(pick.IsEffectivelyVisible);
        EntityPickerWindow? window = null;
        using (Window.WindowOpenedEvent.AddClassHandler<EntityPickerWindow>((opened, _) => window = opened))
        {
            pick.Command!.Execute(pick.CommandParameter);
            await WindowTestHelper.WaitUntilAsync(() => window is not null, "the entity picker");
        }
        await WindowTestHelper.PumpAsync();
        var picker = window!.Picker!;
        picker.SelectedEntity = picker.Roots.BreadthFirst(x => x.Children).Single(x => x.Name == "Parent");
        window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "OkButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await WindowTestHelper.WaitUntilAsync(() => component.Target == scene.Entity("Parent").AssetSideEntity, "the picked entity");

        // An entity of the tree dropped on the field is referenced too
        var drop = Interaction.GetBehaviors(field.GetVisualDescendants().OfType<DockPanel>().First(x => Interaction.GetBehaviors(x).OfType<ContentReferenceDropBehavior>().Any()))
            .OfType<ContentReferenceDropBehavior>().Single();
        var target = drop.GetDropTarget([scene.Entity("Child")], AddChildModifiers.None, out _);
        Assert.NotNull(target);
        target.AddChildren([scene.Entity("Child")], AddChildModifiers.None);
        Assert.Same(scene.Entity("Child").AssetSideEntity, component.Target);
        Assert.Null(drop.GetDropTarget([scene.Fixture.Asset("ChildScene")], AddChildModifiers.None, out _));
    }

    private static async Task<PropertyViewItem> ShowPropertyAsync(Window window, string member)
    {
        PropertyViewItem? item = null;
        await WindowTestHelper.WaitUntilAsync(() => (item = window.GetVisualDescendants().OfType<PropertyGridView>().SelectMany(x => x.GetVisualDescendants().OfType<PropertyViewItem>())
            .FirstOrDefault(x => x.DataContext is NodeViewModel node && node.Name == member)) is not null, $"the property {member}");
        // and settle: a second generation would replace the item
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return item!;
    }
}
