// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Assets.Entities;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Translation;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// A folder of the hierarchy of a scene or a prefab. A folder is a path in <see cref="EntityDesign.Folder"/> of its
/// entities; an empty folder exists only in the editor, and the undo and redo of its creation, deletion and renaming go
/// through <see cref="EntityFolderOperation"/>.
/// </summary>
[DebuggerDisplay("Folder = {" + nameof(Name) + "}")]
public sealed class EntityFolderViewModel : EntityHierarchyItemViewModel
{
    public const char FolderSeparator = '/';

    /// <summary>
    /// Folder paths and names ignore the case.
    /// </summary>
    internal const StringComparison FolderCase = StringComparison.OrdinalIgnoreCase;

    private string name;

    public EntityFolderViewModel(EntityHierarchyViewModel asset, string name, IEnumerable<EntityDesign> entities)
        : base(asset, entities)
    {
        this.name = name;
        Id = new AbsoluteId(Asset.Id, Guid.NewGuid());
        RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);
    }

    /// <inheritdoc/>
    public override AbsoluteId Id { get; }

    /// <summary>
    /// Gets the path of this folder from its owner (the entity or the root that contains it). Paths ignore the case.
    /// </summary>
    public string Path => CombinePath((Parent as EntityFolderViewModel)?.Path, Name);

    /// <inheritdoc/>
    /// <remarks>Folder names ignore the case.</remarks>
    public override string Name { get => name; set => SetValue(() => CanUpdateName(value), () => UpdateName(value!)); }

    /// <inheritdoc/>
    public override EntityHierarchyElementViewModel Owner => Parent?.Owner ?? throw new InvalidOperationException($"{nameof(Parent)} cannot be null");

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

    /// <summary>
    /// Gets the command that begins the renaming of this folder in place (F2).
    /// </summary>
    public ICommandBase RenameCommand { get; }

    /// <summary>
    /// Creates the folders of the given path under the given item, the last one with the given entities.
    /// </summary>
    /// <returns><c>false</c> when the path is empty or the item is a folder, which gets no subfolder this way.</returns>
    public static bool GenerateFolder(EntityHierarchyItemViewModel parent, string? path, IEnumerable<EntityDesign> entities)
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (parent is EntityFolderViewModel)
            return false;
        // Trim the separator of a malformed path
        path = path?.Trim(FolderSeparator);
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var folders = path.Split(FolderSeparator, StringSplitOptions.RemoveEmptyEntries);
        var folderCollection = parent.Folders;

        EntityFolderViewModel? subFolder;
        foreach (var folder in folders.Take(folders.Length - 1))
        {
            subFolder = folderCollection.FirstOrDefault(x => string.Equals(x.Name, folder, FolderCase));
            if (subFolder is null)
            {
                subFolder = new EntityFolderViewModel((EntityHierarchyViewModel)parent.Asset, folder, []);
                parent.Folders.Add(subFolder);
            }
            folderCollection = subFolder.Folders;
            parent = subFolder;
        }
        subFolder = new EntityFolderViewModel((EntityHierarchyViewModel)parent.Asset, folders[^1], entities);
        parent.Folders.Add(subFolder);
        return true;
    }

    public static string CombinePath(string? leftPath, string? rightPath)
    {
        return (leftPath + FolderSeparator + rightPath).Trim(FolderSeparator);
    }

    /// <inheritdoc/>
    public override int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection)
    {
        // The entities of this folder, before the given one
        var indexInFolder = 0;
        var i = 0;
        foreach (var entity in entityCollection)
        {
            if (i++ == indexInEntity)
                break;

            if (string.Equals(entity.Folder, Path, FolderCase))
                ++indexInFolder;
        }
        return indexInFolder;
    }

    /// <summary>
    /// Deletes this folder, which must not contain any entity.
    /// </summary>
    public void Delete()
    {
        if (InnerSubEntities.Any())
            throw new InvalidOperationException("The folder being deleted is not empty.");
        if (Parent is null)
            throw new InvalidOperationException($"{nameof(Parent)} cannot be null");

        var path = Path;
        var ownerId = Owner.Id;
        Parent.Folders.Remove(this);
        if (UndoRedoService is { UndoRedoInProgress: false } undoRedo)
        {
            undoRedo.PushOperation(new EntityFolderOperation((EntityHierarchyViewModel)Asset, EntityFolderOperation.Action.FolderDeleted, path, ownerId));
        }
        Destroy();
    }

    /// <inheritdoc />
    public override GraphNodePath GetNodePath()
    {
        return Owner.GetNodePath();
    }

    private bool CanUpdateName(string? newName)
    {
        // No empty name, and no separator
        if (string.IsNullOrWhiteSpace(newName) || newName.Contains(FolderSeparator))
            return false;

        // No other folder with the same name
        if (Parent is null)
            throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
        var canRename = Parent.Folders.All(x => x == this || !string.Equals(x.Name, newName, FolderCase));
        if (!canRename)
        {
            ServiceProvider.TryGet<IDialogService>()?.MessageBoxAsync(string.Format(Tr._p("Message", "Unable to rename the folder '{0}' to '{1}'. A folder with the same name already exists."), name, newName),
                MessageBoxButton.OK, MessageBoxImage.Information).Forget();
        }
        return canRename;
    }

    private void UpdateName(string newName)
    {
        // A change of the case only still updates the entities, even though paths ignore the case
        if (string.Equals(name, newName, StringComparison.Ordinal))
            return;

        if (UndoRedoService is not { UndoRedoInProgress: false } undoRedo)
        {
            name = newName;
            return;
        }

        using var transaction = undoRedo.CreateTransaction();
        // The name changes first, so that the paths are right
        var oldName = name;
        name = newName;

        foreach (var entity in InnerSubEntities)
        {
            var node = Asset.Session.AssetNodeContainer.GetOrCreateNode(entity.EntityDesign);
            // The parent of the entity is this folder or one of its subfolders
            var parent = (EntityFolderViewModel?)entity.Parent ?? throw new InvalidOperationException($"{nameof(entity.Parent)} cannot be null");
            node[nameof(EntityDesign.Folder)].Update(parent.Path);
        }

        if (Parent is null)
            throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
        undoRedo.PushOperation(new EntityFolderOperation((EntityHierarchyViewModel)Asset, EntityFolderOperation.Action.FolderRenamed, Path, oldName, Parent.Owner.Id));
        undoRedo.SetName(transaction, $"Rename folder '{oldName}' to '{newName}'");
    }
}
