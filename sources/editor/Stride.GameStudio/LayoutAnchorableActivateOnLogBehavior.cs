// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.View.Behaviors;
using AvalonDock.Layout;

namespace Stride.GameStudio
{
    /// <summary>
    /// An implementation of the <see cref="ActivateOnLogBehavior{T}"/> for the <see cref="LayoutAnchorable"/> control.
    /// </summary>
    public class LayoutAnchorableActivateOnLogBehavior : ActivateOnLogBehavior<LayoutAnchorable>
    {
        protected override void Activate()
        {
            AssociatedObject.Show();
            AssociatedObject.IsSelected = true;
            AssociatedObject.IsActive = true;       // This ensures this 'tab' is the selected one in a tab group
        }
    }
}
