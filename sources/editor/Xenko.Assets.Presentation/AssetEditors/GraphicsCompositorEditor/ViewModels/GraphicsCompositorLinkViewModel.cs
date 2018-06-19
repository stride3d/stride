// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public class GraphicsCompositorLinkViewModel : DispatcherViewModel, IGraphicsCompositorLinkViewModel
    {
        public GraphicsCompositorLinkViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] GraphicsCompositorSlotViewModel source, [NotNull] GraphicsCompositorSlotViewModel target)
            : base(serviceProvider)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            SourceSlot = source;
            TargetSlot = target;
        }

        public IGraphicsCompositorSlotViewModel SourceSlot { get; }

        public IGraphicsCompositorSlotViewModel TargetSlot { get; }
    }
}
