// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Translation;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    [DebuggerDisplay("Folder = {" + nameof(Name) + "}")]
    public sealed class EntityFolderViewModel : EntityHierarchyItemViewModel
    {
        public const char FolderSeparator = '/';
        /// <summary>
        /// Folder paths/names are case-insensitive.
        /// </summary>
        internal const StringComparison FolderCase = StringComparison.OrdinalIgnoreCase;
        private string name;

        public EntityFolderViewModel([NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EntityHierarchyViewModel asset, string name, [NotNull] IEnumerable<EntityDesign> entities)
            : base(editor, asset, entities)
        {
            this.name = name;
            Id = new AbsoluteId(Asset.Id, Guid.NewGuid());
            RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);
        }

        public AbsoluteId Id { get; }

        /// <summary>
        /// The relative path from the owning entity (or root if the folder is at the top level).
        /// </summary>
        /// /// <remarks>Folder paths are case-insensitive.</remarks>
        /// <seealso cref="Owner"/>
        [NotNull]
        public string Path => CombinePath((Parent as EntityFolderViewModel)?.Path, Name);

        /// <inheritdoc/>
        /// <remarks>Folder names are case-insensitive.</remarks>
        public override string Name { get { return name; } set { SetValue(() => CanUpdateName(value), () => UpdateName(value)); } }

        /// <inheritdoc/>
        public override EntityHierarchyElementViewModel Owner
        {
            get
            {
                if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
                return Parent.Owner;
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

        [NotNull]
        public ICommandBase RenameCommand { get; }

        public static bool GenerateFolder([NotNull] EntityHierarchyItemViewModel parent, string path, [NotNull] IEnumerable<EntityDesign> entities)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (parent is EntityFolderViewModel)
                return false;
            // Trim separator in malformed path
            path = path?.Trim(FolderSeparator);
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var folders = path.Split(new[] { FolderSeparator }, StringSplitOptions.RemoveEmptyEntries);
            var folderCollection = parent.Folders;

            EntityFolderViewModel subFolder;
            foreach (var folder in folders.Take(folders.Length - 1))
            {
                subFolder = folderCollection.FirstOrDefault(x => string.Equals(x.Name, folder, FolderCase));
                if (subFolder == null)
                {
                    subFolder = new EntityFolderViewModel(parent.Editor, parent.Asset, folder, Enumerable.Empty<EntityDesign>());
                    parent.Folders.Add(subFolder);
                }
                folderCollection = subFolder.Folders;
                parent = subFolder;
            }
            subFolder = new EntityFolderViewModel(parent.Editor, parent.Asset, folders[folders.Length - 1], entities);
            parent.Folders.Add(subFolder);
            return true;
        }

        [NotNull]
        public static string CombinePath(string leftPath, string rightPath)
        {
            return (leftPath + FolderSeparator + rightPath).Trim(FolderSeparator);
        }

        /// <inheritdoc/>
        public override int FindIndexInParent(int indexInEntity, [NotNull] IEnumerable<EntityDesign> entityCollection)
        {
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
        /// Deletes this folder. The folder must not contain any entity before being deleted.
        /// </summary>
        public void Delete()
        {
            if (InnerSubEntities.Any()) throw new InvalidOperationException("The folder being deleted is not empty.");
            var path = Path;
            var ownerId = Owner.Id;
            if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
            Parent.Folders.Remove(this);
            if (!Editor.UndoRedoService.UndoRedoInProgress)
            {
                var operation = new EntityFolderOperation(Asset, EntityFolderOperation.Action.FolderDeleted, path, ownerId);
                Editor.UndoRedoService.PushOperation(operation);
            }
        }

        /// <inheritdoc />
        public override GraphNodePath GetNodePath()
        {
            return Owner.GetNodePath();
        }

        private bool CanUpdateName([CanBeNull] string newName)
        {
            // empty names are not allowed
            if (string.IsNullOrWhiteSpace(newName))
                return false;

            // folder separator is not allowed
            if (newName.Contains(FolderSeparator))
                return false;

            // check for name collision
            if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
            var canRename = Parent.Folders.All(x => x == this || !string.Equals(x.Name, newName, FolderCase));
            if (!canRename)
            {
                ServiceProvider.Get<IDialogService>()
                    .BlockingMessageBox(string.Format(Tr._p("Message", "Unable to rename the folder '{0}' to '{1}'. A folder with the same name already exists."), name, newName), MessageBoxButton.OK,
                        MessageBoxImage.Information);
            }
            return canRename;
        }

        private void UpdateName([NotNull] string newName)
        {
            // note: even though folder paths are considered to be case-insensitive, we still want to propagate a case-only change and update the affected entities.
            if (string.Equals(name, newName, StringComparison.Ordinal))
                return;

            if (Editor.UndoRedoService.UndoRedoInProgress)
            {
                name = newName;
                return;
            }

            using (var transaction = Editor.UndoRedoService.CreateTransaction())
            {
                // We need to update the name first so that the Path properties is consistent
                var oldName = name;
                name = newName;

                foreach (var entity in InnerSubEntities)
                {
                    var node = Editor.NodeContainer.GetOrCreateNode(entity.EntityDesign);
                    // In this case, the parent is either the current folder or a subfolder in between
                    var parent = (EntityFolderViewModel)entity.Parent;
                    if (parent == null) throw new InvalidOperationException($"{nameof(parent)} cannot be null");
                    node[nameof(EntityDesign.Folder)].Update(parent.Path);
                }

                if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
                var operation = new EntityFolderOperation(Asset, EntityFolderOperation.Action.FolderRenamed, Path, oldName, Parent.Owner.Id);
                Editor.UndoRedoService.PushOperation(operation);
                Editor.UndoRedoService.SetName(transaction, $"Rename folder '{oldName}' to '{newName}'");
            }
        }
    }
}
