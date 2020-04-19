// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public sealed class AssetDependenciesViewModel : DispatcherViewModel
    {
        private static readonly HashSet<AssetViewModel> DirtyDependencies = new HashSet<AssetViewModel>();
        private static TaskCompletionSource<int> dependenciesUpdated;
        private IReadOnlyCollection<AssetViewModel> referencerAssets = new List<AssetViewModel>();
        private IReadOnlyCollection<AssetViewModel> referencedAssets = new List<AssetViewModel>();
        private IReadOnlyCollection<AssetViewModel> recursiveReferencerAssets = new List<AssetViewModel>();
        private IReadOnlyCollection<AssetViewModel> recursiveReferencedAssets = new List<AssetViewModel>();

        public AssetDependenciesViewModel(AssetViewModel asset, bool forcedRoot)
            : base(asset.SafeArgument(nameof(asset)).ServiceProvider)
        {
            Asset = asset;
            ToggleIsRootOnSelectedAssetCommand = new AnonymousCommand(ServiceProvider, () => IsRoot = !IsRoot);
            ForcedRoot = forcedRoot;
            DirtyDependencies.Add(asset);
        }

        /// <summary>
        /// Gets the asset related to this view model.
        /// </summary>
        public AssetViewModel Asset { get; }

        /// <summary>
        /// Gets the session containing the related asset.
        /// </summary>
        public SessionViewModel Session => Asset.Session;

        /// <summary>
        /// Gets the collection of assets that directly references the related asset.
        /// </summary>
        /// <remarks>This collection is updated asynchronously, however it is always up-to-date when <see cref="SessionViewModel.AssetPropertiesChanged"/> is raised.</remarks>
        public IReadOnlyCollection<AssetViewModel> ReferencerAssets { get { return referencerAssets; } private set { SetValue(ref referencerAssets, value); } }

        /// <summary>
        /// Gets the collection of assets directly referenced by the related asset.
        /// </summary>
        /// <remarks>This collection is updated asynchronously, however it is always up-to-date when <see cref="SessionViewModel.AssetPropertiesChanged"/> is raised.</remarks>
        public IReadOnlyCollection<AssetViewModel> ReferencedAssets { get { return referencedAssets; } private set { SetValue(ref referencedAssets, value); } }

        /// <summary>
        /// Gets the collection of assets that references the related asset directly or indirectly.
        /// </summary>
        /// <remarks>This collection is updated asynchronously, however it is always up-to-date when <see cref="SessionViewModel.AssetPropertiesChanged"/> is raised.</remarks>
        public IReadOnlyCollection<AssetViewModel> RecursiveReferencerAssets { get { return recursiveReferencerAssets; } private set { SetValue(ref recursiveReferencerAssets, value); } }

        /// <summary>
        /// Gets the collection of assets referenced by the related asset directly or indirectly.
        /// </summary>
        /// <remarks>This collection is updated asynchronously, however it is always up-to-date when <see cref="SessionViewModel.AssetPropertiesChanged"/> is raised.</remarks>
        public IReadOnlyCollection<AssetViewModel> RecursiveReferencedAssets { get { return recursiveReferencedAssets; } private set { SetValue(ref recursiveReferencedAssets, value); } }

        /// <summary>
        /// Gets whether this asset and all its references will be compiled.
        /// </summary>
        public bool IsRoot
        {
            get { return !Asset.IsDeleted && (Session.CurrentProject?.IsInScope(Asset) ?? false) && (ForcedRoot || (Session.CurrentProject?.RootAssets.Contains(Asset) ?? false)); }
            set
            {
                if ((Session.CurrentProject?.IsInScope(Asset) ?? false) && !ForcedRoot)
                {
                    if (value)
                        Session.CurrentProject.RootAssets.Add(Asset);
                    else
                        Session.CurrentProject.RootAssets.Remove(Asset);
                }
            }
        }

        /// <summary>
        /// Gets whether this asset will be compiled as a dependency of an asset that has <see cref="IsRoot"/> set to <c>true</c>.
        /// </summary>
        public bool IsIndirectlyIncluded => !IsRoot && !Asset.IsDeleted && RecursiveReferencerAssets.Any(x => x.Dependencies.IsRoot);

        /// <summary>
        /// Gets whether this asset will be excluded from compilation.
        /// </summary>
        public bool IsExcluded => !IsRoot && !IsIndirectlyIncluded;

        /// <summary>
        /// Gets whether this asset is forced to be a root asset.
        /// </summary>
        public bool ForcedRoot { get; }

        /// <summary>
        /// Gets a command that will toggle the <see cref="IsRoot"/> property.
        /// </summary>
        public ICommandBase ToggleIsRootOnSelectedAssetCommand { get; }

        internal static Task TriggerInitialReferenceBuild(SessionViewModel session) => NotifyAssetChanged(session, null);

        internal static Task NotifyAssetChanged(SessionViewModel session, AssetViewModel asset)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            lock (DirtyDependencies)
            {
                if (asset != null)
                {
                    DirtyDependencies.Add(asset);
                }

                // If a task of updating dependencies is already running, then we should return it - this asset will be included into it.
                var task = dependenciesUpdated;
                if (task != null)
                    return task.Task;

                dependenciesUpdated = new TaskCompletionSource<int>();
            }

            // Trigger the update for the next dispatcher frame
            return session.Dispatcher.InvokeAsync(() => UpdateReferences(session));
        }

        internal void NotifyRootAssetChange(bool notifyReferencedAssets)
        {
            OnPropertyChanging(nameof(IsRoot), nameof(IsIndirectlyIncluded), nameof(IsExcluded));
            OnPropertyChanged(nameof(IsRoot), nameof(IsIndirectlyIncluded), nameof(IsExcluded));
            if (notifyReferencedAssets)
            {
                foreach (var referencedAsset in RecursiveReferencedAssets)
                {
                    referencedAsset.Dependencies.OnPropertyChanging(nameof(IsIndirectlyIncluded), nameof(IsExcluded));
                    referencedAsset.Dependencies.OnPropertyChanged(nameof(IsIndirectlyIncluded), nameof(IsExcluded));
                }
            }
        }

        private static void UpdateReferences(SessionViewModel session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            var dirtyReferencers = new HashSet<AssetViewModel>();
            var dirtyAssets = new HashSet<AssetViewModel>();
            var dirtyReferenced = new HashSet<AssetViewModel>();
            var dirtyDirectReferenced = new HashSet<AssetViewModel>();
            var referencerAssets = new HashSet<AssetViewModel>();
            var referencedAssets = new HashSet<AssetViewModel>();
            TaskCompletionSource<int> tcs;
            lock (DirtyDependencies)
            {

                foreach (var asset in DirtyDependencies)
                {
                    // Add dirty dependencies from the previous reference values
                    dirtyReferencers.AddRange(asset.Dependencies.RecursiveReferencerAssets);
                    dirtyReferenced.AddRange(asset.Dependencies.RecursiveReferencedAssets);
                    dirtyDirectReferenced.AddRange(asset.Dependencies.ReferencedAssets);

                    referencerAssets.Clear();
                    referencedAssets.Clear();

                    if (!asset.IsDeleted)
                    {
                        var dependencyManager = session.DependencyManager;
                        var dependencies = dependencyManager.ComputeDependencies(asset.AssetItem.Id, AssetDependencySearchOptions.In | AssetDependencySearchOptions.Out, ContentLinkType.Reference); // TODO: Change ContentLinkType.Reference to handle other types
                        if (dependencies != null)
                        {
                            dependencies.LinksIn.Select(x => session.GetAssetById(x.Item.Id)).NotNull().ForEach(x => referencerAssets.Add(x));
                            dependencies.LinksOut.Select(x => session.GetAssetById(x.Item.Id)).NotNull().ForEach(x => referencedAssets.Add(x));
                        }
                        dirtyAssets.Add(asset);

                        // Add dirty dependencies from the updated reference values
                        dirtyReferencers.AddRange(referencerAssets);
                        dirtyReferenced.AddRange(referencedAssets);
                        dirtyDirectReferenced.AddRange(referencedAssets);
                    }

                    // Note: the collections can be empty. This is especially needed when reimporting - we want to be sure that references are cleared.
                    asset.Dependencies.ReferencerAssets = referencerAssets.ToList();
                    asset.Dependencies.ReferencedAssets = referencedAssets.ToList();
                }
                DirtyDependencies.Clear();

                // Clear the task now that we processed all the dirty dependencies
                tcs = dependenciesUpdated;
                dependenciesUpdated = null;
            }

            dirtyDirectReferenced.ExceptWith(dirtyAssets);
            // Add the dirty assets to the list of asset that needs to update recursive referenced assets.
            dirtyReferencers.AddRange(dirtyAssets);
            // Imported/undeleted assets must update their recursive referencers
            dirtyReferenced.AddRange(dirtyAssets);

            // Update the referencers of the (previous/updated) directly referenced assets
            foreach (var asset in dirtyDirectReferenced)
            {
                var dependencyManager = session.DependencyManager;
                var dependencies = dependencyManager.ComputeDependencies(asset.AssetItem.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference); // TODO: Change ContentLinkType.Reference to handle other types
                referencerAssets.Clear();
                dependencies?.LinksIn.Select(x => session.GetAssetById(x.Item.Id)).NotNull().ForEach(x => referencerAssets.Add(x));
                asset.Dependencies.ReferencerAssets = referencerAssets.ToList();
            }

            // Update recursive lists of referenced/referenced assets for assets affected by the changes
            foreach (var asset in dirtyReferenced)
            {
                asset.Dependencies.OnPropertyChanging(nameof(IsIndirectlyIncluded), nameof(IsExcluded));
                asset.Dependencies.UpdateRecursiveReferencerAssets();
                asset.Dependencies.OnPropertyChanged(nameof(IsIndirectlyIncluded), nameof(IsExcluded));
            }
            foreach (var asset in dirtyReferencers)
            {
                asset.Dependencies.UpdateRecursiveReferencedAssets();
            }
            foreach (var asset in dirtyAssets)
            {
                asset.Dependencies.OnPropertyChanging(nameof(IsIndirectlyIncluded), nameof(IsExcluded));
                asset.Dependencies.OnPropertyChanged(nameof(IsIndirectlyIncluded), nameof(IsExcluded));
            }

            // Job is completed, notify anything awaiting on it
            tcs.SetResult(0);
        }

        private void UpdateRecursiveReferencerAssets()
        {
            var result = new HashSet<AssetViewModel>();
            var dependenciesToProcess = new Stack<AssetDependenciesViewModel>();
            dependenciesToProcess.Push(this);
            while (dependenciesToProcess.Count > 0)
            {
                var dependencies = dependenciesToProcess.Pop();
                foreach (var referencer in dependencies.ReferencerAssets)
                {
                    if (!result.Contains(referencer))
                    {
                        result.Add(referencer);
                        dependenciesToProcess.Push(referencer.Dependencies);
                    }
                }
            }
            RecursiveReferencerAssets = result.ToList();
        }

        private void UpdateRecursiveReferencedAssets()
        {
            var result = new HashSet<AssetViewModel>();
            var dependenciesToProcess = new Stack<AssetDependenciesViewModel>();
            dependenciesToProcess.Push(this);
            while (dependenciesToProcess.Count > 0)
            {
                var dependencies = dependenciesToProcess.Pop();
                foreach (var referenced in dependencies.ReferencedAssets.Where(x => !result.Contains(x)))
                {
                    result.Add(referenced);
                    dependenciesToProcess.Push(referenced.Dependencies);
                }
            }
            RecursiveReferencedAssets = result.ToList();
        }
    }
}
