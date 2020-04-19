// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Presentation.Behaviors;

namespace Stride.Core.Presentation.Graph.Behaviors
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ConnectorDropBehavior : DeferredBehaviorBase<FrameworkElement>
    {
        #region IDropHandler Interface
        /// <summary>
        /// 
        /// </summary>
        public interface IDropHandler
        {
            void OnDragOver(object sender, DragEventArgs e);
            void OnDrop(object sender, DragEventArgs e);
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty DropHandlerProperty = DependencyProperty.Register("DropHandler", typeof(IDropHandler), typeof(ConnectorDropBehavior));
        #endregion

        #region Constructor
        /// <summary>
        /// 
        /// </summary>
        public ConnectorDropBehavior()
        {
            // nothing
        }
        #endregion

        #region Attach & Detach Methods
        /// <summary>
        /// 
        /// </summary>
        protected override void OnAttachedAndLoaded()
        {
            base.OnAttachedAndLoaded();
            
            AssociatedObject.Drop += OnDropEvent;
            AssociatedObject.DragOver += OnDragOverEvent;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDetachingAndUnloaded()
        {
            AssociatedObject.Drop -= OnDropEvent;
            AssociatedObject.DragOver -= OnDragOverEvent;
            base.OnDetachingAndUnloaded();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDropEvent(object sender, DragEventArgs e)
        {

            if (DropHandler != null)
            {
                DropHandler.OnDrop(sender, e);                
            }

            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDragOverEvent(object sender, DragEventArgs e)
        {
            if (DropHandler != null)
            {
                DropHandler.OnDragOver(sender, e);
                
            }
        }
        #endregion

        #region Properties
        public IDropHandler DropHandler { get { return (IDropHandler)GetValue(DropHandlerProperty); } set { SetValue(DropHandlerProperty, value); } }
        #endregion
    }
}
