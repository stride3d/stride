// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class AssetViewModel : SessionObjectViewModel
{
    private AssetItem assetItem;
    private string name;

    protected AssetViewModel(AssetItem assetItem, ISessionViewModel session)
        : base(session)
    {
        this.assetItem = assetItem;
        this.name = Path.GetFileName(assetItem.Location);
    }

    public AssetItem AssetItem
    {
        get => assetItem;
        set => SetProperty(ref assetItem, value);
    }

    public AssetId Id => AssetItem.Id;

    public override string Name
    {
        get => name;
        set => SetProperty(ref name, value); // TODO rename
    }
}
