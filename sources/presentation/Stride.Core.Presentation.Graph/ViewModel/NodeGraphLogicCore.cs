// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using GraphX;
using GraphX.PCL.Logic.Models;
using QuickGraph;

namespace Stride.Core.Presentation.Graph.ViewModel
{
    /// <summary>
    /// Logics core object which contains all algorithms and logic settings
    /// </summary>
    public class NodeGraphLogicCore : GXLogicCore<NodeVertex, NodeEdge, BidirectionalGraph<NodeVertex, NodeEdge>> { }
}
