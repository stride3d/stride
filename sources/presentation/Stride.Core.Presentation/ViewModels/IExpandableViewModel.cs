// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// An item of a tree that remembers whether it shows its children, so that a view can expand it before its row exists.
/// </summary>
public interface IExpandableViewModel
{
    /// <summary>
    /// Gets or sets whether the item shows its children.
    /// </summary>
    bool IsExpanded { get; set; }
}
