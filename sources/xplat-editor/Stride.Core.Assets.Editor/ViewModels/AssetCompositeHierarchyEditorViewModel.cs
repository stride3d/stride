// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Base class for the view model of an <see cref="AssetCompositeHierarchyViewModel{TAssetPartDesign,TAssetPart}"/> editor.
/// </summary>
/// <typeparam name="TAssetPartDesign">The type of a part design.</typeparam>
/// <typeparam name="TAssetPart">The type of a part.</typeparam>
/// <typeparam name="TAssetViewModel"></typeparam>
/// <typeparam name="TItemViewModel">The type of a real <see cref="AssetCompositeItemViewModel"/> that can be copied/cut/pasted.</typeparam>
public abstract class AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TAssetViewModel, TItemViewModel>
    : AssetCompositeEditorViewModel<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>, AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    where TAssetPart : class, IIdentifiable
    where TAssetViewModel : AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>
    where TItemViewModel : AssetCompositeItemViewModel<TAssetViewModel, TItemViewModel>
{
    private TItemViewModel rootPart;

    protected AssetCompositeHierarchyEditorViewModel(TAssetViewModel asset)
        : base(asset)
    {
        rootPart = CreateRootPartViewModel();
    }

    public TItemViewModel RootPart { get => rootPart; private set => SetValue(ref rootPart, value); }

    protected abstract TItemViewModel CreateRootPartViewModel();
}
