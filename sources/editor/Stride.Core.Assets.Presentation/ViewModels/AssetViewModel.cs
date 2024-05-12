// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
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
        name = Path.GetFileName(assetItem.Location);
    }

    public AssetItem AssetItem
    {
        get => assetItem;
        set => SetValue(ref assetItem, value);
    }

    public AssetId Id => AssetItem.Id;

    public override string Name
    {
        get => name;
        set => SetValue(ref name, value); // TODO rename
    }
}
