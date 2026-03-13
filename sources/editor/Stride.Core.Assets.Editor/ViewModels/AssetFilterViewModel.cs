// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// View model information for an asset filter.
/// </summary>
public sealed class AssetFilterViewModel : DispatcherViewModel, IEquatable<AssetFilterViewModel>
{
    /// <summary>
    /// View model information for an asset filter.
    /// </summary>
    /// <param name="collection">The parent view model that contains the asset filter.</param>
    /// <param name="category">The filter category.</param>
    /// <param name="filter">The filter string.</param>
    /// <param name="displayName">The filter display name.</param>
    public AssetFilterViewModel(AssetCollectionViewModel collection, FilterCategory category, string filter, string displayName)
        : base(collection.ServiceProvider)
    {
        Category = category;
        DisplayName = displayName;
        Filter = filter;
        isActive = true;

        RemoveFilterCommand = new AnonymousCommand<AssetFilterViewModel>(ServiceProvider, collection.RemoveAssetFilter);
        ToggleIsActiveCommand = new AnonymousCommand(ServiceProvider, () => IsActive = !IsActive);
    }

    /// <summary>
    /// The filter category. See <see cref="ViewModels.FilterCategory"/>
    /// </summary>
    public FilterCategory Category { get; }

    /// <summary>
    /// The filter's display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The filter's value.
    /// </summary>
    public string Filter { get; }

    private bool isActive;

    /// <summary>
    /// Whether the filter is enabled.
    /// </summary>
    public bool IsActive { get => isActive; set => SetValue(ref isActive, value); }
    
    /// <summary>
    /// Removes itself from the parent. See <see cref="AssetCollectionViewModel.RemoveAssetFilter"/>
    /// </summary>
    public ICommandBase RemoveFilterCommand { get; }

    /// <summary>
    /// Toggles <see cref="IsActive"/>.
    /// </summary>
    public ICommandBase ToggleIsActiveCommand { get; }

    /// <summary>
    /// Checks if the filter matches the asset.
    /// </summary>
    /// <param name="asset">The asset to check.</param>
    /// <returns>Whether the filter is active.</returns>
    public bool Match(AssetViewModel asset)
    {
        return Category switch
        {
            FilterCategory.AssetName => ComputeTokens(Filter).All(t => asset.Name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0),
            FilterCategory.AssetTag => asset.Tags.Any(y => y.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0),
            FilterCategory.AssetType => string.Equals(asset.AssetType.FullName, Filter),
            _ => false,
        };
    }

    private static string[] ComputeTokens(string pattern) => pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Checks if an asset filter has the same string pattern and category.
    /// </summary>
    /// <param name="other">The asset filter to check.</param>
    /// <returns>Whether the asset filter is functionally identical.</returns>
    public bool Equals(AssetFilterViewModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Category == other.Category && string.Equals(Filter, other.Filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AssetFilterViewModel other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        unchecked(((int)Category * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(Filter));

    /// <summary>
    /// See <see cref="Equals(Stride.Core.Assets.Editor.ViewModels.AssetFilterViewModel?)"/>.
    /// </summary>
    public static bool operator ==(AssetFilterViewModel? left, AssetFilterViewModel? right) => Equals(left, right);

    /// <summary>
    /// See <see cref="Equals(Stride.Core.Assets.Editor.ViewModels.AssetFilterViewModel?)"/>.
    /// </summary>
    public static bool operator !=(AssetFilterViewModel? left, AssetFilterViewModel? right) => !Equals(left, right);
}
