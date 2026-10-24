// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

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
    private readonly HashSet<AssetViewModel> referencerAssets = [];
    /// <summary>
    /// The collection of referencees for the current selection of assets.
    /// </summary>
    private readonly HashSet<AssetViewModel> referencedAssets = [];
    private bool showReferencers;
    private string typeCountersAsText = "";

    public ReferencesViewModel(SessionViewModel session)
        : base(session.SafeArgument().ServiceProvider)
    {
        assetCollection = session.AssetCollection;
        DisplayedReferences = new AssetCollectionViewModel(session);

        assetCollection.SelectedAssets.CollectionChanged += (_, _) => RefreshReferences();
        session.AssetPropertiesChanged += (_, _) => Dispatcher.Invoke(RefreshReferences);
    }

    /// <summary>
    /// Gets the <see cref="AssetCollectionViewModel"/> that should be currently displayed according to other properties values.
    /// </summary>
    public AssetCollectionViewModel DisplayedReferences { get; }

    /// <summary>
    /// Gets or sets whether to show the referencers of the selection of assets. If <c>false</c>, the referenced assets will be displayed instead.
    /// </summary>
    public bool ShowReferencers
    {
        get => showReferencers;
        set => SetValue(ref showReferencers, value, UpdateDisplayedContent);
    }

    /// <summary>
    /// Gets the counter of asset references grouped by types.
    /// </summary>
    public string TypeCountersAsText
    {
        get => typeCountersAsText;
        private set => SetValue(ref typeCountersAsText, value);
    }

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

        // special case
        return string.Equals(word, "entity", StringComparison.OrdinalIgnoreCase)
            ? $"{word[..^1]}es"
            : $"{word}s";
    }
}
