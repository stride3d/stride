// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Quantum.ViewModels;

public class NodeViewModelValueChangedArgs : EventArgs
{
    public NodeViewModelValueChangedArgs(GraphViewModel viewModel, NodeViewModel node)
        : this(viewModel, node, false)
    {
    }

    public NodeViewModelValueChangedArgs(GraphViewModel viewModel, NodeViewModel node, bool isValueOnly)
    {
        ViewModel = viewModel;
        Node = node;
        IsValueOnly = isValueOnly;
    }

    public GraphViewModel ViewModel { get; }

    public NodeViewModel Node { get; }

    /// <summary>
    /// Whether only the value of the node changed: the node kept its children (see <see cref="Presenters.ValueChangedEventArgs.IsValueOnly"/>).
    /// </summary>
    public bool IsValueOnly { get; }
}
