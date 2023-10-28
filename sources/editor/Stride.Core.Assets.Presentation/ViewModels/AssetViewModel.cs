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
    public AssetViewModel(AssetItem assetItem, DirectoryBaseViewModel directory)
        : base(assetItem, directory)
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
    private ThumbnailData thumbnailData;

    protected AssetViewModel(AssetItem assetItem, DirectoryBaseViewModel directory)
        : base(directory.Session)
    {
        this.assetItem = assetItem;
        this.directory = directory;
        var forcedRoot = AssetType.GetCustomAttribute<AssetDescriptionAttribute>()?.AlwaysMarkAsRoot ?? false;
        Dependencies = new AssetDependenciesViewModel(this, forcedRoot);
        name = Path.GetFileName(assetItem.Location);
        PropertyGraph = Session.GraphContainer.TryGetGraph(assetItem.Id);
        Session.RegisterAsset(this);
    }

    public Asset Asset => AssetItem.Asset;

    public AssetItem AssetItem
    {
        get => assetItem;
        set => SetValue(ref assetItem, value);
    }

    public Type AssetType => AssetItem.Asset.GetType();

    public AssetId Id => AssetItem.Id;

    public DirectoryBaseViewModel Directory
    {
        get => directory;
        private set => SetValue(ref directory, value);
    }
    
    /// <summary>
    /// Gets the dependencies of this asset.
    /// </summary>
    public AssetDependenciesViewModel Dependencies { get; }

    public override string Name
    {
        get => name;
        set => SetValue(ref name, value); // TODO rename
    }

    public AssetPropertyGraph? PropertyGraph { get; }
    
    /// <summary>
    /// The <see cref="ThumbnailData"/> associated to this <see cref="AssetViewModel"/>.
    /// </summary>
    public ThumbnailData ThumbnailData
    {
        get => thumbnailData;
        set => SetValue(ref thumbnailData, value);
    }

    /// <summary>
    /// Gets the display name of the type of this asset.
    /// </summary>
    public string TypeDisplayName { get { var desc = DisplayAttribute.GetDisplay(AssetType); return desc != null ? desc.Name : AssetType.Name; } }
    
    /// <summary>
    /// Gets the url of this asset.
    /// </summary>
    public string Url => AssetItem.Location;

    protected Package Package => Directory.Package.Package;

    protected internal IAssetObjectNode? AssetRootNode => PropertyGraph?.RootNode;

    protected internal IUndoRedoService? UndoRedoService => ServiceProvider.TryGet<IUndoRedoService>();
    
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

    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true; //!IsDeleted && IsEditable;

    GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode() => GetPathToPropertiesRootNode();

    IObjectNode? IPropertyProviderViewModel.GetRootNode() => GetPropertiesRootNode();

    bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ShouldConstructPropertyItem(collection, index);

    bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ShouldConstructPropertyMember(member);
}
