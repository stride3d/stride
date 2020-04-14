// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Game;
using Stride.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Adorners
{
    internal interface IAdornerBase<out TVisual>
        where TVisual : UIElement
    {
        TVisual Visual { get; }

        void Disable();

        void Enable();

        void Hide();

        void Show();

        void Update(Vector3 position);
    }

    /// <summary>
    /// Base class for adorners used by the <see cref="UIEditorGameAdornerService"/>.
    /// </summary>
    /// <typeparam name="TVisual"></typeparam>
    internal abstract class AdornerBase<TVisual> : IAdornerBase<TVisual>
        where TVisual : UIElement
    {
        /// <summary>
        /// Creates a new instance of <see cref="AdornerBase{TVisual}"/>.
        /// </summary>
        /// <param name="service">The adorner service.</param>
        /// <param name="gameSideElement">The associated game-side UIElement.</param>
        protected AdornerBase(UIEditorGameAdornerService service, UIElement gameSideElement)
        {
            Service = service;
            GameSideElement = gameSideElement;
        }

        /// <summary>
        /// Gets the associated game-side UIElement.
        /// </summary>
        public UIElement GameSideElement { get; }

        public bool IsVisible => Visual.Visibility == Visibility.Visible;

        /// <summary>
        /// Gets the visual of this adorner.
        /// </summary>
        public abstract TVisual Visual { get; }

        protected UIEditorGameAdornerService Service { get; }
        
        public virtual void Disable()
        {
            Visual.CanBeHitByUser = false;
        }
        
        public virtual void Enable()
        {
            Visual.CanBeHitByUser = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hide()
        {
            Visual.Visibility = Visibility.Hidden;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Show()
        {
            Visual.Visibility = Visibility.Visible;
        }

        public abstract void Update(Vector3 position);

        protected void InitializeAttachedProperties()
        {
#if DEBUG
            Visual.DependencyProperties.Set(UIEditorGameAdornerService.AssociatedElementPropertyKey, GameSideElement); 
#endif
            Visual.DependencyProperties.Set(UIEditorGameAdornerService.AssociatedAdornerPropertyKey, this);
            Visual.DependencyProperties.Set(UIEditorGameAdornerService.AssociatedElementIdPropertyKey, GameSideElement.Id);
        }
    }
}
