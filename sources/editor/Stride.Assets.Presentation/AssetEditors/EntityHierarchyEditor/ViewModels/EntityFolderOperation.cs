// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Dirtiables;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    /// <summary>
    /// Represents operation on entity folder in the editor.
    /// </summary>
    public class EntityFolderOperation : DirtyingOperation
    {
        private readonly AbsoluteId ownerId;
        private readonly string[] folderPath;
        private Action action;
        private string oldFolderName;
        private EntityHierarchyViewModel asset;

        public enum Action
        {
            FolderCreated,
            FolderDeleted,
            FolderRenamed,
        }

        public EntityFolderOperation([NotNull] EntityHierarchyViewModel asset, Action action, [NotNull] string folderPath, string oldFolderName, AbsoluteId ownerId)
            : this(action, folderPath, ownerId, asset.SafeArgument(nameof(asset)).Dirtiables)
        {
            if (action != Action.FolderRenamed)
                throw new ArgumentException(@"The action must be FolderRenamed when using this constructor.", nameof(action));

            this.asset = asset;
            this.oldFolderName = oldFolderName;
        }

        public EntityFolderOperation([NotNull] EntityHierarchyViewModel asset, Action action, [NotNull] string folderPath, AbsoluteId ownerId)
            : this(action, folderPath, ownerId, asset.SafeArgument(nameof(asset)).Dirtiables)
        {
            if (action != Action.FolderCreated && action != Action.FolderDeleted)
                throw new ArgumentException(@"The action must be FolderCreated or FolderDeleted when using this constructor.", nameof(action));

            this.asset = asset;
        }

        private EntityFolderOperation(Action action, [NotNull] string folderPath, AbsoluteId ownerId, IEnumerable<IDirtiable> dirtiables)
            : base(dirtiables)
        {
            this.action = action;
            this.folderPath = folderPath.Split(new[] { EntityFolderViewModel.FolderSeparator }, StringSplitOptions.RemoveEmptyEntries);
            this.ownerId = ownerId;
        }

        /// <inheritdoc/>
        protected override void FreezeContent()
        {
            asset = null;
        }

        /// <inheritdoc/>
        protected override void Undo()
        {
            var editor = asset.Editor;
            var folderName = folderPath[folderPath.Length - 1];
            var parent = (EntityHierarchyItemViewModel)editor?.FindPartViewModel(ownerId);
            for (var i = 0; i < folderPath.Length - 1; ++i)
            {
                parent = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderPath[i], EntityFolderViewModel.FolderCase));
            }
            // Note: empty folders are not saved and can "disappear" after closing/reopening an editor.
            // In order for action to be undo/redo-able we must allow the possibility to not find the folder.
            EntityFolderViewModel folder;
            switch (action)
            {
                case Action.FolderCreated:
                    folder = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
                    folder?.Delete();
                    action = Action.FolderDeleted;
                    break;
                case Action.FolderDeleted:
                    folder = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
                    if (folder == null && editor != null)
                    {
                        // folder not found, let's recreate it
                        if (parent == null) throw new InvalidOperationException($"{nameof(parent)} cannot be null");
                        folder = editor.CreateFolder(asset, parent, false);
                        folder.Name = folderName;
                    }
                    action = Action.FolderCreated;
                    break;
                case Action.FolderRenamed:
                    folder = parent?.Folders.FirstOrDefault(x => string.Equals(x.Name, folderName, EntityFolderViewModel.FolderCase));
                    if (folder != null)
                    {
                        folder.Name = oldFolderName;
                    }
                    folderPath[folderPath.Length - 1] = oldFolderName;
                    oldFolderName = folderName;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
            {
                return $"{{{nameof(EntityFolderOperation)}: (Frozen)}}";
            }

            try
            {
                var sb = new StringBuilder($"{{{nameof(EntityFolderOperation)}: ");
                var newPath = string.Join(EntityFolderViewModel.FolderSeparator.ToString(), folderPath);
                switch (action)
                {
                    case Action.FolderCreated:
                        sb.Append($" create {newPath}");
                        break;
                    case Action.FolderDeleted:
                        sb.Append($" delete {newPath}");
                        break;
                    case Action.FolderRenamed:
                        sb.Append($" rename {oldFolderName} to {newPath}}}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return sb.ToString();
            }
            catch (Exception)
            {
                return base.ToString();
            }
        }
    }
}
