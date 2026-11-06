// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The asset picker: the assets of the session that the caller accepts, narrowed to a location (a package or one of its folders)
/// and to the ones whose name contains a search text.
/// </summary>
public sealed class AssetPickerViewModel : DispatcherViewModel
{
    private readonly SessionViewModel session;
    private string searchText = string.Empty;
    private AssetViewModel? selectedAsset;
    private object? selectedLocation;

    /// <param name="session">The session whose assets are listed.</param>
    /// <param name="acceptedTypes">The asset types to list (and their derived types); empty lists every type.</param>
    /// <param name="filter">An additional condition on the listed assets, or <c>null</c>.</param>
    /// <param name="initialAsset">The asset selected when the picker opens (the current value of a reference), or <c>null</c>.</param>
    /// <param name="initialLocation">The folder selected when the picker opens and there is no initial asset, or <c>null</c> for every location.</param>
    public AssetPickerViewModel(SessionViewModel session, IEnumerable<Type> acceptedTypes, Func<AssetViewModel, bool>? filter = null, AssetViewModel? initialAsset = null, DirectoryBaseViewModel? initialLocation = null)
        : base(session.SafeArgument(nameof(session)).ServiceProvider)
    {
        this.session = session;
        AcceptedTypes = acceptedTypes.ToList();
        Filter = filter;
        selectedLocation = initialAsset?.Directory ?? initialLocation;
        RefreshFilteredAssets();
        if (initialAsset is not null && FilteredAssets.Contains(initialAsset))
            selectedAsset = initialAsset;
    }

    /// <summary>
    /// Gets the asset types the picker lists (and their derived types); empty lists every type.
    /// </summary>
    public IReadOnlyList<Type> AcceptedTypes { get; }

    /// <summary>
    /// Gets or sets a text shown above the assets (what the asset is picked for), or <c>null</c>.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets the additional condition on the listed assets, or <c>null</c>.
    /// </summary>
    public Func<AssetViewModel, bool>? Filter { get; }

    /// <summary>
    /// Gets the packages of the session, the roots of the location tree: their children are the folders of their assets.
    /// </summary>
    public IEnumerable<PackageViewModel> Packages => session.LocalPackages.Concat(session.StorePackages);

    /// <summary>
    /// Gets or sets the text that the name of a listed asset contains (case-insensitive); empty lists every name.
    /// </summary>
    public string SearchText
    {
        get => searchText;
        set
        {
            if (SetValue(ref searchText, value ?? string.Empty))
                RefreshFilteredAssets();
        }
    }

    /// <summary>
    /// Gets or sets the selected location: a <see cref="PackageViewModel"/> lists the assets of the package, a
    /// <see cref="DirectoryBaseViewModel"/> the assets of the folder and its sub-folders, <c>null</c> the assets of the session.
    /// </summary>
    public object? SelectedLocation
    {
        get => selectedLocation;
        set
        {
            if (SetValue(ref selectedLocation, value))
                RefreshFilteredAssets();
        }
    }

    /// <summary>
    /// Gets or sets the selected asset, the result of the picker when it is validated.
    /// </summary>
    public AssetViewModel? SelectedAsset
    {
        get => selectedAsset;
        set => SetValue(ref selectedAsset, value);
    }

    /// <summary>
    /// Gets the assets that match the accepted types, the filter, the location and the search text, sorted by name.
    /// </summary>
    public ObservableList<AssetViewModel> FilteredAssets { get; } = [];

    /// <summary>
    /// Whether the picker accepts an asset, regardless of the location and the search text.
    /// </summary>
    public bool IsAccepted(AssetViewModel asset)
    {
        if (AcceptedTypes.Count > 0 && !AcceptedTypes.Any(x => x.IsAssignableFrom(asset.AssetType)))
            return false;
        return Filter?.Invoke(asset) ?? true;
    }

    private void RefreshFilteredAssets()
    {
        var assets = session.AllAssets.Where(x => IsAccepted(x) && IsInLocation(x));
        if (!string.IsNullOrWhiteSpace(searchText))
            assets = assets.Where(x => x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        FilteredAssets.Clear();
        FilteredAssets.AddRange(assets.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase));
        if (selectedAsset is not null && !FilteredAssets.Contains(selectedAsset))
            SelectedAsset = null;
    }

    private bool IsInLocation(AssetViewModel asset)
    {
        switch (selectedLocation)
        {
            case PackageViewModel package:
                return asset.Directory?.Package == package;
            case DirectoryBaseViewModel directory:
                for (var current = asset.Directory; current is not null; current = current.Parent)
                {
                    if (current == directory)
                        return true;
                }
                return false;
            default:
                return true;
        }
    }
}
