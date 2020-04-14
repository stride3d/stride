// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using GraphX.Controls;
using GraphX.Controls.Models;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Graph.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Assets.Scripts;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    public class BlockNodeVertex : NodeVertex, IAddChildViewModel
    {
        public const int SnapGridSize = 16;

        private VertexControl vertexControl;

        // Used to avoid cycle dependency between control <=> quantum two way binding
        private bool updatingPosition;

        internal BlockNodeVertex(VisualScriptBlockViewModel viewModel)
        {
            ViewModel = viewModel;
            InputSlots = new ObservableCollection<object>();
            OutputSlots = new ObservableCollection<object>();

            // Add initial slots (if any)
            foreach (var slot in viewModel.Block.Slots)
            {
                var slots = slot.Direction == SlotDirection.Input ? InputSlots : OutputSlots;
                var slotViewModel = new VisualScriptSlotViewModel(viewModel, slot);
                slots.Add(slotViewModel);
                viewModel.Slots.Add(slot, slotViewModel);
            }

            // Setup listener to be aware of future changes
            viewModel.Block.Slots.CollectionChanged += Slots_CollectionChanged;
        }

        private void Slots_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var slot = (Slot)e.Item;
                    var slots = slot.Direction == SlotDirection.Input ? InputSlots : OutputSlots;
                    var slotViewModel = new VisualScriptSlotViewModel(ViewModel, slot);
                    slots.Add(slotViewModel);
                    ViewModel.Slots.Add(slot, slotViewModel);
                    break;
                }
                case NotifyCollectionChangedAction.Remove:
                {
                    var slot = (Slot)e.Item;
                    var slots = slot.Direction == SlotDirection.Input ? InputSlots : OutputSlots;
                    var slotViewModel = ViewModel.Slots[slot];
                    slots.Remove(slotViewModel);
                    ViewModel.Slots.Remove(slot);
                    break;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public VisualScriptBlockViewModel ViewModel { get; }

        public override void AddOutgoing(NodeVertex target, object from, object to)
        {
            base.AddOutgoing(target, @from, to);

            // Add link through editor view model
            ViewModel.Method.Method.AddLink(new Link
            {
                Source = ((VisualScriptSlotViewModel)@from).Slot,
                Target = ((VisualScriptSlotViewModel)to).Slot,
            });
        }

        public override void ConnectControl(VertexControl vertexControl)
        {
            this.vertexControl = vertexControl;

            // Set initial position from Block
            var currentPosition = ViewModel.Position;
            vertexControl.SetPosition(new System.Windows.Point(currentPosition.X, currentPosition.Y));

            // Listen to changes
            // TODO: we listen on MouseUp instead of PositionChanged to avoid intermediate changes
            // Ideally, we should use a single Quantum transaction in the drag behavior to end up with a single change
            vertexControl.MouseUp += VertexControl_MouseUp;
            vertexControl.PositionChanged += VertexControl_PositionChanged;
            ViewModel.PositionChanged += ViewModel_PositionChanged;
        }

        private void VertexControl_PositionChanged(object sender, VertexPositionEventArgs args)
        {
            var position = args.Position;

            // Round integer and snap
            var newPosition = RoundSnapPosition(position);

            // Move the vertex control there
            vertexControl.SetPosition(new System.Windows.Point(newPosition.X, newPosition.Y));
        }

        private void VertexControl_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // This will call FinalizeMove on every selected elements (the one that might have moved)
            ViewModel.Method.FinalizeMoves();
        }

        internal void FinalizeMove()
        {
            if (updatingPosition)
                return;

            updatingPosition = true;

            var position = vertexControl.GetPosition();

            // Round integer and snap
            var newPosition = RoundSnapPosition(position);

            // Move the vertex control there
            vertexControl.SetPosition(new System.Windows.Point(newPosition.X, newPosition.Y), false);

            // Forward only if there is an actual change (to ignore MouseUp that were not a drag & drop)
            if (ViewModel.Position != newPosition)
                ViewModel.Position = newPosition;

            updatingPosition = false;
        }

        private static Int2 RoundSnapPosition(System.Windows.Point position)
        {
            // Round integer and snap (using rounding)
            return new Int2(((int)Math.Round(position.X) + SnapGridSize / 2) / SnapGridSize * SnapGridSize,
                            ((int)Math.Round(position.Y) + SnapGridSize / 2) / SnapGridSize * SnapGridSize);
        }

        public override void DisconnectControl(VertexControl vertexControl)
        {
            ViewModel.PositionChanged -= ViewModel_PositionChanged;
            vertexControl.MouseUp -= VertexControl_MouseUp;

            this.vertexControl = null;
        }

        private void ViewModel_PositionChanged(object sender, MemberNodeChangeEventArgs e)
        {
            if (updatingPosition)
                return;

            updatingPosition = true;

            var currentPosition = ViewModel.Position;
            vertexControl.SetPosition(new System.Windows.Point(currentPosition.X, currentPosition.Y));

            updatingPosition = false;
        }

        public override string ToString()
        {
            return ViewModel.Block.Title;
        }

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            // Defer to VisualScriptBlockViewModel
            return ViewModel.CanAddChildren(children, modifiers, out message);
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            // Defer to VisualScriptBlockViewModel
            ViewModel.AddChildren(children, modifiers);
        }
    }
}
