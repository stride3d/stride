// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Starts dragging the asset of an item when the pointer moves past a small distance with the left button down. Attach it to the
/// root of the item template of an asset list: its data context is the <see cref="AssetViewModel"/>.
/// </summary>
/// <remarks>
/// The asset can be linked (a reference of the property grid) or moved (a folder of the solution explorer).
/// </remarks>
public sealed class AssetDragBehavior : DragSourceBehavior
{
    /// <inheritdoc />
    protected override DragDropEffects AllowedEffects => DragDropEffects.Link | DragDropEffects.Move;

    /// <inheritdoc />
    protected override IDataTransfer? CreateData(object? dataContext)
    {
        return dataContext is AssetViewModel asset ? AssetDragDropHelper.CreateData([asset]) : null;
    }
}
