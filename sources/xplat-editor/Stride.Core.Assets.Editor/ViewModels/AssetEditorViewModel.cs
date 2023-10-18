// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public interface IAssetEditorViewModel<out TAsset>
    where TAsset : AssetViewModel
{
    TAsset Asset { get; }
}

public class AssetEditorViewModel<TAsset> : AssetEditorViewModel, IAssetEditorViewModel<TAsset>
    where TAsset : AssetViewModel
{
    public AssetEditorViewModel(AssetViewModel asset)
        : base(asset)
    {
    }

    /// <inheritdoc />
    public new TAsset Asset => (TAsset)base.Asset;
}

public abstract class AssetEditorViewModel : DispatcherViewModel
{
    protected AssetEditorViewModel(AssetViewModel asset)
        : base(asset.ServiceProvider)
    {
        Asset = asset;
    }
    
    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    public AssetViewModel Asset { get;}
    
    /// <summary>
    /// The current session.
    /// </summary>
    public ISessionViewModel Session => Asset.Session;
}
