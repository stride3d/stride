// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public interface INodePresenterFactory
{
    INodePresenter CreateNodeHierarchy(IObjectNode rootNode, GraphNodePath rootNodePath, IPropertyProviderViewModel? propertyProvider = null);

    bool IsPrimitiveType(Type type);

    /// <summary>
    /// Starts a batch of changes to the nodes shown by the presenters of this factory, on the calling thread.
    /// </summary>
    /// <returns>An object that ends the batch when it is disposed. Batches can be nested: the outermost one ends the batch.</returns>
    /// <remarks>
    /// <para>
    /// During a batch, a change of the value only of a node (see <see cref="ValueChangedEventArgs.IsValueOnly"/>) is raised at
    /// once, as outside of a batch. A change that refreshes a presenter rebuilds its children and runs the updaters on it at
    /// once, but the finalization of its tree and its <see cref="INodePresenter.ValueChanged"/> event wait for the end of the
    /// batch: the end of the batch finalizes each such tree once, then raises those events, in the order of the changes.
    /// </para>
    /// <para>
    /// A caller that writes many values in a row (the values of a running game, a paste on many members) uses it so that the
    /// changes of structure among them finalize the tree once instead of once per change.
    /// </para>
    /// </remarks>
    IDisposable BeginBatch();
}
