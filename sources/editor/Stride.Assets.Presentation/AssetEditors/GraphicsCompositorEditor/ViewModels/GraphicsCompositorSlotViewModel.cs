// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public abstract class GraphicsCompositorSlotViewModel : DispatcherViewModel, IGraphicsCompositorSlotViewModel
    {
        protected GraphicsCompositorSlotViewModel(GraphicsCompositorBlockViewModel block, string name)
            : base(block.SafeArgument(nameof(block)).ServiceProvider)
        {
            Block = block;
            Name = name;
        }

        public string Name { get; }

        public IGraphicsCompositorBlockViewModel Block { get; }

        public IObservableCollection<IGraphicsCompositorLinkViewModel> Links { get; } = new ObservableList<IGraphicsCompositorLinkViewModel>();

        public abstract void UpdateLink();

        public abstract bool CanLinkTo(GraphicsCompositorSlotViewModel target);

        public abstract void LinkTo(IGraphicsCompositorSlotViewModel target);

        public abstract void ClearLink();

        public override void Destroy()
        {
            base.Destroy();
            foreach (var link in Links.Cast<GraphicsCompositorLinkViewModel>().ToList())
            {
                link.Destroy();
                link.SourceSlot.Links.Remove(link);
                link.TargetSlot.Links.Remove(link);
            }
            Links.Clear();
        }
    }
}
