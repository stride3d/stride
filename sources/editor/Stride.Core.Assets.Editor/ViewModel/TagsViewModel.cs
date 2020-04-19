// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// This view model represents the union of the tags of a collection of selected assets.
    /// </summary>
    public class TagsViewModel : DispatcherViewModel
    {
        /// <summary>
        /// This view model represents a single tag associated to a collection of assets.
        /// </summary>
        public class TagViewModel : DispatcherViewModel
        {
            private readonly int selectedAssetsCount;
            private readonly TagsViewModel tags;
            private string counter;

            /// <summary>
            /// Initializes a new instance of the <see cref="TagViewModel"/> class.
            /// </summary>
            /// <param name="tags">The tags view model that created this tag.</param>
            /// <param name="name">The name of this tag.</param>
            /// <param name="taggedAssets">The assets who have this flag.</param>
            /// <param name="selectedAssetsCount">The number of assets currently selected, whether or not they have this tag.</param>
            public TagViewModel(TagsViewModel tags, string name, IEnumerable<AssetViewModel> taggedAssets, int selectedAssetsCount)
                : base(tags.SafeArgument(nameof(tags)).ServiceProvider)
            {
                this.tags = tags;
                this.selectedAssetsCount = selectedAssetsCount;
                Name = name;
                // Add assets before monitoring the Assets collection
                Assets.AddRange(taggedAssets);
                Assets.CollectionChanged += CollectionChanged;
                RemoveTagCommand = new AnonymousCommand(ServiceProvider, RemoveTag);
                AddTagToAllCommand = new AnonymousCommand(ServiceProvider, () => tags.AddTagCommand.Execute(Name));
                UpdateCounter();
            }

            /// <summary>
            /// Gets the list of assets that contain this tag.
            /// </summary>
            /// <remarks>The <see cref="ObservableList{AssetViewModel}.Clear"/> method is not supported and will throw an exception.</remarks>
            public ObservableList<AssetViewModel> Assets { get; } = new ObservableList<AssetViewModel>();

            /// <summary>
            /// Gets the name of this tag.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets a string representation of the number of assets that have this tag in the current selection.
            /// </summary>
            public string Counter { get { return counter; } set { SetValue(ref counter, value); } }

            /// <summary>
            /// Gets a command that will remove this tag from all selected assets.
            /// </summary>
            public ICommandBase RemoveTagCommand { get; private set; }

            /// <summary>
            /// Gets a command that will add this tag to all selected assets.
            /// </summary>
            public ICommandBase AddTagToAllCommand { get; private set; }

            private void RemoveTag()
            {
                using (var transaction = tags.UndoRedoService.CreateTransaction())
                {
                    {
                        tags.modifyingTag = true;
                        string message = $"Remove tag '{Name}' from {(selectedAssetsCount > 1 ? $"{selectedAssetsCount} assets" : Assets.First().Url)}";

                        while (Assets.Count > 0)
                            Assets.RemoveAt(Assets.Count - 1);

                        tags.modifyingTag = false;
                        tags.UndoRedoService.SetName(transaction, message);
                    }
                }
            }

            private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Reset) throw new InvalidOperationException("Reset is not supported on this collection.");

                if (e.NewItems != null)
                {
                    foreach (AssetViewModel newItem in e.NewItems)
                    {
                        if (!newItem.Tags.Contains(Name))
                            newItem.Tags.Add(Name);
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (AssetViewModel oldItem in e.OldItems)
                    {
                        oldItem.Tags.Remove(Name);
                    }
                }
                UpdateCounter();
            }

            private void UpdateCounter()
            {
                if (selectedAssetsCount > 1)
                {
                    // NOTE: the " (all)" string is used in the xaml file too, don't forget to update it if you change this string!
                    Counter = Assets.Count == selectedAssetsCount ? " (all)" : $" ({Assets.Count})";
                }
                else
                {
                    Counter = "";
                }
            }
        }

        private readonly AutoUpdatingSortedObservableCollection<TagViewModel> tags = new AutoUpdatingSortedObservableCollection<TagViewModel>(new AnonymousComparer<TagViewModel>((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)));
        private readonly AssetCollectionViewModel assetCollection;
        private bool modifyingTag;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagsViewModel"/> class.
        /// </summary>
        /// <param name="assetCollection">The <see cref="AssetCollectionViewModel"/> that will be used to track the list of selected assets.</param>
        public TagsViewModel(AssetCollectionViewModel assetCollection)
            : base(assetCollection.SafeArgument(nameof(assetCollection)).ServiceProvider)
        {
            this.assetCollection = assetCollection;
            assetCollection.SelectedAssets.CollectionChanged += SelectedAssetsChanged;
            AddTagCommand = new AnonymousCommand<string>(ServiceProvider, AddTag);
        }

        /// <summary>
        /// Gets a sorted collection of the union of all the tags associated to the currently selected assets.
        /// </summary>
        public SortedObservableCollection<TagViewModel> Tags => tags;

        /// <summary>
        /// Gets the action service to use for transactions.
        /// </summary>
        public IUndoRedoService UndoRedoService => assetCollection.Session.UndoRedoService;

        /// <summary>
        /// Gets a command that will add the string given as parameter as a tag for all currently selected assets.
        /// </summary>
        public CommandBase AddTagCommand { get; }

        private void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            var singleAsset = assetCollection.SelectedAssets.First();
            var tagViewModel = Tags.FirstOrDefault(x => x.Name == tag);

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                int assetCount;
                modifyingTag = true;
                if (tagViewModel == null)
                {
                    tagViewModel = new TagViewModel(this, tag, Enumerable.Empty<AssetViewModel>(), assetCollection.SelectedAssets.Count);
                    // We had the assets to the tag after construction to ensure they will create an operation for the undo stack.
                    tagViewModel.Assets.AddRange(assetCollection.SelectedAssets);
                    assetCount = assetCollection.SelectedAssets.Count;
                    Tags.Add(tagViewModel);
                }
                else
                {
                    var assetsToAdd = assetCollection.SelectedAssets.Where(x => !tagViewModel.Assets.Contains(x)).ToList();
                    assetCount = assetsToAdd.Count;
                    tagViewModel.Assets.AddRange(assetsToAdd);
                }

                string message = $"Added tag '{tag}' to {(assetCount > 1 ? $"{assetCount} assets" : singleAsset.Url)}";
                modifyingTag = false;
                UndoRedoService.SetName(transaction, message);
            }
        }

        private void RefreshTags()
        {
            var assetTags = new Dictionary<string, List<AssetViewModel>>();
            foreach (var asset in assetCollection.SelectedAssets)
            {
                foreach (var tag in asset.Tags)
                {
                    List<AssetViewModel> assets;
                    if (!assetTags.TryGetValue(tag, out assets))
                    {
                        assets = new List<AssetViewModel>();
                        assetTags.Add(tag, assets);
                    }
                    assets.Add(asset);
                }
            }
            Tags.Clear();
            foreach (var tag in assetTags)
            {
                Tags.Add(new TagViewModel(this, tag.Key, tag.Value, assetCollection.SelectedAssets.Count));
            }
        }

        private void SelectedAssetsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshTags();

            foreach (var asset in assetCollection.SelectedAssets)
            {
                asset.Tags.CollectionChanged += AssetTagsChanged;
            }
        }

        private void AssetTagsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Refresh tags if the modification is external (undo stack or whatever)
            if (!modifyingTag)
                RefreshTags();
        }
    }
}
