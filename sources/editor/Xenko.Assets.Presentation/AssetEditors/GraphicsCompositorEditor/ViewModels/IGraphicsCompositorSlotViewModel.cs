// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Collections;

namespace Xenko.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
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
