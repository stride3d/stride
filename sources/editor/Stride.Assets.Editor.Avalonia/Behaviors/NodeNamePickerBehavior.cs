// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Assets.Models;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Makes an <see cref="AutoCompleteBox"/> a filtering list of the node name of a node link: the items are the nodes of the
/// skeleton, and picking one gives its name.
/// </summary>
/// <seealso cref="AutoCompletePickerBehavior"/>
public sealed class NodeNamePickerBehavior : AutoCompletePickerBehavior
{
    /// <inheritdoc/>
    protected override string? GetItemText(object? item) => (item as NodeInformation)?.Name;
}
