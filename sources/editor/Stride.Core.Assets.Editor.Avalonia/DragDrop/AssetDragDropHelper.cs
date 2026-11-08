// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// The data of a drag inside the editor: one item per dragged <see cref="AssetViewModel"/>, or one item per dragged object of
/// a view (the entities and folders of the scene tree, for instance).
/// </summary>
public static class AssetDragDropHelper
{
    /// <summary>
    /// The in-process format of a dragged asset.
    /// </summary>
    public static readonly DataFormat<AssetViewModel> AssetViewModelFormat = DataFormat.CreateInProcessFormat<AssetViewModel>("Stride.AssetViewModel");

    /// <summary>
    /// The in-process format of a dragged object that is not an asset, such as an entity of the scene tree.
    /// </summary>
    public static readonly DataFormat<object> ItemFormat = DataFormat.CreateInProcessFormat<object>("Stride.DraggedItem");

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
    /// Creates the data of a drag of the given objects. The assets among them can also be read with <see cref="GetAssets"/>.
    /// </summary>
    public static DataTransfer CreateItemData(IEnumerable<object> items)
    {
        var data = new DataTransfer();
        foreach (var item in items)
        {
            var dataItem = new DataTransferItem();
            if (item is AssetViewModel asset)
                dataItem.Set(AssetViewModelFormat, asset);
            else
                dataItem.Set(ItemFormat, item);
            data.Add(dataItem);
        }
        return data;
    }

    /// <summary>
    /// Gets the dragged objects, assets included, in their order, or an empty list when the data holds none.
    /// </summary>
    public static IReadOnlyList<object> GetItems(IDataTransfer? data)
    {
        if (data is null)
            return [];

        var items = new List<object>();
        foreach (var item in data.Items)
        {
            if (item.TryGetRaw(AssetViewModelFormat) is AssetViewModel asset)
                items.Add(asset);
            else if (item.TryGetRaw(ItemFormat) is { } other)
                items.Add(other);
        }
        return items;
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
