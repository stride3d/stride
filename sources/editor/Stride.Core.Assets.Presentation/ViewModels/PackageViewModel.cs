// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public class PackageViewModel : SessionObjectViewModel, IComparable<PackageViewModel>
{
    // FIXME should only contain editable viewmodels
    protected readonly SortedObservableCollection<ViewModelBase> content = new(ComparePackageContent);

    public PackageViewModel(ISessionViewModel session, PackageContainer packageContainer)
        : base(session)
    {
        AssetMountPoint = new AssetMountPointViewModel(this);
        PackageContainer = packageContainer;

        content.Add(AssetMountPoint);
    }

    public AssetMountPointViewModel AssetMountPoint { get; }

    public IReadOnlyObservableCollection<ViewModelBase> Content => content;

    public override string Name
    {
        get => PackagePath.GetFileNameWithoutExtension() ?? string.Empty;
        set { } // TODO rename
    }

    /// <summary>
    /// Gets the underlying <see cref="Package"/> used as a model for this view.
    /// </summary>
    public Package Package => PackageContainer.Package;

    public PackageContainer PackageContainer { get; }

    /// <summary>
    /// Gets or sets the path of this package.
    /// </summary>
    /// <remarks>Modifying this property also modify the <see cref="Name"/> property.</remarks>
    public UFile PackagePath
    {
        get => Package.FullPath;
        set => SetValue(() => Package.FullPath = value);
    }

    public UDirectory RootDirectory => Package.RootDirectory;

    /// <inheritdoc/>
    public int CompareTo(PackageViewModel? other)
    {
        return other != null ? string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase) : -1;
    }
    
    private static int ComparePackageContent(ViewModelBase x, ViewModelBase y)
    {
        if (x is AssetMountPointViewModel xAssets)
        {
            if (y is AssetMountPointViewModel yAssets)
                return string.Compare(xAssets.Name, yAssets.Name, StringComparison.InvariantCultureIgnoreCase);
            return -1;
        }
        if (x is ProjectViewModel xProject)
        {
            if (y is ProjectViewModel yProject)
            {
                return xProject.CompareTo(yProject);
            }
            return y is AssetMountPointViewModel ? 1 : -1;
        }
        throw new InvalidOperationException("Unable to sort the given items for the Content collection of PackageViewModel");
    }
}
