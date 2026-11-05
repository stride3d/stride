// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System.Linq;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.ViewModels.GraphicsCompositorEditor
{
    public sealed class SharedRendererOutputSlotViewModel : GraphicsCompositorSlotViewModel
    {
        private readonly NodeAccessor accessor;
        private readonly SharedRendererReferenceKey slotKey;

        public SharedRendererOutputSlotViewModel(GraphicsCompositorBlockViewModel block, string name, SharedRendererReferenceKey slotKey)
            : base(block, name)
        {
            this.slotKey = slotKey;
            accessor = slotKey.Path.GetAccessor();
            if (accessor.Index != NodeIndex.Empty)
            {
                ((IObjectNode)accessor.Node).ItemChanged += ItemChanged;
            }
            else
            {
                ((IMemberNode)accessor.Node).ValueChanged += ValueChanged;
            }

        }

        public SharedRendererReferenceKey GetKey() => slotKey;

        private new GraphicsCompositorBlockViewModel Block => (GraphicsCompositorBlockViewModel)base.Block;

        public override void Destroy()
        {
            base.Destroy();
            if (accessor.Index != NodeIndex.Empty)
            {
                ((IObjectNode)accessor.Node).ItemChanged -= ItemChanged;
            }
            else
            {
                ((IMemberNode)accessor.Node).ValueChanged -= ValueChanged;
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
            return sharedRenderer != null && accessor.AcceptValue(sharedRenderer.GetSharedRenderer());
        }

        public override void LinkTo(IGraphicsCompositorSlotViewModel target)
        {
            using (var transaction = ServiceProvider.Get<IUndoRedoService>().CreateTransaction())
            {
                var sharedRenderer = target as SharedRendererInputSlotViewModel;
                if (sharedRenderer != null)
                {
                    if (accessor.AcceptValue(sharedRenderer.GetSharedRenderer()))
                    {
                        var newValue = sharedRenderer.GetSharedRenderer();
                        accessor.UpdateValue(newValue);
                    }
                }
                ServiceProvider.Get<IUndoRedoService>().SetName(transaction, $"Connect {Name} to {target.Name}");
            }
        }

        public override void ClearLink()
        {
            using (var transaction = ServiceProvider.Get<IUndoRedoService>().CreateTransaction())
            {
                accessor.UpdateValue(null);
                ServiceProvider.Get<IUndoRedoService>().SetName(transaction, "Clear link");
            }
        }

        public override void UpdateLink()
        {
            var value = accessor.RetrieveValue();
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
            if (e.ChangeType == ContentChangeType.CollectionUpdate && e.Index == accessor.Index)
            {
                UpdateLink();
            }
        }
    }
}
