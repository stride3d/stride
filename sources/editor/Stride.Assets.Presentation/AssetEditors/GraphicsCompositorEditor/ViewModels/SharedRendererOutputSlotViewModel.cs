// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public class SharedRendererOutputSlotViewModel : GraphicsCompositorSlotViewModel
    {
        protected readonly NodeAccessor Accessor;
        private readonly SharedRendererReferenceKey slotKey;

        public SharedRendererOutputSlotViewModel(GraphicsCompositorBlockViewModel block, string name, SharedRendererReferenceKey slotKey)
            : base(block, name)
        {
            this.slotKey = slotKey;
            Accessor = slotKey.Path.GetAccessor();
            if (Accessor.Index != NodeIndex.Empty)
            {
                ((IObjectNode)Accessor.Node).ItemChanged += ItemChanged;
            }
            else
            {
                ((IMemberNode)Accessor.Node).ValueChanged += ValueChanged;
            }

        }

        public SharedRendererReferenceKey GetKey() => slotKey;

        private new GraphicsCompositorBlockViewModel Block => (GraphicsCompositorBlockViewModel)base.Block;

        public override void Destroy()
        {
            base.Destroy();
            if (Accessor.Index != NodeIndex.Empty)
            {
                ((IObjectNode)Accessor.Node).ItemChanged -= ItemChanged;
            }
            else
            {
                ((IMemberNode)Accessor.Node).ValueChanged -= ValueChanged;
            }
        }

        public static string ComputeName(SharedRendererReferenceKey slotKey)
        {
            var name = string.Concat(slotKey.Path.Path.Where(x => x.Type != GraphNodePath.ElementType.Target).Select(x => x.ToString()));
            var memberNode = slotKey.Path.RootNode as IMemberNode;
            if (memberNode != null)
                name = memberNode.Name + name;
            return name.TrimStart('.');
        }

        public override bool CanLinkTo(GraphicsCompositorSlotViewModel target)
        {
            var sharedRenderer = target as SharedRendererInputSlotViewModel;
            return sharedRenderer != null && Accessor.AcceptValue(sharedRenderer.GetSharedRenderer());
        }

        public override void LinkTo(IGraphicsCompositorSlotViewModel target)
        {
            using (var transaction = ServiceProvider.Get<IUndoRedoService>().CreateTransaction())
            {
                var sharedRenderer = target as SharedRendererInputSlotViewModel;
                if (sharedRenderer != null)
                {
                    if (Accessor.AcceptValue(sharedRenderer.GetSharedRenderer()))
                    {
                        var newValue = sharedRenderer.GetSharedRenderer();
                        Accessor.UpdateValue(newValue);
                    }
                }
                ServiceProvider.Get<IUndoRedoService>().SetName(transaction, $"Connect {Name} to {target.Name}");
            }
        }

        public override void ClearLink()
        {
            using (var transaction = ServiceProvider.Get<IUndoRedoService>().CreateTransaction())
            {
                Accessor.UpdateValue(null);
                ServiceProvider.Get<IUndoRedoService>().SetName(transaction, "Clear link");
            }
        }

        public override void UpdateLink()
        {
            var value = Accessor.RetrieveValue();
            foreach (var link in Links.Cast<GraphicsCompositorLinkViewModel>().ToList())
            {
                link.Destroy();
                link.SourceSlot.Links.Remove(link);
                link.TargetSlot.Links.Remove(link);
            }
            if (value != null)
            {
                var targetBlock = Block.Editor.GetBlock(value);
                if (targetBlock != null)
                {
                    // Shared renderers have a single input, representing the renderer itself.
                    var targetSlot = (GraphicsCompositorSlotViewModel)targetBlock.InputSlots[0];
                    var link = Block.Editor.CreateLink(this, targetSlot);
                    Links.Add(link);
                    targetSlot.Links.Add(link);
                }
            }
        }

        private void ValueChanged(object sender, MemberNodeChangeEventArgs e)
        {
            UpdateLink();
        }

        private void ItemChanged(object sender, ItemChangeEventArgs e)
        {
            if (e.ChangeType == ContentChangeType.CollectionUpdate && e.Index == Accessor.Index)
            {
                UpdateLink();
            }
        }
    }
}
