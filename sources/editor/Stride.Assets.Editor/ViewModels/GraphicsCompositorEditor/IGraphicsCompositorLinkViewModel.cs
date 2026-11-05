// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
namespace Stride.Assets.Editor.ViewModels.GraphicsCompositorEditor
{
    public interface IGraphicsCompositorLinkViewModel
    {
        IGraphicsCompositorSlotViewModel SourceSlot { get; }
        IGraphicsCompositorSlotViewModel TargetSlot { get; }
    }
}
