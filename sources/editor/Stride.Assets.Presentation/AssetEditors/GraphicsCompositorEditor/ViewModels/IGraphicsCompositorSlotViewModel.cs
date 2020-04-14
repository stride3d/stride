// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Collections;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public interface IGraphicsCompositorSlotViewModel
    {
        string Name { get; }

        IGraphicsCompositorBlockViewModel Block { get; }

        IObservableCollection<IGraphicsCompositorLinkViewModel> Links { get; }

        bool CanLinkTo(GraphicsCompositorSlotViewModel target);

        void LinkTo(IGraphicsCompositorSlotViewModel target);

        void ClearLink();
    }
}
