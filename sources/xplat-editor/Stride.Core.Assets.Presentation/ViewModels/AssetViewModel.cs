// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum;

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

public abstract class AssetViewModel : SessionObjectViewModel
{
    private AssetItem assetItem;
    private DirectoryBaseViewModel directory;
    private string name;

    protected AssetViewModel(AssetItem assetItem, DirectoryBaseViewModel directory)
        : base(directory.Session)
    {
        this.assetItem = assetItem;
        this.directory = directory;
        this.name = Path.GetFileName(assetItem.Location);
        PropertyGraph = Session.GraphContainer.TryGetGraph(assetItem.Id);
        Session.RegisterAsset(this);
    }

    public Asset Asset => AssetItem.Asset;

    public AssetItem AssetItem
    {
        get => assetItem;
        set => SetProperty(ref assetItem, value);
    }

    public AssetId Id => AssetItem.Id;

    public DirectoryBaseViewModel Directory
    {
        get => directory;
        private set => SetProperty(ref directory, value);
    }

    public override string Name
    {
        get => name;
        set => SetProperty(ref name, value); // TODO rename
    }

    public AssetPropertyGraph PropertyGraph { get; }

    protected Package Package => Directory.Package.Package;
}
