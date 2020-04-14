// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    /// <summary>
    /// An object that will customize an instance of <see cref="INodePresenter"/>.
    /// </summary>
    public interface INodePresenterUpdater
    {
        /// <summary>
        /// Updates the given node if required. This method is called once all the children of the node have been created,
        /// but its siblings and the siblings of its parent are not guaranteed to exist. This method is also called again
        /// when the node is being refreshed, after its value has changed.
        /// </summary>
        /// <param name="node">The node to update.</param>
        void UpdateNode([NotNull] INodePresenter node);

        /// <summary>
        /// Finalizes the tree of node presenters. This method is called once the full tree has been generated, when
        /// any of its node has been refreshed, or when a virtual node has been added.
        /// </summary>
        /// <param name="root">The root of of the tree.</param>
        /// <remarks>This method should not be used to modify the hierarchy.</remarks>
        void FinalizeTree([NotNull] INodePresenter root);
    }
}
