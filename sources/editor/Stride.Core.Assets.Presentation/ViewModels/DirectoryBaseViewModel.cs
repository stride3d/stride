// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class DirectoryBaseViewModel : SessionObjectViewModel
{
    public const string Separator = "/";
    private readonly AutoUpdatingSortedObservableCollection<DirectoryViewModel> subDirectories = new(CompareDirectories);
    private readonly ObservableSet<AssetViewModel> assets = new();

    protected DirectoryBaseViewModel(ISessionViewModel session)
        : base(session)
    {
        SubDirectories = new ReadOnlyObservableCollection<DirectoryViewModel>(subDirectories);
    }

    public IReadOnlyObservableList<AssetViewModel> Assets { get { return assets; } }

    /// <summary>
    /// Gets the package containing this directory.
    /// </summary>
    public abstract PackageViewModel Package { get; }

    /// <summary>
    /// Gets or sets the parent directory of this directory.
    /// </summary>
    public abstract DirectoryBaseViewModel Parent { get; set; }

    /// <summary>
    /// Gets the path of this directory in its current package.
    /// </summary>
    public abstract string Path { get; }

    /// <summary>
    /// Gets the root directory containing this directory, or this directory itself if it is a root directory.
    /// </summary>
    public abstract MountPointViewModel Root { get; }

    /// <summary>
    /// Gets the read-only collection of sub-directories contained in this directory.
    /// </summary>
    public ReadOnlyObservableCollection<DirectoryViewModel> SubDirectories { get; }

    /// <summary>
    /// Retrieves the directory corresponding to the given path.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>The directory corresponding to the given path if found, otherwise <c>null</c>.</returns>
    /// <remarks>The path should correspond to a directory, not an asset.</remarks>
    public DirectoryBaseViewModel? GetDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var directoryNames = path.Split(Separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        DirectoryBaseViewModel? currentDirectory = this;
        foreach (var directoryName in directoryNames)
        {
            currentDirectory = currentDirectory.SubDirectories.FirstOrDefault(x => string.Equals(directoryName, x.Name, StringComparison.InvariantCultureIgnoreCase));
            if (currentDirectory is null)
                return null;
        }
        return currentDirectory;
    }

    public IReadOnlyCollection<DirectoryBaseViewModel> GetDirectoryHierarchy()
    {
        var hierarchy = new List<DirectoryBaseViewModel> { this };
        hierarchy.AddRange(SubDirectories.DepthFirst(x => x.SubDirectories));
        return hierarchy;
    }

    public DirectoryBaseViewModel GetOrCreateDirectory(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        DirectoryBaseViewModel result = this;
        if (!string.IsNullOrEmpty(path))
        {
            var directoryNames = path.Split(Separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            result = directoryNames.Aggregate(result, (current, next) => current.SubDirectories.FirstOrDefault(x => string.Equals(next, x.Name, StringComparison.InvariantCultureIgnoreCase)) ?? new DirectoryViewModel(next, current));
        }
        return result;
    }

    internal void AddAsset(AssetViewModel asset, bool canUndoRedo)
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

    internal void RemoveAsset(AssetViewModel asset)
    {
        assets.Remove(asset);
    }

    private static int CompareDirectories(DirectoryViewModel? x, DirectoryViewModel? y)
    {
        if (x == null && y == null)
            return 0;

        if (x != null && y != null)
            return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        return x == null ? -1 : 1;
    }
}
