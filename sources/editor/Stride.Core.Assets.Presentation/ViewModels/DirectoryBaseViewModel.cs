// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class DirectoryBaseViewModel : SessionObjectViewModel
{
    private readonly AutoUpdatingSortedObservableCollection<DirectoryViewModel> subDirectories = new(CompareDirectories);

    protected DirectoryBaseViewModel(ISessionViewModel session)
        : base(session)
    {
        SubDirectories = new ReadOnlyObservableCollection<DirectoryViewModel>(subDirectories);
    }

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

    private static int CompareDirectories(DirectoryViewModel? x, DirectoryViewModel? y)
    {
        if (x == null && y == null)
            return 0;

        if (x != null && y != null)
            return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        return x == null ? -1 : 1;
    }
}
