// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;

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
        ServiceProvider.TryGet<SelectionService>()?.RegisterSelectionScope(GetObjectToSelect, GetSelectedObjectId, SelectedContent);
    }

    public ObservableSet<object> SelectedContent { get; } = [];

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectedContent.Clear();
    }

    public override void Destroy()
    {
        // Unregister collection
        ServiceProvider.TryGet<SelectionService>()?.UnregisterSelectionScope(SelectedContent);

        base.Destroy();
    }

    public abstract IAssetPartViewModel? FindPartViewModel(AbsoluteId id);

    /// <summary>
    /// Resolves the provided <paramref name="id"/> into the corresponding object, or <c>null</c>.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <seealso cref="SelectionService.RegisterSelectionScope"/>
    protected virtual object? GetObjectToSelect(AbsoluteId id)
    {
        return FindPartViewModel(id);
    }

    /// <summary>
    /// Resolves the provided <paramref name="obj"/> into its corresponding id, or <c>null</c>.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <seealso cref="SelectionService.RegisterSelectionScope"/>
    protected virtual AbsoluteId? GetSelectedObjectId(object obj)
    {
        return (obj as IAssetPartViewModel)?.Id;
    }
}
