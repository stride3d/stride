// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// Where a panel goes when a layout has no place for it yet.
/// </summary>
public enum PanelPlacement
{
    /// <summary>
    /// A group on the left of the main window.
    /// </summary>
    Left,

    /// <summary>
    /// A group on the right of the main window.
    /// </summary>
    Right,

    /// <summary>
    /// A group at the bottom of the main window.
    /// </summary>
    Bottom,

    /// <summary>
    /// The centre of the main window: where the asset view is when no editor is open. In the layout with editors, the
    /// panel goes to the bottom instead, as the asset view does.
    /// </summary>
    Document,

    /// <summary>
    /// A floating window of its own.
    /// </summary>
    Floating,
}
