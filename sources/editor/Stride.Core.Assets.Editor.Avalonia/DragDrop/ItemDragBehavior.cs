// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Starts dragging the data context of an item, whatever its type, when the pointer moves past a small distance with the
/// left button down: the element factories of the toolbox of the UI editor, for instance, which the drop targets read with
/// <see cref="AssetDragDropHelper.GetItems"/>. Attach it to the root of the item template.
/// </summary>
public sealed class ItemDragBehavior : DragSourceBehavior
{
    /// <inheritdoc />
    protected override DragDropEffects AllowedEffects => DragDropEffects.Copy | DragDropEffects.Move;

    /// <inheritdoc />
    protected override IDataTransfer? CreateData(object? dataContext)
    {
        return dataContext is not null ? AssetDragDropHelper.CreateItemData([dataContext]) : null;
    }
}
