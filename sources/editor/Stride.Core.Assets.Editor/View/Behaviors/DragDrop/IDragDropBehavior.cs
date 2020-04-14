// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public interface IDragDropBehavior
    {
        bool CanDrag { get; set; }

        bool CanDrop { get; set; }

        DisplayDropAdorner DisplayDropAdorner { get; set; }
    }
}
