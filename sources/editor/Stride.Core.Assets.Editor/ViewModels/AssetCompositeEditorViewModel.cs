// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Base class for the view model of an <see cref="AssetCompositeViewModel{TAsset}"/> editor.
/// </summary>
/// <typeparam name="TAsset"></typeparam>
/// <typeparam name="TAssetViewModel"></typeparam>
public abstract class AssetCompositeEditorViewModel<TAsset, TAssetViewModel> : AssetEditorViewModel<TAssetViewModel>
    where TAsset : AssetComposite
    where TAssetViewModel : AssetCompositeViewModel<TAsset>   
{
    protected AssetCompositeEditorViewModel(TAssetViewModel asset)
        : base(asset)
    {
    }
}
