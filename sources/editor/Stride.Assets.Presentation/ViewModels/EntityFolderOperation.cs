// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// The creation, deletion or renaming of a folder of an entity hierarchy, for the undo and redo. Folders exist only in the
/// editor: the operation finds them through the hierarchy of the open editor of the asset.
/// </summary>
public sealed class EntityFolderOperation : DirtyingOperation
{
    private readonly AbsoluteId ownerId;
    private readonly string[] folderPath;
    private Action action;
    private string? oldFolderName;
    private EntityHierarchyViewModel? asset;

    public enum Action
    {
        FolderCreated,
        FolderDeleted,
        FolderRenamed,
    }

    public EntityFolderOperation(EntityHierarchyViewModel asset, Action action, string folderPath, string oldFolderName, AbsoluteId ownerId)
        : this(action, folderPath, ownerId, asset)
    {
        if (action != Action.FolderRenamed)
            throw new ArgumentException(@"The action must be FolderRenamed when using this constructor.", nameof(action));

        this.oldFolderName = oldFolderName;
    }

    public EntityFolderOperation(EntityHierarchyViewModel asset, Action action, string folderPath, AbsoluteId ownerId)
        : this(action, folderPath, ownerId, asset)
    {
        if (action != Action.FolderCreated && action != Action.FolderDeleted)
            throw new ArgumentException(@"The action must be FolderCreated or FolderDeleted when using this constructor.", nameof(action));
    }

    private EntityFolderOperation(Action action, string folderPath, AbsoluteId ownerId, EntityHierarchyViewModel asset)
        : base(asset.Dirtiables)
    {
        this.action = action;
        this.folderPath = folderPath.Split(EntityFolderViewModel.FolderSeparator, StringSplitOptions.RemoveEmptyEntries);
        this.ownerId = ownerId;
        this.asset = asset;
    }

    /// <inheritdoc/>
    protected override void FreezeContent()
    {
        asset = null;
    }

    /// <inheritdoc/>
    protected override void Undo()
    {
        var folderName = folderPath[^1];
        var parent = asset?.HierarchyRoot?.FindItem(ownerId);
        for (var i = 0; i < folderPath.Length - 1; ++i)
        {
            parent = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderPath[i], EntityFolderViewModel.FolderCase));
        }
        // An empty folder is not saved: it is gone after the editor is closed and opened again. The operation does nothing
        // when it can't find the folder.
        EntityFolderViewModel? folder;
        switch (action)
        {
            case Action.FolderCreated:
                folder = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
                folder?.Delete();
                action = Action.FolderDeleted;
                break;
            case Action.FolderDeleted:
                folder = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
                if (folder is null && parent is not null)
                {
                    parent.Folders.Add(new EntityFolderViewModel(asset!, folderName, []));
                    parent.IsExpanded = true;
                }
                action = Action.FolderCreated;
                break;
            case Action.FolderRenamed:
                folder = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
                if (folder is not null)
                {
                    folder.Name = oldFolderName!;
                }
                folderPath[^1] = oldFolderName!;
                oldFolderName = folderName;
                break;
            default:
                throw new InvalidOperationException($"Unknown folder action {action}.");
        }
    }

    /// <inheritdoc/>
    protected override void Redo()
    {
        Undo();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (IsFrozen)
            return $"{{{nameof(EntityFolderOperation)}: (Frozen)}}";

        var sb = new StringBuilder($"{{{nameof(EntityFolderOperation)}: ");
        var newPath = string.Join(EntityFolderViewModel.FolderSeparator.ToString(), folderPath);
        sb.Append(action switch
        {
            Action.FolderCreated => $" create {newPath}}}",
            Action.FolderDeleted => $" delete {newPath}}}",
            _ => $" rename {oldFolderName} to {newPath}}}",
        });
        return sb.ToString();
    }
}
