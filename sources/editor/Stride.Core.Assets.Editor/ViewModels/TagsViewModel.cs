// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// This view model represents the union of the tags of a collection of selected assets (the tags of the property grid's header).
/// </summary>
public sealed class TagsViewModel : DispatcherViewModel
{
    /// <summary>
    /// The text of <see cref="TagViewModel.Counter"/> when every selected asset has the tag.
    /// </summary>
    public const string AllCounter = " (all)";

    private readonly AutoUpdatingSortedObservableCollection<TagViewModel> tags = new(new AnonymousComparer<TagViewModel>((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)));
    private readonly HashSet<AssetViewModel> monitoredAssets = [];
    private readonly AssetCollectionViewModel assetCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagsViewModel"/> class.
    /// </summary>
    /// <param name="assetCollection">The <see cref="AssetCollectionViewModel"/> that will be used to track the list of selected assets.</param>
    public TagsViewModel(AssetCollectionViewModel assetCollection)
        : base(assetCollection.ServiceProvider)
    {
        this.assetCollection = assetCollection;
        assetCollection.SelectedAssets.CollectionChanged += SelectedAssetsChanged;
        AddTagCommand = new AnonymousCommand<string?>(ServiceProvider, AddTag);
    }

    /// <summary>
    /// Gets a sorted collection of the union of all the tags associated to the currently selected assets.
    /// </summary>
    public SortedObservableCollection<TagViewModel> Tags => tags;

    /// <summary>
    /// Gets or sets whether a tag command is changing the tags of the assets, so that the change doesn't rebuild the tags.
    /// </summary>
    internal bool ModifyingTag { get; set; }

    /// <summary>
    /// Gets the action service to use for transactions.
    /// </summary>
    public IUndoRedoService UndoRedoService => assetCollection.Session.UndoRedoService;

    /// <summary>
    /// Gets a command that will add the string given as parameter as a tag for all currently selected assets.
    /// </summary>
    public ICommandBase AddTagCommand { get; }

    /// <inheritdoc/>
    public override void Destroy()
    {
        assetCollection.SelectedAssets.CollectionChanged -= SelectedAssetsChanged;
        MonitorAssets([]);
        base.Destroy();
    }

    private void AddTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || assetCollection.SelectedAssets.Count == 0)
            return;

        var tagViewModel = Tags.FirstOrDefault(x => x.Name == tag);

        using var transaction = UndoRedoService.CreateTransaction();
        List<AssetViewModel> assetsToAdd;
        ModifyingTag = true;
        if (tagViewModel == null)
        {
            tagViewModel = new TagViewModel(this, tag, [], assetCollection.SelectedAssets.Count);
            // We add the assets to the tag after construction to ensure they will create an operation for the undo stack.
            assetsToAdd = assetCollection.SelectedAssets.ToList();
            tagViewModel.Assets.AddRange(assetsToAdd);
            Tags.Add(tagViewModel);
        }
        else
        {
            assetsToAdd = assetCollection.SelectedAssets.Where(x => !tagViewModel.Assets.Contains(x)).ToList();
            tagViewModel.Assets.AddRange(assetsToAdd);
        }

        // WPF named the first selected asset, even when the tag went to another one (Add tag to all)
        var message = $"Added tag '{tag}' to {(assetsToAdd.Count == 1 ? assetsToAdd[0].Url : $"{assetsToAdd.Count} assets")}";
        ModifyingTag = false;
        UndoRedoService.SetName(transaction, message);
    }

    private void RefreshTags()
    {
        var assetTags = new Dictionary<string, List<AssetViewModel>>();
        foreach (var asset in assetCollection.SelectedAssets)
        {
            foreach (var tag in asset.Tags)
            {
                if (!assetTags.TryGetValue(tag, out var assets))
                {
                    assets = [];
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

    private void MonitorAssets(IReadOnlyCollection<AssetViewModel> assets)
    {
        // The WPF view model subscribed again on each selection change and never unsubscribed; follow only the selected assets
        foreach (var asset in monitoredAssets.Where(x => !assets.Contains(x)).ToList())
        {
            asset.Tags.CollectionChanged -= AssetTagsChanged;
            monitoredAssets.Remove(asset);
        }
        foreach (var asset in assets.Where(monitoredAssets.Add))
        {
            asset.Tags.CollectionChanged += AssetTagsChanged;
        }
    }

    private void SelectedAssetsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshTags();
        MonitorAssets(assetCollection.SelectedAssets.ToList());
    }

    private void AssetTagsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Refresh tags if the modification is external (undo stack or whatever)
        if (!ModifyingTag)
            RefreshTags();
    }
}

/// <summary>
/// This view model represents a single tag associated to a collection of assets.
/// </summary>
public sealed class TagViewModel : DispatcherViewModel
{
    private readonly int selectedAssetsCount;
    private readonly TagsViewModel tags;
    private string counter = string.Empty;
    private bool canAddToAll;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagViewModel"/> class.
    /// </summary>
    /// <param name="tags">The tags view model that created this tag.</param>
    /// <param name="name">The name of this tag.</param>
    /// <param name="taggedAssets">The assets who have this flag.</param>
    /// <param name="selectedAssetsCount">The number of assets currently selected, whether or not they have this tag.</param>
    public TagViewModel(TagsViewModel tags, string name, IEnumerable<AssetViewModel> taggedAssets, int selectedAssetsCount)
        : base(tags.ServiceProvider)
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
    public ObservableList<AssetViewModel> Assets { get; } = [];

    /// <summary>
    /// Gets the name of this tag.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a string representation of the number of assets that have this tag in the current selection.
    /// </summary>
    public string Counter { get => counter; private set => SetValue(ref counter, value); }

    /// <summary>
    /// Gets whether some selected assets don't have this tag yet, so that <see cref="AddTagToAllCommand"/> has something to do.
    /// </summary>
    public bool CanAddToAll { get => canAddToAll; private set => SetValue(ref canAddToAll, value); }

    /// <summary>
    /// Gets a command that will remove this tag from all selected assets.
    /// </summary>
    public ICommandBase RemoveTagCommand { get; }

    /// <summary>
    /// Gets a command that will add this tag to all selected assets.
    /// </summary>
    public ICommandBase AddTagToAllCommand { get; }

    private void RemoveTag()
    {
        if (Assets.Count == 0)
            return;

        using var transaction = tags.UndoRedoService.CreateTransaction();
        tags.ModifyingTag = true;
        var message = $"Remove tag '{Name}' from {(selectedAssetsCount > 1 ? $"{selectedAssetsCount} assets" : Assets.First().Url)}";

        while (Assets.Count > 0)
            Assets.RemoveAt(Assets.Count - 1);

        tags.ModifyingTag = false;
        tags.UndoRedoService.SetName(transaction, message);
    }

    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
            Counter = Assets.Count == selectedAssetsCount ? TagsViewModel.AllCounter : $" ({Assets.Count})";
        }
        else
        {
            Counter = "";
        }
        CanAddToAll = selectedAssetsCount > 1 && Assets.Count < selectedAssetsCount;
    }
}
