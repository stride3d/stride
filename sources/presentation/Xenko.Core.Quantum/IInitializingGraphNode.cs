// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Quantum
{
    /// <summary>
    /// An interface representing an <see cref="IGraphNode"/> during its initialization phase.
    /// </summary>
    public interface IInitializingGraphNode : IGraphNode
    {
        /// <summary>
        /// Seal the node, indicating its construction is finished and that no more children will be added.
        /// </summary>
        void Seal();
    }
}
