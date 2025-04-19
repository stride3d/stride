// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// A base class for implementations of <see cref="ITemplateProvider"/> that can provide templates for <see cref="NodeViewModel"/> instances.
/// </summary>
public abstract class NodeViewModelTemplateProvider : TemplateProviderBase
{
    /// <inheritdoc/>
    public override bool Match(object? obj)
    {
        return obj is NodeViewModel node && MatchNode(node);
    }

    /// <summary>
    /// Indicates whether this instance of <see cref="ITemplateProvider"/> can provide a template for the given <see cref="NodeViewModel"/>.
    /// </summary>
    /// <param name="node">The node to test.</param>
    /// <returns><c>true</c> if this template provider can provide a template for the given node, <c>false</c> otherwise.</returns>
    /// <remarks>This method is invoked by <see cref="Match"/>.</remarks>
    public abstract bool MatchNode(NodeViewModel node);

    /// <summary>
    /// Indicates whether the given node matches the given type, either with the <see cref="NodeViewModel.Type"/> property
    /// or the type of the <see cref="NodeViewModel.NodeValue"/> property.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <param name="type">The type to match.</param>
    /// <returns><c>true</c> if the node matches the given type, <c>false</c> otherwise.</returns>
    protected static bool MatchType(NodeViewModel node, Type type)
    {
        return type.IsAssignableFrom(node.Type) || type.IsInstanceOfType(node.NodeValue);
    }
}
