// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Quantum.Presenters;

/// <summary>
/// Arguments of the <see cref="INodePresenter.ValueChanged"/> event.
/// </summary>
public class ValueChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldValue">The old value of the node.</param>
    public ValueChangedEventArgs(object? oldValue)
        : this(oldValue, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldValue">The old value of the node.</param>
    /// <param name="isValueOnly">Whether only the value of the node changed, see <see cref="IsValueOnly"/>.</param>
    public ValueChangedEventArgs(object? oldValue, bool isValueOnly)
    {
        OldValue = oldValue;
        IsValueOnly = isValueOnly;
    }

    /// <summary>
    /// The old value of the node.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Whether only the value of the node changed: the node keeps its children and its attached properties, and the tree
    /// of presenters was neither refreshed nor finalized. Otherwise the node was refreshed (see <see cref="INodePresenter"/>).
    /// </summary>
    public bool IsValueOnly { get; }
}
