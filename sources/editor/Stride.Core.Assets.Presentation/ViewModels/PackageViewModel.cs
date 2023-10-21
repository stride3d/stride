// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public class PackageViewModel : SessionObjectViewModel, IComparable<PackageViewModel>
{
    // FIXME should only contain editable viewmodels
    protected readonly SortedObservableCollection<ViewModelBase> content = new(ComparePackageContent);

    protected readonly ObservableSet<AssetViewModel> rootAssets = new();

    public PackageViewModel(ISessionViewModel session, PackageContainer packageContainer)
        : base(session)
    {
        AssetMountPoint = new AssetMountPointViewModel(this);
        PackageContainer = packageContainer;

        content.Add(AssetMountPoint);
    }

    /// <summary>
    /// Gets the root directory of this package.
    /// </summary>
    public AssetMountPointViewModel AssetMountPoint { get; }

    /// <summary>
    /// Gets all assets contained in this package.
    /// </summary>
    public IEnumerable<AssetViewModel> Assets => MountPoints.SelectMany(x => x.GetDirectoryHierarchy().SelectMany(y => y.Assets));

    /// <summary>
    /// Gets the list of child item to be used to display in a hierachical view.
    /// </summary>
    /// <remarks>This collection usually contains categories and root folders.</remarks>
    public IReadOnlyObservableCollection<ViewModelBase> Content => content;

    public IEnumerable<MountPointViewModel> MountPoints => Content.OfType<MountPointViewModel>();

    /// <summary>
    /// Gets or sets the name of this package.
    /// </summary>
    /// <remarks>Modifying this property also modify the <see cref="PackagePath"/> property if the package has already been saved once.</remarks>
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

    public DirectoryBaseViewModel GetOrCreateAssetDirectory(string assetDirectory)
    {
        return AssetMountPoint.GetOrCreateDirectory(assetDirectory);
    }

    /// <summary>
    /// Creates the view models for each asset, directory, profile, project and reference of this package.
    /// </summary>
    /// <param name="token">A cancellation token to cancel the load process. Can be <c>null</c>.</param>
    public void LoadPackageInformation(CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
            return;

        foreach (var asset in Package.Assets.ToList())
        {
            if (token.IsCancellationRequested)
                return;

            var url = asset.Location;
            DirectoryBaseViewModel directory;
            // TODO CSPROJ=XKPKG override rather than cast to subclass
            if (asset.Asset is IProjectAsset && this is ProjectViewModel project)
            {
                directory = project.GetOrCreateProjectDirectory(url.GetFullDirectory());
            }
            else
            {
                directory = GetOrCreateAssetDirectory(url.GetFullDirectory());
            }
            var assetViewModel = CreateAsset(asset, directory);
            directory.AddAsset(assetViewModel);
        }

        FillRootAssetCollection();

        foreach (var explicitDirectory in Package.ExplicitFolders)
        {
            GetOrCreateAssetDirectory(explicitDirectory);
        }
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

    private AssetViewModel CreateAsset(AssetItem assetItem, DirectoryBaseViewModel directory, ILogger? logger = null)
    {
        AssetCollectionItemIdHelper.GenerateMissingItemIds(assetItem.Asset);
        Session.GraphContainer.InitializeAsset(assetItem, logger);
        var assetViewModelType = Session.GetAssetViewModelType(assetItem);
        if (assetViewModelType.IsGenericType)
        {
            assetViewModelType = assetViewModelType.MakeGenericType(assetItem.Asset.GetType());
        }
        return (AssetViewModel)Activator.CreateInstance(assetViewModelType, assetItem, directory)!;
    }
    
    private void FillRootAssetCollection()
    {
        rootAssets.Clear();
        rootAssets.AddRange(Package.RootAssets.Select(x => Session.GetAssetById(x.Id)).NotNull()!);
        foreach (var dependency in PackageContainer.FlattenedDependencies)
        {
            if (dependency.Package != null)
                rootAssets.AddRange(dependency.Package.RootAssets.Select(x => Session.GetAssetById(x.Id)).NotNull()!);
        }
    }
}
