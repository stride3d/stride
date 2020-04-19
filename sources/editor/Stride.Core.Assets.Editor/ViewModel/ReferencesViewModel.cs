// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// A view model that represents the referencers and referencees of a selection of asset.
    /// </summary>
    public sealed class ReferencesViewModel : DispatcherViewModel
    {
        /// <summary>
        /// The asset collection view model of assets for which we want to gather references.
        /// </summary>
        private readonly AssetCollectionViewModel assetCollection;

        /// <summary>
        /// The collection of referencers for the current selection of assets.
        /// </summary>
        private readonly HashSet<AssetViewModel> referencerAssets = new HashSet<AssetViewModel>();

        /// <summary>
        /// The collection of referencees for the current selection of assets.
        /// </summary>
        private readonly HashSet<AssetViewModel> referencedAssets = new HashSet<AssetViewModel>();

        /// <summary>
        /// Backing field for the <see cref="ShowReferencers"/> property.
        /// </summary>
        private bool showReferencers;
        /// <summary>
        /// Backing field for the <see cref="TypeCountersAsText"/> property.
        /// </summary>
        private string typeCountersAsText;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferencesViewModel"/> class.
        /// </summary>
        /// <param name="session">The session view model.</param>
        /// <param name="assetCollection">The asset collection view model of assets for which we want to gather references.</param>
        public ReferencesViewModel(SessionViewModel session, AssetCollectionViewModel assetCollection)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            this.assetCollection = assetCollection;
            DisplayedReferences = new AssetCollectionViewModel(session.ServiceProvider, session, AssetCollectionViewModel.AllFilterCategories);

            session.ActiveAssetView.SelectedAssets.CollectionChanged += (sender, e) => RefreshReferences();
            session.AssetPropertiesChanged += (sender, e) => Dispatcher.InvokeAsync(RefreshReferences);
        }

        /// <summary>
        /// Gets the <see cref="AssetCollectionViewModel"/> that should be currently displayed according to other properties values.
        /// </summary>
        public AssetCollectionViewModel DisplayedReferences { get; }

        /// <summary>
        /// Gets or sets whether to show the referencers of the selection of assets. If <c>false</c>, the referenced assets will be displayed instead.
        /// </summary>
        public bool ShowReferencers { get { return showReferencers; } set { SetValue(ref showReferencers, value, UpdateDisplayedContent); } }
        
        /// <summary>
        /// Gets the counter of asset references grouped by types.
        /// </summary>
        public string TypeCountersAsText { get { return typeCountersAsText; } private set { SetValue(ref typeCountersAsText, value); } }

        /// <summary>
        /// Rebuilds the references collections from the current selection in the asset view model collection passed to the constructor of this instance.
        /// </summary>
        private void RefreshReferences()
        {
            Dispatcher.EnsureAccess();

            var referencers = assetCollection.SelectedAssets.SelectMany(x => x.Dependencies.ReferencerAssets);
            referencerAssets.Clear();
            referencerAssets.AddRange(referencers);

            var referenced = AssetViewModel.ComputeRecursiveReferencedAssets(assetCollection.SelectedAssets);
            referencedAssets.Clear();
            referencedAssets.AddRange(referenced);

            UpdateDisplayedContent();
        }

        /// <summary>
        /// Updates the <see cref="DisplayedReferences"/> collection.
        /// </summary>
        private void UpdateDisplayedContent()
        {
            var assets = ShowReferencers ? referencerAssets : referencedAssets;

            DisplayedReferences.UpdateAssetsCollection(assets);
            UpdateStats(assets);
        }

        /// <summary>
        /// Updates the <see cref="TypeCountersAsText"/> property.
        /// </summary>
        /// <param name="assets"></param>
        private void UpdateStats(IEnumerable<AssetViewModel> assets)
        {
            var typeCounters = assets.GroupBy(a => a.TypeDisplayName).Select(grp =>
            {
                var count = grp.Count();
                return $"{count} {Pluralize(grp.Key, count)}";
            });
            TypeCountersAsText = string.Join(", ", typeCounters);
        }

        private static string Pluralize(string word, int count)
        {
            if (count == 1)
                return word;
            // exceptions
            if (word.ToUpperInvariant() == "ENTITY")
            {
                return $"{word.Substring(0, word.Length - 1)}es";
            }
            return $"{word}s";
        }
    }
}
