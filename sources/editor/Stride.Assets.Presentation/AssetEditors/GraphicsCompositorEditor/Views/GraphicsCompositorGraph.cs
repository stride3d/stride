// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Graph.ViewModel;
using Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.Views
{
    // TODO: this object should be more closely integrated with the NodeGraphBehavior
    public class GraphicsCompositorGraph : IDisposable
    {
        private readonly IObservableCollection<IGraphicsCompositorBlockViewModel> blocks;
        private readonly IObservableCollection<IGraphicsCompositorBlockViewModel> selectedBlocks;
        private readonly IObservableCollection<IGraphicsCompositorLinkViewModel> selectedLinks;
        private readonly Dictionary<IGraphicsCompositorBlockViewModel, NodeVertex> vertexMapping = new Dictionary<IGraphicsCompositorBlockViewModel, NodeVertex>();
        private readonly Dictionary<IGraphicsCompositorLinkViewModel, NodeEdge> edgeMapping = new Dictionary<IGraphicsCompositorLinkViewModel, NodeEdge>();
        private readonly Dictionary<NodeVertex, IGraphicsCompositorBlockViewModel> blockMapping = new Dictionary<NodeVertex, IGraphicsCompositorBlockViewModel>();
        private readonly Dictionary<NodeEdge, IGraphicsCompositorLinkViewModel> linkMapping = new Dictionary<NodeEdge, IGraphicsCompositorLinkViewModel>();
        private readonly ObservableList<NodeVertex> vertices = new ObservableList<NodeVertex>();
        private readonly ObservableList<NodeEdge> edges = new ObservableList<NodeEdge>();
        private readonly ObservableList<NodeVertex> selectedVertices = new ObservableList<NodeVertex>();
        private readonly ObservableList<NodeEdge> selectedEdges = new ObservableList<NodeEdge>();
        private int vertexId;
        private bool synchronizingSelection;

        public GraphicsCompositorGraph([NotNull] IObservableCollection<IGraphicsCompositorBlockViewModel> blocks, IObservableCollection<IGraphicsCompositorBlockViewModel> selectedBlocks, IObservableCollection<IGraphicsCompositorLinkViewModel> selectedLinks)
        {
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));
            this.blocks = blocks;
            this.selectedBlocks = selectedBlocks;
            this.selectedLinks = selectedLinks;
            blocks.CollectionChanged += BlocksCollectionChanged;
            selectedBlocks.CollectionChanged += SelectedBlocksCollectionChanged;
            selectedLinks.CollectionChanged += SelectedLinksCollectionChanged;
            selectedVertices.CollectionChanged += SelectedVerticesCollectionChanged;
            selectedEdges.CollectionChanged += SelectedEdgesCollectionChanged;
        }

        public void Dispose()
        {
            blocks.CollectionChanged -= BlocksCollectionChanged;
            selectedBlocks.CollectionChanged -= SelectedBlocksCollectionChanged;
            selectedLinks.CollectionChanged -= SelectedLinksCollectionChanged;
            selectedVertices.CollectionChanged -= SelectedVerticesCollectionChanged;
            selectedEdges.CollectionChanged -= SelectedEdgesCollectionChanged;
        }

        public IList Vertices => new NonGenericObservableListWrapper<NodeVertex>(vertices);

        public IList Edges => new NonGenericObservableListWrapper<NodeEdge>(edges);

        public IList SelectedVertices => new NonGenericObservableListWrapper<NodeVertex>(selectedVertices);

        public IList SelectedEdges => new NonGenericObservableListWrapper<NodeEdge>(selectedEdges);

        public void CreateVertex(IGraphicsCompositorBlockViewModel block)
        {
            var vertex = new GraphicsCompositorNodeVertex(this, block) { ID = ++vertexId };
            vertices.Add(vertex);
            vertexMapping.Add(block, vertex);
            blockMapping.Add(vertex, block);
        }

        public void RemoveVertex(IGraphicsCompositorBlockViewModel block)
        {
            var vertex = GetVertex(block);
            vertexMapping.Remove(block);
            blockMapping.Remove(vertex);
            vertices.Remove(vertex);
        }

        public NodeVertex GetVertex(IGraphicsCompositorBlockViewModel block)
        {
            return vertexMapping[block];
            //NodeVertex vertex;
            //vertexMapping.TryGetValue(block, out vertex);
            //return vertex;
        }

        public void CreateEdge(IGraphicsCompositorLinkViewModel link)
        {
            var sourceVertex = GetVertex(link.SourceSlot.Block);
            var targetVertex = GetVertex(link.TargetSlot.Block);
            var nodeEdge = new NodeEdge(sourceVertex, targetVertex)
            {
                SourceSlot = link.SourceSlot,
                TargetSlot = link.TargetSlot
            };
            edgeMapping.Add(link, nodeEdge);
            linkMapping.Add(nodeEdge, link);
            edges.Add(nodeEdge);
        }

        public void RemoveEdge(IGraphicsCompositorLinkViewModel link)
        {
            var edge = GetEdge(link);
            edgeMapping.Remove(link);
            linkMapping.Remove(edge);
            edges.Remove(edge);
        }

        public void ClearAllEdges(IGraphicsCompositorSlotViewModel slot)
        {
            var linksToRemove = edges.Select(GetLink).Where(x => x.SourceSlot == slot || x.TargetSlot == slot).ToList();
            foreach (var link in linksToRemove)
            {
                RemoveEdge(link);
            }
        }

        public NodeEdge GetEdge(IGraphicsCompositorLinkViewModel link)
        {
            return edgeMapping[link];
            //NodeEdge edge;
            //edgeMapping.TryGetValue(link, out edge);
            //return edge;
        }

        private IGraphicsCompositorBlockViewModel GetBlock(NodeVertex vertex)
        {
            return blockMapping[vertex];
        }

        private IGraphicsCompositorLinkViewModel GetLink(NodeEdge edge)
        {
            return linkMapping[edge];
        }

        private void BlocksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        foreach (IGraphicsCompositorBlockViewModel newBlock in e.NewItems)
                        {
                            CreateVertex(newBlock);
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        foreach (IGraphicsCompositorBlockViewModel oldBlock in e.OldItems)
                        {
                            RemoveVertex(oldBlock);
                        }
                        break;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        private void SelectedBlocksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeSelectionCollections<IGraphicsCompositorBlockViewModel, NodeVertex>(selectedVertices, GetVertex, e);
        }

        private void SelectedLinksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeSelectionCollections<IGraphicsCompositorLinkViewModel, NodeEdge>(selectedEdges, GetEdge, e);
        }

        private void SelectedVerticesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeSelectionCollections<NodeVertex, IGraphicsCompositorBlockViewModel>(selectedBlocks, GetBlock, e);
        }

        private void SelectedEdgesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SynchronizeSelectionCollections<NodeEdge, IGraphicsCompositorLinkViewModel>(selectedLinks, GetLink, e);
        }

        private void SynchronizeSelectionCollections<TSource, TTarget>(IObservableCollection<TTarget> collection, Func<TSource, TTarget> getItem, NotifyCollectionChangedEventArgs e)
        {
            if (synchronizingSelection)
                return;

            synchronizingSelection = true;
            if (e.NewItems != null)
            {
                foreach (TSource newItem in e.NewItems)
                {
                    var targetItem = getItem(newItem);
                    collection.Add(targetItem);
                }
            }
            if (e.OldItems != null)
            {
                foreach (TSource oldItem in e.OldItems)
                {
                    var targetItem = getItem(oldItem);
                    collection.Remove(targetItem);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                collection.Clear();
            }
            synchronizingSelection = false;
        }
    }
}
