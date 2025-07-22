// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// An interface that represents the view model of an asset editor.
/// </summary>
/// <typeparam name="TAsset"></typeparam>
public interface IAssetEditorViewModel<out TAsset>
    where TAsset : AssetViewModel
{
    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    TAsset Asset { get; }
}

/// <summary>
/// Base view model for asset editors.
/// </summary>
/// <typeparam name="TAsset"></typeparam>
public class AssetEditorViewModel<TAsset> : AssetEditorViewModel, IAssetEditorViewModel<TAsset>
    where TAsset : AssetViewModel
{
    public AssetEditorViewModel(TAsset asset)
        : base(asset)
    {
    }

    /// <inheritdoc cref="IAssetEditorViewModel{T}.Asset" />
    public override TAsset Asset => (TAsset)base.Asset;

    public SessionObjectPropertiesViewModel EditorProperties => Session.ActiveProperties;
}

/// <summary>
/// Base view model for asset editors.
/// </summary>
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
    public virtual AssetViewModel Asset { get; }

    /// <summary>
    /// The current session.
    /// </summary>
    public SessionViewModel Session => (SessionViewModel)Asset.Session;
}
