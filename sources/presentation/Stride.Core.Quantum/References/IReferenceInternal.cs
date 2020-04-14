// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;

namespace Stride.Core.Quantum.References
{
    internal interface IReferenceInternal : IReference
    {
        /// <summary>
        /// Refreshes this reference by making point to the proper target nodes. If no node exists yet for some of the target, they will be created
        /// using the given node factory.
        /// </summary>
        /// <param name="ownerNode">The node owning this reference.</param>
        /// <param name="nodeContainer">The node container containing the <paramref name="ownerNode"/> and the target nodes.</param>
        void Refresh([NotNull] IGraphNode ownerNode, NodeContainer nodeContainer);
    }
}
