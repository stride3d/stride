// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class AssetFilterViewModel : DispatcherViewModel, IEquatable<AssetFilterViewModel>
{
    private bool isActive;
    private bool isReadOnly;

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

    public FilterCategory Category { get; }

    public string DisplayName { get; }

    public string Filter { get; }

    public bool IsActive { get => isActive; set => SetValue(ref isActive, value); }

    /// <summary>
    /// Gets or sets whether the user can remove or deactivate this filter (a picker can force a type filter).
    /// </summary>
    public bool IsReadOnly
    {
        get => isReadOnly;
        set => SetValue(ref isReadOnly, value, () =>
        {
            RemoveFilterCommand.IsEnabled = !value;
            ToggleIsActiveCommand.IsEnabled = !value;
        });
    }

    public ICommandBase RemoveFilterCommand { get; }

    public ICommandBase ToggleIsActiveCommand { get; }

    public bool Match(AssetViewModel asset)
    {
        return Category switch
        {
            FilterCategory.AssetName => ComputeTokens(Filter).All(t => asset.Name.Contains(t, StringComparison.OrdinalIgnoreCase)),
            FilterCategory.AssetTag => asset.Tags.Any(y => y.Contains(Filter, StringComparison.OrdinalIgnoreCase)),
            FilterCategory.AssetType => string.Equals(asset.AssetType.FullName, Filter),
            _ => false,
        };
    }

    internal static string[] ComputeTokens(string? pattern) => pattern?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];

    public bool Equals(AssetFilterViewModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Category == other.Category && string.Equals(Filter, other.Filter, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => obj is AssetFilterViewModel other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Category, StringComparer.OrdinalIgnoreCase.GetHashCode(Filter));

    public static bool operator ==(AssetFilterViewModel? left, AssetFilterViewModel? right) => Equals(left, right);

    public static bool operator !=(AssetFilterViewModel? left, AssetFilterViewModel? right) => !Equals(left, right);
}
