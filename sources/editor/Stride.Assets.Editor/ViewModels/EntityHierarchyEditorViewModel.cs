// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

public abstract class EntityHierarchyEditorViewModel : AssetCompositeHierarchyEditorViewModel<EntityDesign, Entity, EntityHierarchyViewModel, EntityViewModel>
{
    protected EntityHierarchyEditorViewModel(EntityHierarchyViewModel asset)
        : base(asset)
    {
    }

    public EntityHierarchyRootViewModel HierarchyRoot => (EntityHierarchyRootViewModel)RootPart;

    /// <inheritdoc />
    protected override bool CanDelete()
    {
        return SelectedContent.Count > 0 && !SelectedContent.Contains(HierarchyRoot);
    }

    /// <inheritdoc />
    protected override bool CanPaste(bool asRoot)
    {
        if (!base.CanPaste(asRoot))
            return false;

        return CopyPasteService!.CanPaste(
            ClipboardService!.GetTextAsync().Result, Asset.AssetType,
            asRoot ? typeof(AssetCompositeHierarchyData<EntityDesign, Entity>) : typeof(Entity),
            typeof(AssetCompositeHierarchyData<EntityDesign, Entity>), typeof(EntityComponent));
    }

    /// <inheritdoc />
    protected override async Task Delete()
    {
        var entitiesToDelete = GetCommonRoots(SelectedItems);
        // FIXME xplat-editor
        //var ask = SceneEditorSettings.AskBeforeDeletingEntities.GetValue();
        //if (ask)
        //{
        //    var confirmMessage = Tr._p("Message", "Are you sure you want to delete this entity?");
        //    // TODO: we should compute the actual total number of entities to be deleted here (children recursively, etc.)
        //    if (entitiesToDelete.Count > 1)
        //        confirmMessage = string.Format(Tr._p("Message", "Are you sure you want to delete these {0} entities?"), entitiesToDelete.Count);
        //    var checkedMessage = string.Format(Stride.Core.Assets.Editor.Settings.EditorSettings.AlwaysDeleteWithoutAsking, "entities");
        //    var buttons = DialogHelper.CreateButtons(new[] { Tr._p("Button", "Delete"), Tr._p("Button", "Cancel") }, 1, 2);
        //    var result = await ServiceProvider.Get<IDialogService>().CheckedMessageBoxAsync(confirmMessage, false, checkedMessage, buttons, MessageBoxImage.Question);
        //    if (result.Result != 1)
        //        return;
        //    if (result.IsChecked == true)
        //    {
        //        SceneEditorSettings.AskBeforeDeletingEntities.SetValue(false);
        //        SceneEditorSettings.Save();
        //    }
        //}

        using var transaction = Session.ActionService?.CreateTransaction();
        //var foldersToDelete = SelectedContent.OfType<EntityFolderViewModel>().ToList();
        ClearSelection();

        // Delete entities first
        var entitiesPerScene = entitiesToDelete.GroupBy(x => x.Asset);
        foreach (var entities in entitiesPerScene)
        {
            entities.Key.AssetHierarchyPropertyGraph.DeleteParts(entities.Select(x => x.PartDesign), out var mapping);
            Session.ActionService?.PushOperation(new DeletedPartsTrackingOperation<EntityDesign, Entity>(entities.Key, mapping));
        }

        //// Then folders
        //foreach (var folder in foldersToDelete)
        //{
        //    folder.Delete();
        //}

        Session.ActionService?.SetName(transaction!, "Delete selected entities");
    }

    /// <inheritdoc />
    protected override async Task RefreshEditorProperties()
    {
        EditorProperties.UpdateTypeAndName(SelectedItems, _ => "Entity", x => x.Name ?? string.Empty, "entities");
        await EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems);
    }

    /// <inheritdoc />
    protected override void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedItems.Clear();
        SelectedItems.AddRange(SelectedContent.Cast<EntityHierarchyItemViewModel>().SelectMany(x => x.InnerSubEntities));
    }
}
