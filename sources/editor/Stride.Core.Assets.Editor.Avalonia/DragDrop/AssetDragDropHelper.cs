// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// The data of an asset drag inside the editor: one item per dragged <see cref="AssetViewModel"/>.
/// </summary>
public static class AssetDragDropHelper
{
    /// <summary>
    /// The in-process format of a dragged asset.
    /// </summary>
    public static readonly DataFormat<AssetViewModel> AssetViewModelFormat = DataFormat.CreateInProcessFormat<AssetViewModel>("Stride.AssetViewModel");

    /// <summary>
    /// Creates the data of a drag of the given assets.
    /// </summary>
    public static DataTransfer CreateData(IEnumerable<AssetViewModel> assets)
    {
        var data = new DataTransfer();
        foreach (var asset in assets)
        {
            var item = new DataTransferItem();
            item.Set(AssetViewModelFormat, asset);
            data.Add(item);
        }
        return data;
    }

    /// <summary>
    /// Gets the dragged assets, or an empty list when the data holds none.
    /// </summary>
    public static IReadOnlyList<AssetViewModel> GetAssets(IDataTransfer? data)
    {
        if (data is null)
            return [];
        return data.TryGetValues(AssetViewModelFormat)?.OfType<AssetViewModel>().ToList() ?? [];
    }

    /// <summary>
    /// The modifier keys of a drag, as <see cref="IAddChildViewModel"/> takes them.
    /// </summary>
    public static AddChildModifiers ToAddChildModifiers(KeyModifiers modifiers)
    {
        var result = AddChildModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Control))
            result |= AddChildModifiers.Ctrl;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            result |= AddChildModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Alt))
            result |= AddChildModifiers.Alt;
        return result;
    }
}
