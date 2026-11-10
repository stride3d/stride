// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

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

    public override void Destroy()
    {
        // Unregister collection
        ServiceProvider.TryGet<SelectionService>()?.UnregisterSelectionScope(SelectedContent);

        base.Destroy();
    }

    public abstract IAssetPartViewModel? FindPartViewModel(AbsoluteId id);

    /// <summary>
    /// Selects again the items of <paramref name="previousSelection"/> that are in <paramref name="newSelection"/>, after
    /// they moved: a part moved from another asset into this one has a new view model, found by its id.
    /// </summary>
    public void FixupAndRestoreSelection(IEnumerable<object> previousSelection, IReadOnlyCollection<object> newSelection)
    {
        var fixedUpSelection = new List<object>();
        foreach (var item in previousSelection)
        {
            if (!newSelection.Contains(item))
                continue;

            if (item is not IAssetPartViewModel part)
            {
                fixedUpSelection.Add(item);
                continue;
            }

            // The part can have be moved from another asset, into the current one.
            // We expect a viewmodel to exist with a new absolute Id.
            if (FindPartViewModel(new AbsoluteId(Asset.Id, part.Id.ObjectId)) is { } movedPart)
                fixedUpSelection.Add(movedPart);
        }
        // Restore selection
        SelectedContent.AddRange(fixedUpSelection);
    }

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
