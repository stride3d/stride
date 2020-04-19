// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections;
using System.Reflection;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Processor for a single node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="nodeProcessorContext">The node processor context.</param>
    /// <returns>The node transformed</returns>
    public delegate Node NodeProcessor(Node node, ref NodeProcessorContext nodeProcessorContext);

    /// <summary>
    /// Processor for a list of node.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="nodeProcessorContext">The node processor context.</param>
    public delegate void NodeListProcessor(IList list, ref NodeProcessorContext nodeProcessorContext);

    /// <summary>
    /// Node explorer.
    /// </summary>
    public struct NodeProcessorContext
    {
        /// <summary>
        /// Gets or sets the node processor.
        /// </summary>
        public NodeProcessor NodeProcessor;

        /// <summary>
        /// Gets or sets the list processor.
        /// </summary>
        public NodeListProcessor ListProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeProcessorContext"/> class.
        /// </summary>
        /// <param name="nodeProcessor">The node processor.</param>
        /// <param name="listProcessor">The list processor.</param>
        public NodeProcessorContext(NodeProcessor nodeProcessor, NodeListProcessor listProcessor)
        {
            NodeProcessor = nodeProcessor;
            ListProcessor = listProcessor;
        }
    }
}
