// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// The side of a node a <see cref="NodeGraphConnector"/> is on: a link goes from an output slot to an input slot.
/// </summary>
public enum NodeGraphSlotDirection
{
    /// <summary>
    /// The slot receives links.
    /// </summary>
    Input,

    /// <summary>
    /// The slot starts links.
    /// </summary>
    Output,
}
