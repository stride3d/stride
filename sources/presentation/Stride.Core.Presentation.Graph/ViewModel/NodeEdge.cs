// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.ObjectModel;
using GraphX.PCL.Common.Models;

namespace Stride.Core.Presentation.Graph.ViewModel
{
    /// <summary>
    /// Base edge used in node-based graphs.
    /// This class must derived from EdgeBase in GraphX.
    /// </summary>
    public class NodeEdge : EdgeBase<NodeVertex>
    {
        /// <summary>
        /// Default constructor. We need to set at least Source and Target properties of the edge.
        /// </summary>
        /// <param name="source">Source vertex data</param>
        /// <param name="target">Target vertex data</param>
        /// <param name="weight">Optional edge weight</param>
        public NodeEdge(NodeVertex source, NodeVertex target, double weight = 1)
            : base(source, target, weight)
        {
            // nothing
        }

        public object SourceSlot { get; set; }

        public object TargetSlot { get; set; }
    }
}
