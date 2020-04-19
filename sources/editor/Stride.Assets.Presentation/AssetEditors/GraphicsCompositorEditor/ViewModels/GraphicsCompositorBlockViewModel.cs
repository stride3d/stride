// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public abstract class GraphicsCompositorBlockViewModel : GraphicsCompositorItemViewModel, IGraphicsCompositorBlockViewModel
    {
        protected readonly ObservableList<IGraphicsCompositorSlotViewModel> InputSlotCollection = new ObservableList<IGraphicsCompositorSlotViewModel>();
        protected readonly ObservableList<IGraphicsCompositorSlotViewModel> OutputSlotCollection = new ObservableList<IGraphicsCompositorSlotViewModel>();

        private GraphNodeChangeListener graphNodeListener;

        protected GraphicsCompositorBlockViewModel([NotNull] GraphicsCompositorEditorViewModel editor)
            : base(editor)
        {
        }

        public abstract string Title { get; }

        public IObservableList<IGraphicsCompositorSlotViewModel> InputSlots => InputSlotCollection;

        public IObservableList<IGraphicsCompositorSlotViewModel> OutputSlots => OutputSlotCollection;

        public void Initialize()
        {
            // If anything changes in the renderer, scan references again
            graphNodeListener = new AssetGraphNodeChangeListener(GetRootNode(), Editor.Asset.PropertyGraph.Definition);
            graphNodeListener.Initialize();
            graphNodeListener.ValueChanged += GraphNodeListenerChanged;
            graphNodeListener.ItemChanged += GraphNodeListenerChanged;
        }

        public override void Destroy()
        {
            base.Destroy();
            foreach (var slot in InputSlots.Cast<GraphicsCompositorSlotViewModel>())
            {
                slot.Destroy();
            }
            InputSlots.Clear();
            foreach (var slot in OutputSlots.Cast<GraphicsCompositorSlotViewModel>())
            {
                slot.Destroy();
            }
            OutputSlots.Clear();
            Editor.SelectedSharedRenderers.Remove(this);
            graphNodeListener.ItemChanged -= GraphNodeListenerChanged;
            graphNodeListener.ValueChanged -= GraphNodeListenerChanged;
            graphNodeListener.Dispose();
            graphNodeListener = null;
        }

        public abstract bool UpdateSlots();
        
        private void GraphNodeListenerChanged(object sender, INodeChangeEventArgs e)
        {
            if (UpdateSlots())
            {
                UpdateOutgoingLinks();
            }
        }

        public void UpdateOutgoingLinks()
        {
            foreach (var slot in OutputSlots.Cast<GraphicsCompositorSlotViewModel>())
            {
                slot.UpdateLink();
            }
        }
    }
}
