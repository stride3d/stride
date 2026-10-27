// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Avalonia.Commands;

/// <summary>
/// The commands of the Edit menu that act on the panel with the focus, like the application commands of WPF.
/// </summary>
public enum EditCommandKind
{
    Undo,
    Redo,
    Cut,
    Copy,
    Paste,
    Delete,
    Rename,
}
