// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using QuickGraph;
using Stride.Core.Presentation.Graph.ViewModel;
using System.Windows;
using GraphX.Controls;

namespace Stride.Core.Presentation.Graph.Controls
{    
    /// <summary>
    /// 
    /// </summary>
    public class NodeGraphArea : GraphArea<NodeVertex, NodeEdge, BidirectionalGraph<NodeVertex, NodeEdge>> 
    {
        public virtual event LinkSelectedEventHandler LinkSelected;

        internal virtual void OnLinkSelected(FrameworkElement link)
        {
            LinkSelected?.Invoke(this, new LinkSelectedEventArgs(link));
        }
    }
}
