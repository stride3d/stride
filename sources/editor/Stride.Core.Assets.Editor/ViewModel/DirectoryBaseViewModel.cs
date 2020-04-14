// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class DirectoryBaseViewModel : SessionObjectViewModel, IChildViewModel, IAddChildViewModel
    {
        public const string Separator = "/";
        public const string NewFolderDefaultName = "New folder";
        private readonly AutoUpdatingSortedObservableCollection<DirectoryViewModel> subDirectories = new AutoUpdatingSortedObservableCollection<DirectoryViewModel>(CompareDirectories);
        private readonly ObservableList<AssetViewModel> assets = new ObservableList<AssetViewModel>();

        protected DirectoryBaseViewModel(SessionViewModel session)
            : base(session)
        {
            SubDirectories = new ReadOnlyObservableCollection<DirectoryViewModel>(subDirectories);
            // ReSharper disable DoNotCallOverridableMethodsInConstructor - looks like an issue in resharper
            DependentProperties.Add(nameof(Parent), new[] { nameof(Path), nameof(Package), nameof(Root) });
            // ReSharper restore DoNotCallOverridableMethodsInConstructor
            RegisterMemberCollectionForActionStack(nameof(Assets), Assets);
        }

        /// <summary>
        /// Gets the path of this directory in its current package.
        /// </summary>
        public abstract string Path { get; }

        /// <summary>
        /// Gets or sets the parent directory of this directory.
        /// </summary>
        public abstract DirectoryBaseViewModel Parent { get; set; }

        /// <summary>
        /// Gets the root directory containing this directory, or this directory itself if it is a root directory.
        /// </summary>
        public abstract MountPointViewModel Root { get; }

        /// <summary>
        /// Gets the level of this directory, root directory being <c>0</c>.
        /// </summary>
        public int Level => Parent?.Level + 1 ?? 0;

        /// <summary>
        /// Gets the collection of assets contained in this directory.
        /// </summary>
        public IReadOnlyObservableList<AssetViewModel> Assets { get { return assets; } }

        /// <summary>
        /// Gets the read-only collection of sub-directories contained in this directory.
        /// </summary>
        public ReadOnlyObservableCollection<DirectoryViewModel> SubDirectories { get; }

        /// <summary>
        /// Gets the package containing this directory.
        /// </summary>
        public abstract PackageViewModel Package { get; }

        /// <inheritdoc/>
        public override IEnumerable<IDirtiable> Dirtiables => Parent != null ? base.Dirtiables.Concat(Parent.Dirtiables) : base.Dirtiables;

        /// <summary>
        /// Creates a new sub-directory with a default name in this directory.
        /// </summary>
        /// <param name="editing">Indicates whether the new sub-directory should be put in edit mode immediately when constructed.</param>
        /// <returns>A new instance of <see cref="DirectoryViewModel"/> representing the new sub-directory.</returns>
        public DirectoryViewModel CreateSubDirectory(bool editing)
        {
            var newDirectory = new DirectoryViewModel(NamingHelper.ComputeNewName(NewFolderDefaultName, SubDirectories.Cast<DirectoryBaseViewModel>(), x => x.Name), this, true) { IsEditing = editing };
            return newDirectory;
        }

        public void AddAsset(AssetViewModel asset, bool canUndoRedo)
        {
            if (canUndoRedo)
            {
                assets.Add(asset);
            }
            else
            {
                using (SuspendNotificationForCollectionChange(nameof(Assets)))
                {
                    assets.Add(asset);
                }
            }
        }

        public void RemoveAsset(AssetViewModel asset)
        {
            assets.Remove(asset);
        }

        /// <summary>
        /// Adds itself and all its sub-directories recursively th the given collection.
        /// </summary>
        /// <param name="hierarchy">The collection in which to add the directory hierarchy.</param>
        public void GetDirectoryHierarchy(ICollection<DirectoryBaseViewModel> hierarchy)
        {
            hierarchy.Add(this);
            foreach (var subDirectory in SubDirectories)
            {
                subDirectory.GetDirectoryHierarchy(hierarchy);
            }
        }
        
        /// <summary>
        /// Retrieves the directory corresponding to the given path.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>The directory corresponding to the given path if found, otherwise <c>null</c>.</returns>
        /// <remarks>The path should correspond to a directory, not an asset.</remarks>
        [CanBeNull]
        public DirectoryBaseViewModel GetDirectory(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var directoryNames = path.Split(Separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            DirectoryBaseViewModel currentDirectory = this;
            foreach (var directoryName in directoryNames)
            {
                currentDirectory = currentDirectory.SubDirectories.FirstOrDefault(x => string.Equals(directoryName, x.Name, StringComparison.InvariantCultureIgnoreCase));
                if (currentDirectory == null)
                    return null;
            }
            return currentDirectory;
        }

        /// <summary>
        /// Gets directory view model for a given path and creates all missing parts.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <param name="canUndoRedoCreation">True if register UndoRedo operation for missing path parts.</param>
        /// <returns>Given directory view model.</returns>
        /// <remarks>The path should correspond to a directory, not an asset.</remarks>
        [NotNull]
        public DirectoryBaseViewModel GetOrCreateDirectory(string path, bool canUndoRedoCreation)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            DirectoryBaseViewModel result = this;
            if (!string.IsNullOrEmpty(path))
            {
                var directoryNames = path.Split(Separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                result = directoryNames.Aggregate(result, (current, next) => current.SubDirectories.FirstOrDefault(x => string.Equals(next, x.Name, StringComparison.InvariantCultureIgnoreCase)) ?? new DirectoryViewModel(next, current, canUndoRedoCreation));
            }
            return result;
        }

        public abstract bool CanDelete(out string error);

        public abstract void Delete();

        /// <summary>
        /// Set the parent of this directory and properly update <see cref="SubDirectories"/> collection of the previous and the new parent.
        /// </summary>
        /// <remarks>Should be invoked only by the setter of <see cref="Parent"/>.</remarks>
        /// <param name="oldParent">The old parent of this directory.</param>
        /// <param name="newParent">The nwe parent of this directory.</param>
        protected void SetParent(DirectoryBaseViewModel oldParent, DirectoryBaseViewModel newParent)
        {
            var directory = this as DirectoryViewModel;
            if (directory == null) throw new InvalidOperationException("Can't change the parent of this folder");

            Dispatcher.Invoke(() =>
            {
                oldParent?.subDirectories.Remove(directory);

                if (newParent != null)
                {
                    newParent.subDirectories.Add(directory);
                    UpdateAssetUrls();
                }
            });
        }

        protected void UpdateAssetUrls()
        {
            var hierarchy = new List<DirectoryBaseViewModel>();
            GetDirectoryHierarchy(hierarchy);

            hierarchy.SelectMany(x => x.Assets).ForEach(x => Package.MoveAsset(x, x.Directory));
        }

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            if (children.Any(x => (x is IIsEditableViewModel) && !((IIsEditableViewModel)x).IsEditable))
            {
                message = "Some source items are read-only";
                return false;
            }

            if (!Package.IsEditable)
            {
                message = "Read-only package";
                return false;
            }

            message = $"Add to {Path}";

            foreach (var child in children)
            {
                var mountPoint = child as MountPointViewModel;
                var directory = child as DirectoryViewModel;
                var asset = child as AssetViewModel;
                if (mountPoint != null)
                {
                    message = DragDropBehavior.InvalidDropAreaMessage;
                    return false;
                }
                if (directory != null)
                {
                    if (directory == this)
                    {
                        message = "Can't drop a folder on itself or its children";
                        return false;
                    }
                    if (directory.Root.GetType() != Root.GetType())
                    {
                        message = "Incompatible folder";
                        return false;
                    }
                    if (directory.Root is ProjectViewModel && Root != directory.Root)
                    {
                        message = "Can't move source files between projects";
                        return false;
                    }
                    if (SubDirectories.Any(x => x.Name == directory.Name))
                    {
                        message = $"Directory {((this is DirectoryViewModel) ? Path : Name)} already contains a subfolder named {directory.Name}";
                        return false;
                    }
                    if (children.OfType<DirectoryViewModel>().Any(x => x != child && string.Compare(x.Name, directory.Name, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        message = "Can't move directories with duplicate names";
                        return false;
                    }
                    var currentParent = Parent;
                    while (currentParent != null)
                    {
                        if (currentParent == directory)
                        {
                            message = "Can't drop a directory on itself or its children";
                            return false;
                        }
                        currentParent = currentParent.Parent;
                    }
                }
                else if (asset != null)
                {
                    if (asset.IsLocked)
                    {
                        message = $"Asset {asset.Url} can't be moved";
                        return false;
                    }
                    if (Assets.Any(x => string.Compare(x.Name, asset.Name, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        message = $"{(!string.IsNullOrEmpty(Path) ? $"Directory {Path}" : "This directory")} already contains an asset named {asset.Name}";
                        return false;
                    }
                    if (children.OfType<AssetViewModel>().Any(x => x != child && string.Compare(x.Name, asset.Name, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        message = "Can't move assets with duplicate names";
                        return false;
                    }
                    if (asset.Directory.Root.GetType() != Root.GetType())
                    {
                        message = "Incompatible folder";
                        return false;
                    }
                    if (asset.Directory.Root is ProjectViewModel && Root != asset.Directory.Root)
                    {
                        message = "Can't move source files between projects";
                        return false;
                    }
                }
                else
                {
                    message = "Only folders can be dropped";
                    return false;
                }
            }
            return true;
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            int directoriesMoved = 0;
            int assetsMoved = 0;
            DirectoryViewModel singleDirectoryMoved = null;
            AssetViewModel singleAssetMoved = null;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                foreach (var child in children)
                {
                    var directory = child as DirectoryViewModel;
                    var asset = child as AssetViewModel;
                    if (directory != null)
                    {
                        ++directoriesMoved;
                        singleDirectoryMoved = directoriesMoved == 1 ? directory : null;
                        var hierarchy = new List<DirectoryBaseViewModel>();
                        directory.GetDirectoryHierarchy(hierarchy);
                        assetsMoved += hierarchy.Select(x => x.Assets.Count).Sum();
                        singleAssetMoved = assetsMoved == 1 ? hierarchy.SelectMany(x => x.Assets).FirstOrDefault() : null;
                        directory.Parent = this;
                    }
                    if (asset != null)
                    {
                        ++assetsMoved;
                        singleAssetMoved = assetsMoved == 1 ? asset : null;
                        Package.MoveAsset(asset, this);
                    }
                }
                string message = "";
                if (singleDirectoryMoved != null)
                    message = $"Move directory '{singleDirectoryMoved.Name}'";
                else if (directoriesMoved > 1)
                    message = $"Move {directoriesMoved} directories";

                if (assetsMoved > 0)
                {
                    message += message.Length > 0 ? " and " : "Move ";
                    if (singleAssetMoved != null)
                        message += $"asset '{singleAssetMoved.Url}'";
                    else
                        message += $"{assetsMoved} assets";
                }
                UndoRedoService.SetName(transaction, message);
            }
        }

        private static int CompareDirectories(DirectoryViewModel x, DirectoryViewModel y)
        {
            if (x == null && y == null)
                return 0;

            if (x != null && y != null)
                return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

            return x == null ? -1 : 1;
        }

        IChildViewModel IChildViewModel.GetParent()
        {
            return (IChildViewModel)Parent ?? Package;
        }

        string IChildViewModel.GetName()
        {
            return Name;
        }
    }
}
