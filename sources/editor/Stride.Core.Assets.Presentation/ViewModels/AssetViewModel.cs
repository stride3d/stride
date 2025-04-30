// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Presentation.ViewModels;

public interface IAssetViewModel<out TAsset>
    where TAsset : Asset
{
    TAsset Asset { get; }
}

public class AssetViewModel<TAsset> : AssetViewModel, IAssetViewModel<TAsset>
    where TAsset : Asset
{
    public AssetViewModel(ConstructorParameters parameters)
        : base(parameters)
    {
    }

    /// <inheritdoc />
    public new TAsset Asset => (TAsset)base.Asset;
}

public abstract class AssetViewModel : SessionObjectViewModel, IAssetPropertyProviderViewModel
{
    private AssetItem assetItem;
    private DirectoryBaseViewModel directory;
    private string name;
    private ThumbnailData? thumbnailData;

    protected AssetViewModel(ConstructorParameters parameters)
        : base(parameters.Directory.Session)
    {
        Initializing = true;

        this.assetItem = parameters.AssetItem;
        this.directory = parameters.Directory;
        var forcedRoot = AssetType.GetCustomAttribute<AssetDescriptionAttribute>()?.AlwaysMarkAsRoot ?? false;
        Dependencies = new AssetDependenciesViewModel(this, forcedRoot);
        Sources = new AssetSourcesViewModel(this);

        InitialUndelete(parameters.CanUndoRedoCreation);

        name = Path.GetFileName(assetItem.Location);
        PropertyGraph = Session.GraphContainer.TryGetGraph(assetItem.Id);
        Initializing = false;
    }

    public Asset Asset => AssetItem.Asset;

    public AssetItem AssetItem
    {
        get => assetItem;
        set => SetValue(ref assetItem, value);
    }

    public IAssetObjectNode? AssetRootNode => PropertyGraph?.RootNode;

    public Type AssetType => AssetItem.Asset.GetType();

    public AssetId Id => AssetItem.Id;

    /// <summary>
    /// Gets whether the properties of this asset can be edited.
    /// </summary>
    public override bool IsEditable => Directory?.Package?.IsEditable ?? false;

    public DirectoryBaseViewModel Directory
    {
        get => directory;
        private set => SetValue(ref directory, value);
    }

    /// <summary>
    /// Gets the dependencies of this asset.
    /// </summary>
    public AssetDependenciesViewModel Dependencies { get; }

    /// <inheritdoc/>
    public override string Name
    {
        get => name;
        set => SetValue(ref name, value); // TODO rename
    }

    public AssetPropertyGraph? PropertyGraph { get; }

    /// <summary>
    /// Gets the view model of the sources of this asset.
    /// </summary>
    public AssetSourcesViewModel Sources { get; }

    /// <summary>
    /// The <see cref="ThumbnailData"/> associated to this <see cref="AssetViewModel"/>.
    /// </summary>
    public ThumbnailData? ThumbnailData
    {
        get => thumbnailData;
        set => SetValue(ref thumbnailData, value);
    }

    /// <summary>
    /// Gets the display name of the type of this asset.
    /// </summary>
    public override string TypeDisplayName { get { var desc = DisplayAttribute.GetDisplay(AssetType); return desc != null ? desc.Name : AssetType.Name; } }

    /// <summary>
    /// Gets the url of this asset.
    /// </summary>
    public string Url => AssetItem.Location;

    protected bool Initializing { get; private set; }

    protected Package Package => Directory.Package.Package;

    /// <summary>
    /// Initializes this asset. This method is guaranteed to be called once every other assets are loaded in the session.
    /// </summary>
    /// <remarks>
    /// Inheriting classes should override it when necessary, provided that they also call the base implementation.
    /// </remarks>
    public virtual void Initialize()
    {
        using var transaction = UndoRedoService?.CreateTransaction();
        PropertyGraph?.Initialize();
        UndoRedoService?.SetName(transaction!, $"Reconcile {Url} with its archetypes");
    }

    protected virtual GraphNodePath GetPathToPropertiesRootNode()
    {
        return new GraphNodePath(AssetRootNode);
    }

    protected virtual IObjectNode? GetPropertiesRootNode()
    {
        return AssetRootNode;
    }

    protected virtual bool ShouldConstructPropertyItem(IObjectNode collection, NodeIndex index) => true;

    protected virtual bool ShouldConstructPropertyMember(IMemberNode member) => true;

    /// <inheritdoc/>
    protected override void UpdateIsDeletedStatus()
    {
        if (IsDeleted)
        {
            Package.Assets.Remove(AssetItem);
            Session.UnregisterAsset(this);
            Directory.Package.DeletedAssetsInternal.Add(this);
            if (PropertyGraph != null)
            {
                Session.GraphContainer.UnregisterGraph(Id);
            }
        }
        else
        {
            Package.Assets.Add(AssetItem);
            Session.RegisterAsset(this);
            Directory.Package.DeletedAssetsInternal.Remove(this);
            if (!Initializing && PropertyGraph != null)
            {
                Session.GraphContainer.RegisterGraph(PropertyGraph);
            }
        }
        AssetItem.IsDeleted = IsDeleted;
        Session.SourceTracker?.UpdateAssetStatus(this);
    }

    public static HashSet<AssetViewModel> ComputeRecursiveReferencerAssets(IEnumerable<AssetViewModel> assets)
    {
        var result = new HashSet<AssetViewModel>(assets.SelectMany(x => x.Dependencies.RecursiveReferencerAssets));
        return result;
    }

    public static HashSet<AssetViewModel> ComputeRecursiveReferencedAssets(IEnumerable<AssetViewModel> assets)
    {
        var result = new HashSet<AssetViewModel>(assets.SelectMany(x => x.Dependencies.RecursiveReferencedAssets));
        return result;
    }

    AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => this;

    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => !IsDeleted && IsEditable;

    GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode() => GetPathToPropertiesRootNode();

    IObjectNode? IPropertyProviderViewModel.GetRootNode() => GetPropertiesRootNode();

    bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ShouldConstructPropertyItem(collection, index);

    bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ShouldConstructPropertyMember(member);

    public readonly struct ConstructorParameters
    {
        public ConstructorParameters(AssetItem assetItem, DirectoryBaseViewModel directory, bool canUndoRedoCreation)
        {
            if (directory.Package is null) throw new ArgumentException("The provided directory must be in a project when creating an asset.");

            AssetItem = assetItem;
            CanUndoRedoCreation = canUndoRedoCreation;
            Directory = directory;
        }

        /// <summary>
        /// Gets the <see cref="AssetItem"/> instance representing the asset to construct.
        /// </summary>
        internal readonly AssetItem AssetItem;

        /// <summary>
        /// Gets whether the creation of this asset can be undone/redone.
        /// </summary>
        internal readonly bool CanUndoRedoCreation;

        /// <summary>
        /// Gets the directory containing the asset to construct.
        /// </summary>
        internal readonly DirectoryBaseViewModel Directory;
    }
}
