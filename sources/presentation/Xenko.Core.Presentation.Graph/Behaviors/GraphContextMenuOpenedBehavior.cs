// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using GraphX;
using GraphX.Controls;
using Xenko.Core.Presentation.Extensions;
using Xenko.Core.Presentation.Graph.Helper;

namespace Xenko.Core.Presentation.Graph.Behaviors
{
    public enum ContextMenuEventType
    {
        None,
        Opening,
        Closing,
    }

    public class GraphContextMenuOpenedBehavior : Behavior<GraphAreaBase>
    {
        protected ZoomControl zoomControl;

        public static readonly DependencyProperty EventTypeProperty = DependencyProperty.Register(nameof(EventType), typeof(ContextMenuEventType), typeof(GraphContextMenuOpenedBehavior), new FrameworkPropertyMetadata(ContextMenuEventType.None, EventTypeChanged));

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(GraphContextMenuOpenedBehavior));

        public ContextMenuEventType EventType { get { return (ContextMenuEventType)GetValue(EventTypeProperty); } set { SetValue(EventTypeProperty, value); } }

        /// <summary>
        /// Gets or sets the command to invoke when the event is raised.
        /// </summary>
        public ICommand Command { get { return (ICommand)GetValue(CommandProperty); } set { SetValue(CommandProperty, value); } }

        protected override void OnAttached()
        {
            base.OnAttached();
            RegisterHandler(EventType);
        }

        protected override void OnDetaching()
        {
            UnregisterHandler(EventType);
            base.OnAttached();
        }

        private static void EventTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (GraphContextMenuOpenedBehavior)d;
            if (behavior.AssociatedObject == null)
                return;

            var oldValue = (ContextMenuEventType)e.OldValue;
            behavior.UnregisterHandler(oldValue);
            var newValue = (ContextMenuEventType)e.NewValue;
            behavior.RegisterHandler(newValue);
        }

        private void RegisterHandler(ContextMenuEventType type)
        {
            // TODO: Done on logical tree for now because visual tree is not valid during behavior OnAttachAndLoaded()
            zoomControl = AssociatedObject.FindLogicalParentOfType<ZoomControl>();

            switch (type)
            {
                case ContextMenuEventType.Opening:
                    zoomControl.ContextMenuOpening += ContextMenuEventHandler;
                    break;
                case ContextMenuEventType.Closing:
                    zoomControl.ContextMenuClosing += ContextMenuEventHandler;
                    break;
            }
        }

        private void UnregisterHandler(ContextMenuEventType type)
        {
            switch (type)
            {
                case ContextMenuEventType.Opening:
                    zoomControl.ContextMenuOpening -= ContextMenuEventHandler;
                    break;
                case ContextMenuEventType.Closing:
                    zoomControl.ContextMenuClosing -= ContextMenuEventHandler;
                    break;
            }

            zoomControl = null;
        }

        private void ContextMenuEventHandler(object sender, ContextMenuEventArgs e)
        {
            // Convert position from zoomcontrol space to graph space
            var mousePosition = GetMousePosition(zoomControl, AssociatedObject);

            var cmd = Command;
            if (cmd != null && cmd.CanExecute(mousePosition))
                cmd.Execute(mousePosition);
        }

        public static Point GetMousePosition(UIElement zoomControl, UIElement area)
        {
            // Convert position from zoomcontrol space to graph space
            return zoomControl.TranslatePoint(MouseHelper.GetMousePosition(zoomControl), area);
        }
    }
}
