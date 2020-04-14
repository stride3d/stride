// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Graph.ViewModel;
using Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.Views
{
    public sealed class GraphicsCompositorNodeVertex : NodeVertex
    {
        private readonly GraphicsCompositorGraph graph;

        internal GraphicsCompositorNodeVertex([NotNull] GraphicsCompositorGraph graph, [NotNull] IGraphicsCompositorBlockViewModel block)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            this.graph = graph;
            Block = block;

            InputSlots = new ObservableCollection<object>();
            OutputSlots = new ObservableCollection<object>();
            block.InputSlots.ForEach(AddInputSlot);
            block.OutputSlots.ForEach(AddOutputSlot);

            // Track slot changes
            block.InputSlots.CollectionChanged += InputSlotsCollectionChanged;
            block.OutputSlots.CollectionChanged += OutputSlotsCollectionChanged;
        }

        [NotNull]
        public IGraphicsCompositorBlockViewModel Block { get; }

        public override void AddOutgoing(NodeVertex target, object from, object to)
        {
            var sourceSlot = from as IGraphicsCompositorSlotViewModel;
            var targetSlot = to as IGraphicsCompositorSlotViewModel;
            if (sourceSlot != null && targetSlot != null)
            {
                sourceSlot.LinkTo(targetSlot);
            }
        }

        private void InputSlotsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    foreach (IGraphicsCompositorSlotViewModel newSlot in e.NewItems)
                    {
                        AddInputSlot(newSlot);
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    foreach (IGraphicsCompositorSlotViewModel oldSlot in e.OldItems)
                    {
                        RemoveInputSlot(oldSlot);
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Reset:
                {
                    InputSlots.Clear();
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        private void OutputSlotsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (IGraphicsCompositorSlotViewModel slot in OutputSlots.ToList())
                {
                    RemoveOutputSlot(slot);
                }
            }
            if (e.NewItems != null)
            {
                foreach (IGraphicsCompositorSlotViewModel newSlot in e.NewItems)
                {
                    AddOutputSlot(newSlot);
                }
            }
            if (e.OldItems != null)
            {
                foreach (IGraphicsCompositorSlotViewModel oldSlot in e.OldItems)
                {
                    RemoveOutputSlot(oldSlot);
                }
            }
        }
        

        private void AddInputSlot(IGraphicsCompositorSlotViewModel slot)
        {
            InputSlots.Add(slot);
        }

        private void RemoveInputSlot(IGraphicsCompositorSlotViewModel slot)
        {
            InputSlots.Remove(slot);
        }

        private void AddOutputSlot(IGraphicsCompositorSlotViewModel slot)
        {
            OutputSlots.Add(slot);
            foreach (var link in slot.Links)
            {
                graph.CreateEdge(link);
            }
            slot.Links.CollectionChanged += LinksCollectionChanged;
        }

        private void RemoveOutputSlot(IGraphicsCompositorSlotViewModel slot)
        {
            foreach (var link in slot.Links)
            {
                graph.RemoveEdge(link);
            }
            slot.Links.CollectionChanged -= LinksCollectionChanged;
            OutputSlots.Remove(slot);
        }

        private void LinksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                var slot = Block.OutputSlots.First(x => x.Links == sender);
                graph.ClearAllEdges(slot);
            }

            if (e.NewItems != null)
            {
                foreach (IGraphicsCompositorLinkViewModel newLink in e.NewItems)
                {
                    graph.CreateEdge(newLink);
                }
            }
            if (e.OldItems != null)
            {
                foreach (IGraphicsCompositorLinkViewModel oldLink in e.OldItems)
                {
                    graph.RemoveEdge(oldLink);
                }
            }
        }
    }
}
