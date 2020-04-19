// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum.Internal
{
    /// <summary>
    /// An interface exposing internal methods of <see cref="IAssetNode"/>
    /// </summary>
    internal interface IAssetNodeInternal : IAssetNode
    {
        /// <summary>
        /// Gets or sets whether the override properties of this node are currently being reset.
        /// </summary>
        bool ResettingOverride { get; set; }

        /// <summary>
        /// Sets the <see cref="AssetPropertyGraph"/> of the asset related to this node.
        /// </summary>
        /// <param name="assetPropertyGraph"></param>
        void SetPropertyGraph([NotNull] AssetPropertyGraph assetPropertyGraph);

        /// <summary>
        /// Gets the base of this node.
        /// </summary>
        /// <param name="node">The base node to set.</param>
        void SetBaseNode(IGraphNode node);
    }
}
