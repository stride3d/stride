// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Data;
using Xenko.Core.Presentation.Graph.Behaviors;

namespace Xenko.Core.Presentation.Graph.ViewModel
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionWrapper : DependencyObject
    {
        public static DependencyProperty BindingProperty = DependencyProperty.Register(
            "Binding", 
            typeof(Binding),
            typeof(NodeGraphBehavior), 
            new PropertyMetadata(OnBindingChanged));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var connectionWrapper = (ConnectionWrapper)d;
            connectionWrapper.OnBindingChanged(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBindingChanged(DependencyPropertyChangedEventArgs e)
        {
            // nothing
        }

        /// <summary>
        /// 
        /// </summary>
        public Binding Binding { get { return (Binding)GetValue(BindingProperty); } set { SetValue(BindingProperty, value); } }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SingleConnectionWrapper : ConnectionWrapper { }    
}
