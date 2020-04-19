// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.UI.Events
{
    /// <summary>
    /// Contains state information and event data associated with a routed event.
    /// </summary>
    public class RoutedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value that indicates the present state of the event handling for a routed event as it travels the route.
        /// </summary>
        public bool Handled { get; set; }

        private RoutedEvent routedEvent;

        private UIElement source;

        protected bool IsBeingRouted { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="RoutedEvent"/> associated with this RoutedEventArgs instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Attempted to change the RoutedEvent value while the event is being routed.</exception>
        public RoutedEvent RoutedEvent 
        {
            get { return routedEvent; }
            set
            {
                if (IsBeingRouted)
                    throw new InvalidOperationException("The routed event cannot be changed while the event is being routed.");

                routedEvent = value;
            }
        }

        /// <summary>
        /// Gets or sets a reference to the object that raised the event.
        /// </summary>
        /// <exception cref="InvalidOperationException">Attempted to change the source value while the event is being routed.</exception>
        public UIElement Source 
        {
            get { return source; }
            set
            {
                if (IsBeingRouted)
                    throw new InvalidOperationException("The routed event cannot be changed while the event is being routed.");

                source = value;
            }
        }

        /// <summary>
        /// Indicate to the <see cref="RoutedEventArgs"/> that the event has started being routed.
        /// </summary>
        internal void StartEventRouting()
        {
            IsBeingRouted = true;
        }

        /// <summary>
        /// Indicate to the <see cref="RoutedEventArgs"/> that the event has ended being routed.
        /// </summary>
        internal void EndEventRouting()
        {
            IsBeingRouted = false;
        }

        /// <summary>
        /// Initializes a new instance of the RoutedEventArgs class.
        /// </summary>
        /// <remarks>
        /// When using this parameterless constructor, all public properties of the new <see cref="RoutedEventArgs"/> instance assume the following default values: 
        /// <see cref="RoutedEvent"/> = null, <see cref="Handled"/> = false, <see cref="Source"/> = null.
        /// Null values for <see cref="Source"/> only mean that the <see cref="RoutedEventArgs"/> data makes no attempt to specify the source. 
        /// When this instance is used in a call to <see cref="UIElement.RaiseEvent"/>,  the <see cref="Source"/> value is populated based on the element 
        /// that raised the event and are passed on to listeners through the routing.
        /// </remarks>
        public RoutedEventArgs()
            : this(null, null)
        {
        }
        /// <summary>
        /// Initializes a new instance of the RoutedEventArgs class, using the supplied routed event identifier.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier for this instance of the <see cref="RoutedEventArgs"/> class.</param>
        /// <remarks>
        /// When using this constructor, unspecified  public properties of the new <see cref="RoutedEventArgs"/> instance assume the following default values: 
        /// <see cref="Handled"/> = false, <see cref="Source"/> = null.
        /// Null values for <see cref="Source"/> only mean that the <see cref="RoutedEventArgs"/> data makes no attempt to specify the source. 
        /// When this instance is used in a call to <see cref="UIElement.RaiseEvent"/>,  the <see cref="Source"/> value is populated based on the element 
        /// that raised the event and are passed on to listeners through the routing.
        /// </remarks>
        public RoutedEventArgs(RoutedEvent routedEvent)
            :this(routedEvent, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutedEventArgs"/> class, using the supplied routed event identifier, and providing the opportunity to declare a different source for the event.
        /// </summary>
        /// <param name="routedEvent">The routed event identifier for this instance of the <see cref="RoutedEventArgs"/> class.</param>
        /// <param name="source">An alternate source that will be reported when the event is handled. This pre-populates the <see cref="Source"/> property.</param>
        /// <remarks>
        /// When using this constructor, unspecified public properties of the new <see cref="RoutedEventArgs"/> instance assume the following default values: 
        /// <see cref="Handled"/> = false.</remarks>
        public RoutedEventArgs(RoutedEvent routedEvent, UIElement source)
        {
            RoutedEvent = routedEvent;
            Source = source;
        }
    }
}
