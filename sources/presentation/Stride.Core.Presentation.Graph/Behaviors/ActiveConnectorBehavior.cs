// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Presentation.Behaviors;

namespace Stride.Core.Presentation.Graph.Behaviors
{
    /// <summary>
    /// This behavior is mandatory on slots so that edges start/end positions can be computed.
    /// </summary>
    public sealed class ActiveConnectorBehavior : DeferredBehaviorBase<FrameworkElement>
    {
        #region IDropHandler Interface
        /// <summary>
        /// 
        /// </summary>
        public interface IActiveConnectorHandler
        {
            void OnAttached(FrameworkElement slot);
            void OnDetached(FrameworkElement slot);
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty ActiveConnectorHandlerProperty = DependencyProperty.Register("ActiveConnectorHandler", typeof(IActiveConnectorHandler), typeof(ActiveConnectorBehavior), new PropertyMetadata(OnActiveConnectorHandlerChanged));
        public static DependencyProperty SlotProperty = DependencyProperty.Register("Slot", typeof(object), typeof(ActiveConnectorBehavior));
        #endregion

        protected override void OnAttachedAndLoaded()
        {
            base.OnAttachedAndLoaded();

            ActiveConnectorHandler?.OnAttached(AssociatedObject);
        }

        protected override void OnDetachingAndUnloaded()
        {
            ActiveConnectorHandler?.OnDetached(AssociatedObject);

            base.OnDetachingAndUnloaded();
        }

        private static void OnActiveConnectorHandlerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ActiveConnectorBehavior)d;

            // Was it already loaded?
            if (behavior.AssociatedObject != null && behavior.AssociatedObject.IsLoaded)
            {
                // If yes, update
                ((IActiveConnectorHandler)e.OldValue)?.OnDetached(behavior.AssociatedObject);
                ((IActiveConnectorHandler)e.NewValue)?.OnAttached(behavior.AssociatedObject);
            }
        }

        #region Properties
        public IActiveConnectorHandler ActiveConnectorHandler { get { return (IActiveConnectorHandler)GetValue(ActiveConnectorHandlerProperty); } set { SetValue(ActiveConnectorHandlerProperty, value); } }
        public object Slot { get { return GetValue(SlotProperty); } set { SetValue(SlotProperty, value); } }
        #endregion
    }
}
