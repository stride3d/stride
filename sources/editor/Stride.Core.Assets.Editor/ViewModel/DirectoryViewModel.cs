// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public sealed class DirectoryViewModel : DirectoryBaseViewModel, ISessionObjectViewModel
    {
        private string name;
        private DirectoryBaseViewModel parent;
        private static ThumbnailData FolderThumbnail;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryViewModel"/> class with a parent directory.
        /// </summary>
        /// <param name="name">The name of this directory. Cannot be <c>null</c>.</param>
        /// <param name="parent">The parent of this directory. Cannot be <c>null</c>.</param>
        /// <param name="canUndoRedoCreation">Indicates if the creation of this view model should create a transaction in the undo/redo service.</param>
        public DirectoryViewModel(string name, DirectoryBaseViewModel parent, bool canUndoRedoCreation)
            : base(parent.SafeArgument(nameof(parent)).Package.Session)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            // Update property to make the directory dirty. The name must be already set here!
            if (canUndoRedoCreation)
            {
                Parent = parent;
            }
            else
            {
                this.parent = parent;
                SetParent(null, parent);
            }
            if (!IsValidName(name, out string errorMessage)) throw new ArgumentException(errorMessage);
            RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);
        }

        /// <summary>
        /// Gets or sets the name of this directory.
        /// </summary>
        public override string Name { get { return name; } set { Rename(value); } }

        /// <summary>
        /// Gets the path of this directory in its current package.
        /// </summary>
        public override string Path => Parent.Path + Name + (Name.Length > 0 ? Separator : "");

        /// <summary>
        /// Gets or sets the parent directory of this directory.
        /// </summary>
        public override DirectoryBaseViewModel Parent { get { return parent; } set { var oldParent = Parent; SetValue(ref parent, value, () => SetParent(oldParent, value)); } }

        /// <inheritdoc/>
        public override MountPointViewModel Root => Parent.Root;

        /// <summary>
        /// Gets whether this directory is editable.
        /// </summary>
        public override bool IsEditable => Package.IsEditable;

        /// <summary>
        /// Gets the package containing this directory.
        /// </summary>
        public override PackageViewModel Package => Parent.Package;

        /// <summary>
        /// Gets the session object containing this directory.
        /// </summary>
        public string RootContainerName => Package.Name;

        /// <inheritdoc/>
        public ThumbnailData ThumbnailData => FolderThumbnail ?? (FolderThumbnail = GetFolderThumbnail(Dispatcher));

        /// <inheritdoc/>
        public override string TypeDisplayName => "Folder";

        /// <summary>
        /// Gets the command that initiates the renaming of this directory.
        /// </summary>
        [NotNull]
        public ICommandBase RenameCommand { get; }

        private static ThumbnailData GetFolderThumbnail(IDispatcherService dispatcher)
        {
            const string assetKey = "FolderIconAlfredo";
            var objectId = ObjectId.FromObject(assetKey);
            var data = new ResourceThumbnailData(objectId, assetKey);
            data.PrepareForPresentation(dispatcher).Forget();
            return data;
        }

        public override bool CanDelete(out string error)
        {
            error = "";
            if (!IsEditable)
            {
                error = Tr._p("Message", "This package that contains this folder can't be edited.");
            }
            return IsEditable;
        }

        /// <summary>
        /// Deletes this directory and all its subdirectories and contained assets.
        /// </summary>
        public override void Delete()
        {
            string message = $@"Delete folder ""{Path}""";
            var previousProject = Package;
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var hierarchy = new List<DirectoryBaseViewModel>();
                GetDirectoryHierarchy(hierarchy);
                foreach (var asset in hierarchy.SelectMany(directory => directory.Assets))
                {
                    asset.IsDeleted = true;
                }
                Parent = null;
                UndoRedoService.SetName(transaction, message);
            }
            previousProject.CheckConsistency();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "DirectoryViewModel [" + Path + "]";
        }

        private void Rename(string newName)
        {
            string error;
            if (IsValidName(newName, out error) == false)
            {
                ServiceProvider.Get<IDialogService>().BlockingMessageBox(string.Format(Tr._p("Message", "Unable to rename folder. {0}"), error), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                string previousName = name;
                if (SetValue(ref name, newName, nameof(Name), nameof(Path)))
                {
                    IsEditing = false;
                    UpdateAssetUrls();
                }
                UndoRedoService.SetName(transaction, $@"Rename folder ""{previousName}"" to ""{newName}""");
            }
        }

        protected override bool IsValidName(string value, out string error)
        {
            if (!base.IsValidName(value, out error))
            {
                return false;
            }

            if (Parent.SubDirectories.Any(x => x != this && string.Equals(x.Name, value, StringComparison.InvariantCultureIgnoreCase)))
            {
                error = Tr._p("Message", "A folder with the same name already exists in the parent folder.");
                return false;
            }

            if (!IsPathAcceptedByFilesystem(UPath.Combine<UDirectory>(Package.PackagePath.GetFullDirectory(), value), out error))
            {
                return false;
            }

            return true;
        }

        public static bool IsPathAcceptedByFilesystem(string path, out string message)
        {
            message = "";
            var ok = true;
            try
            {
                var normalized = UPath.Normalize(path);
                var result = System.IO.Path.GetFullPath(normalized.ToString());
                if (result.StartsWith("\\\\.\\"))
                {
                    message = Tr._p("Message", "Path is a device name");  // pipe, CON, NUL, COM1...
                    ok = false;
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                ok = false;
            }
            return ok;
        }

        protected override void UpdateIsDeletedStatus()
        {
            throw new NotImplementedException();
        }
    }
}
