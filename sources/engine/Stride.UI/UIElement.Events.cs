// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core;
using Stride.Input;
using Stride.UI.Events;

namespace Stride.UI
{
    public abstract partial class UIElement
    {
        #region Routed Events

        private static readonly RoutedEvent<PointerEventArgs> PreviewPointerPressedEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PreviewPointerPressed", RoutingStrategy.Tunnel, typeof(UIElement));

        private static readonly RoutedEvent<PointerEventArgs> PreviewPointerMoveEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PreviewPointerMove", RoutingStrategy.Tunnel, typeof(UIElement));

        private static readonly RoutedEvent<PointerEventArgs> PreviewPointerReleasedEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PreviewPointerReleased", RoutingStrategy.Tunnel, typeof(UIElement));

        private static readonly RoutedEvent<PointerEventArgs> PointerPressedEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PointerPressed", RoutingStrategy.Bubble, typeof(UIElement));

        private static readonly RoutedEvent<PointerEventArgs> PointerEnterEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PointerEnter", RoutingStrategy.Direct, typeof(UIElement));

        private static readonly RoutedEvent<PointerEventArgs> PointerLeaveEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PointerLeave", RoutingStrategy.Direct, typeof(UIElement));

        private static readonly RoutedEvent<PointerEventArgs> PointerMoveEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PointerMove", RoutingStrategy.Bubble, typeof(UIElement));

        private static readonly RoutedEvent<PointerEventArgs> PointerReleaseEvent =
            EventManager.RegisterRoutedEvent<PointerEventArgs>("PointerReleased", RoutingStrategy.Bubble, typeof(UIElement));

        private static readonly RoutedEvent<KeyEventArgs> KeyPressedEvent =
            EventManager.RegisterRoutedEvent<KeyEventArgs>("KeyPressed", RoutingStrategy.Bubble, typeof(UIElement));

        private static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            EventManager.RegisterRoutedEvent<KeyEventArgs>("KeyDown", RoutingStrategy.Bubble, typeof(UIElement));

        private static readonly RoutedEvent<KeyEventArgs> KeyReleasedEvent =
            EventManager.RegisterRoutedEvent<KeyEventArgs>("KeyReleased", RoutingStrategy.Bubble, typeof(UIElement));

        private static readonly RoutedEvent<TextEventArgs> TextInputEvent =
            EventManager.RegisterRoutedEvent<TextEventArgs>("TextInput", RoutingStrategy.Bubble, typeof(UIElement));

        #endregion

        private static readonly Queue<List<RoutedEventHandlerInfo>> RoutedEventHandlerInfoListPool = new Queue<List<RoutedEventHandlerInfo>>();

        static UIElement()
        {
            // register the class handlers
            EventManager.RegisterClassHandler(typeof(UIElement), PreviewPointerPressedEvent, PreviewPointerPressedClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), PreviewPointerMoveEvent, PreviewPointerMoveClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), PreviewPointerReleasedEvent, PreviewPointerReleasedClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), PointerPressedEvent, PointerPressedClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), PointerEnterEvent, PointerEnterClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), PointerLeaveEvent, PointerLeaveClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), PointerMoveEvent, PointerMoveClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), PointerReleaseEvent, PointerReleasedClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), KeyPressedEvent, KeyPressedClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), KeyDownEvent, KeyDownClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), KeyReleasedEvent, KeyReleasedClassHandler);
            EventManager.RegisterClassHandler(typeof(UIElement), TextInputEvent, TextInputClassHandler);
        }

        /// <summary>
        /// Gets a value indicating whether a pointer is pressed down on the <see cref="UIElement"/>.
        /// </summary>
        [DataMemberIgnore]
        public bool IsPointerDown { get; internal set; }

        /// <summary>
        /// Gets the current state of the pointer over the UI element.
        /// </summary>
        /// <remarks>
        /// Only elements that can be clicked by user can have the <c>PointerOverState.Self</c> value. 
        /// That is element that have <see cref="CanBeHitByUser"/> set to <value>true</value>
        /// </remarks>
        [DataMemberIgnore]
        public PointerOverState PointerOverState
        {
            get { return pointerOverState; }
            internal set
            {
                var oldValue = pointerOverState;
                if (oldValue == value)
                    return;

                pointerOverState = value;

                MouseOverStateChanged?.Invoke(this, new PropertyChangedArgs<PointerOverState> { NewValue = value, OldValue = oldValue });
            }
        }

        /// <summary>
        /// Gets or sets whether this element requires a mouse over check.
        /// </summary>
        /// <remarks>
        /// By default, the engine does not check whether <see cref="PointerOverState"/>
        /// of the element is changed while the cursor is still. This behavior is 
        /// overriden when this parameter is set to true, which forces the engine to
        /// check for changes of <see cref="PointerOverState"/>.
        /// The engine sets this to true when the layout of the element changes.
        /// </remarks>
        [DataMemberIgnore]
        public bool RequiresMouseOverUpdate { get; set; }

        internal void PropagateRoutedEvent(RoutedEventArgs e)
        {
            var routedEvent = e.RoutedEvent;

            // propagate first if tunneling
            if (routedEvent.RoutingStrategy == RoutingStrategy.Tunnel)
                VisualParent?.PropagateRoutedEvent(e);

            // Trigger the class handler
            var classHandler = EventManager.GetClassHandler(GetType(), routedEvent);
            if (classHandler != null && (classHandler.HandledEventToo || !e.Handled))
                classHandler.Invoke(this, e);

            // Trigger instance handlers
            if (eventsToHandlers.TryGetValue(routedEvent, out var handlers))
            {
                // get a list of handler from the pool where we can copy the handler to trigger
                if (RoutedEventHandlerInfoListPool.Count == 0)
                    RoutedEventHandlerInfoListPool.Enqueue(new List<RoutedEventHandlerInfo>());
                var pooledList = RoutedEventHandlerInfoListPool.Dequeue();

                // copy the RoutedEventHandlerEventInfo list into a list of the pool in order to be able to modify the handler list in the handler itself
                pooledList.AddRange(handlers);

                // iterate on the pooled list to invoke handlers
                foreach (var handlerInfo in pooledList)
                {
                    if (handlerInfo.HandledEventToo || !e.Handled)
                        handlerInfo.Invoke(this, e);
                }

                // add the pooled list back to the pool.
                pooledList.Clear(); // avoid to keep dead references
                RoutedEventHandlerInfoListPool.Enqueue(pooledList);
            }

            // propagate afterwards if bubbling
            if (routedEvent.RoutingStrategy == RoutingStrategy.Bubble)
                VisualParent?.PropagateRoutedEvent(e);
        }

        /// <summary>
        /// Raises a specific routed event. The <see cref="RoutedEvent"/> to be raised is identified within the <see cref="RoutedEventArgs"/> instance 
        /// that is provided (as the <see cref="RoutedEvent"/> property of that event data).
        /// </summary>
        /// <param name="e">A <see cref="RoutedEventArgs"/> that contains the event data and also identifies the event to raise.</param>
        /// <exception cref="ArgumentNullException"><paramref name="e"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The type of the routed event argument <paramref name="e"/> does not match the event handler second argument type.</exception>
        public void RaiseEvent(RoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.RoutedEvent == null)
                return;

            if (!e.RoutedEvent.HandlerSecondArgumentType.GetTypeInfo().IsAssignableFrom(e.GetType().GetTypeInfo()))
                throw new InvalidOperationException("The type of second parameter of the handler (" + e.RoutedEvent.HandlerSecondArgumentType
                                                    + ") is not assignable from the parameter 'e' type (" + e.GetType() + ").");

            var sourceWasNull = e.Source == null;
            if (sourceWasNull) // set the source to default if needed
                e.Source = this;

            e.StartEventRouting();

            PropagateRoutedEvent(e);

            e.EndEventRouting();

            if (sourceWasNull) // reset the source if it was not explicitly set (event might be reused again for other sources)
                e.Source = null;
        }

        /// <summary>
        /// Adds a routed event handler for a specified routed event, adding the handler to the handler collection on the current element. 
        /// Specify handledEventsToo as true to have the provided handler be invoked for routed event that had already been marked as handled by another element along the event route.
        /// </summary>
        /// <param name="routedEvent">An identifier for the routed event to be handled.</param>
        /// <param name="handler">A reference to the handler implementation.</param>
        /// <param name="handledEventsToo">true to register the handler such that it is invoked even when the routed event is marked handled in its event data; 
        /// false to register the handler with the default condition that it will not be invoked if the routed event is already marked handled.</param>
        /// <exception cref="ArgumentNullException">Provided handler or routed event is null.</exception>
        public void AddHandler<T>(RoutedEvent<T> routedEvent, EventHandler<T> handler, bool handledEventsToo = false) where T : RoutedEventArgs
        {
            if (routedEvent == null) throw new ArgumentNullException(nameof(routedEvent));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!eventsToHandlers.ContainsKey(routedEvent))
                eventsToHandlers[routedEvent] = new List<RoutedEventHandlerInfo>();

            eventsToHandlers[routedEvent].Add(new RoutedEventHandlerInfo<T>(handler, handledEventsToo));
        }

        /// <summary>
        /// Removes the specified routed event handler from this element.
        /// </summary>
        /// <param name="routedEvent">The identifier of the routed event for which the handler is attached.</param>
        /// <param name="handler">The specific handler implementation to remove from the event handler collection on this element.</param>
        /// <exception cref="ArgumentNullException">Provided handler or routed event is null.</exception>
        public void RemoveHandler<T>(RoutedEvent<T> routedEvent, EventHandler<T> handler) where T : RoutedEventArgs
        {
            if (routedEvent == null) throw new ArgumentNullException(nameof(routedEvent));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!eventsToHandlers.ContainsKey(routedEvent))
                return;

            eventsToHandlers[routedEvent].Remove(new RoutedEventHandlerInfo<T>(handler));
        }

        private readonly Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>> eventsToHandlers = new Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>();

        #region Events

        /// <summary>
        /// Occurs when the value of the <see cref="PointerOverState"/> property changed.
        /// </summary>
        /// <remarks>This event is not a routed event</remarks>
        public event PropertyChangedHandler<PointerOverState> MouseOverStateChanged;

        /// <summary>
        /// Occurs when the user starts pressing the <see cref="UIElement"/> with a pointer.
        /// </summary>
        /// <remarks>A press event is tunneling</remarks>
        public event EventHandler<PointerEventArgs> PreviewPointerPressed
        {
            add { AddHandler(PreviewPointerPressedEvent, value); }
            remove { RemoveHandler(PreviewPointerPressedEvent, value); }
        }

        /// <summary>
        /// Occurs when the user moves a pointer on the <see cref="UIElement"/>. 
        /// </summary>
        /// <remarks>A move event is tunneling</remarks>
        public event EventHandler<PointerEventArgs> PreviewPointerMove
        {
            add { AddHandler(PreviewPointerMoveEvent, value); }
            remove { RemoveHandler(PreviewPointerMoveEvent, value); }
        }

        /// <summary>
        /// Occurs when the user stops pressing the <see cref="UIElement"/>.
        /// </summary>
        /// <remarks>A release event is tunneling</remarks>
        public event EventHandler<PointerEventArgs> PreviewPointerReleased
        {
            add { AddHandler(PreviewPointerReleasedEvent, value); }
            remove { RemoveHandler(PreviewPointerReleasedEvent, value); }
        }

        /// <summary>
        /// Occurs when the user starts pressing the <see cref="UIElement"/>.
        /// </summary>
        /// <remarks>A press event is bubbling.</remarks>
        public event EventHandler<PointerEventArgs> PointerPressed
        {
            add { AddHandler(PointerPressedEvent, value); }
            remove { RemoveHandler(PointerPressedEvent, value); }
        }

        /// <summary>
        /// Occurs when the user moves a pointer into <see cref="UIElement"/>. 
        /// That is when a pointer was on the screen outside of the element and moved inside the element.
        /// </summary>
        /// <remarks>A enter event is bubbling</remarks>
        public event EventHandler<PointerEventArgs> PointerEnter
        {
            add { AddHandler(PointerEnterEvent, value); }
            remove { RemoveHandler(PointerEnterEvent, value); }
        }

        /// <summary>
        /// Occurs when the user moves a pointer from the <see cref="UIElement"/>. 
        /// That is when a pointer was inside of the element and moved on the screen outside of the element.
        /// </summary>
        /// <remarks>A leave event is bubbling</remarks>
        public event EventHandler<PointerEventArgs> PointerLeave
        {
            add { AddHandler(PointerLeaveEvent, value); }
            remove { RemoveHandler(PointerLeaveEvent, value); }
        }

        /// <summary>
        /// Occurs when the user move a pointer inside the <see cref="UIElement"/>.
        /// That is when a pointer was already on the element and moved from its previous position.
        /// </summary>
        /// <remarks>A move event is bubbling</remarks>
        public event EventHandler<PointerEventArgs> PointerMove
        {
            add { AddHandler(PointerMoveEvent, value); }
            remove { RemoveHandler(PointerMoveEvent, value); }
        }

        /// <summary>
        /// Occurs when the user stops pressing the <see cref="UIElement"/>.
        /// </summary>
        /// <remarks>A release event is bubbling</remarks>
        public event EventHandler<PointerEventArgs> PointerReleased
        {
            add { AddHandler(PointerReleaseEvent, value); }
            remove { RemoveHandler(PointerReleaseEvent, value); }
        }

        /// <summary>
        /// Occurs when the element has the focus and the user press a key on the keyboard.
        /// </summary>
        /// <remarks>A key pressed event is bubbling</remarks>
        internal event EventHandler<KeyEventArgs> KeyPressed
        {
            add { AddHandler(KeyPressedEvent, value); }
            remove { RemoveHandler(KeyPressedEvent, value); }
        }

        /// <summary>
        /// Occurs when the element has the focus and the user maintains a key pressed on the keyboard.
        /// </summary>
        /// <remarks>A key down event is bubbling</remarks>
        internal event EventHandler<KeyEventArgs> KeyDown
        {
            add { AddHandler(KeyDownEvent, value); }
            remove { RemoveHandler(KeyDownEvent, value); }
        }

        /// <summary>
        /// Occurs when the element has the focus and the user release a key on the keyboard.
        /// </summary>
        /// <remarks>A key released event is bubbling</remarks>
        internal event EventHandler<KeyEventArgs> KeyReleased
        {
            add { AddHandler(KeyReleasedEvent, value); }
            remove { RemoveHandler(KeyReleasedEvent, value); }
        }

        /// <summary>
        /// Occurs when the element has the focus and the user inputs text on a text input device (keyboard)
        /// </summary>
        internal event EventHandler<TextEventArgs> Input
        {
            add { AddHandler(TextInputEvent, value); }
            remove { RemoveHandler(TextInputEvent, value); }
        }

        #endregion

        #region Internal Event Raiser

        internal void RaisePointerPressedEvent(PointerEventArgs pointerArgs)
        {
            pointerArgs.RoutedEvent = PreviewPointerPressedEvent;
            RaiseEvent(pointerArgs);

            pointerArgs.RoutedEvent = PointerPressedEvent;
            RaiseEvent(pointerArgs);
        }

        internal void RaisePointerEnterEvent(PointerEventArgs pointerArgs)
        {
            pointerArgs.RoutedEvent = PointerEnterEvent;
            RaiseEvent(pointerArgs);
        }

        internal void RaisePointerLeaveEvent(PointerEventArgs pointerArgs)
        {
            pointerArgs.RoutedEvent = PointerLeaveEvent;
            RaiseEvent(pointerArgs);
        }

        internal void RaisePointerMoveEvent(PointerEventArgs pointerArgs)
        {
            pointerArgs.RoutedEvent = PreviewPointerMoveEvent;
            RaiseEvent(pointerArgs);

            pointerArgs.RoutedEvent = PointerMoveEvent;
            RaiseEvent(pointerArgs);
        }

        internal void RaisePointerReleasedEvent(PointerEventArgs pointerArgs)
        {
            pointerArgs.RoutedEvent = PreviewPointerReleasedEvent;
            RaiseEvent(pointerArgs);

            pointerArgs.RoutedEvent = PointerReleaseEvent;
            RaiseEvent(pointerArgs);
        }

        internal void RaiseKeyPressedEvent(KeyEventArgs keyEventArgs)
        {
            keyEventArgs.RoutedEvent = KeyPressedEvent;
            RaiseEvent(keyEventArgs);
        }

        internal void RaiseKeyDownEvent(KeyEventArgs keyEventArgs)
        {
            keyEventArgs.RoutedEvent = KeyDownEvent;
            RaiseEvent(keyEventArgs);
        }

        internal void RaiseKeyReleasedEvent(KeyEventArgs keyEventArgs)
        {
            keyEventArgs.RoutedEvent = KeyReleasedEvent;
            RaiseEvent(keyEventArgs);
        }

        internal void RaiseTextInputEvent(TextEventArgs textInputEventArgs)
        {
            textInputEventArgs.RoutedEvent = TextInputEvent;
            RaiseEvent(textInputEventArgs);
        }

        #endregion

        #region Class Event Handlers

        private static void PreviewPointerPressedClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPreviewPointerPressed(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PreviewPointerPressed"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPreviewPointerPressed(PointerEventArgs args)
        {
            IsPointerDown = true;
        }

        private static void PreviewPointerMoveClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPreviewPointerMove(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PreviewPointerMove"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPreviewPointerMove(PointerEventArgs args)
        {
        }

        private static void PreviewPointerReleasedClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPreviewPointerReleased(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PreviewPointerReleased"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPreviewPointerReleased(PointerEventArgs args)
        {
            IsPointerDown = false;
        }

        private static void PointerPressedClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPointerPressed(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PointerPressed"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPointerPressed(PointerEventArgs args)
        {
        }

        private static void PointerEnterClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPointerEnter(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PointerEnter"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPointerEnter(PointerEventArgs args)
        {
            IsPointerDown = true;
        }

        private static void PointerLeaveClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPointerLeave(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PointerLeave"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPointerLeave(PointerEventArgs args)
        {
            IsPointerDown = false;
        }

        private static void PointerMoveClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPointerMove(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PointerMove"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPointerMove(PointerEventArgs args)
        {
        }

        private static void PointerReleasedClassHandler(object sender, PointerEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnPointerReleased(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="PointerReleased"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnPointerReleased(PointerEventArgs args)
        {
        }

        private static void KeyPressedClassHandler(object sender, KeyEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnKeyPressed(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="KeyPressed"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        internal virtual void OnKeyPressed(KeyEventArgs args)
        {
        }

        private static void KeyDownClassHandler(object sender, KeyEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnKeyDown(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="Input"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        internal virtual void OnTextInput(TextEventArgs args)
        {
        }

        private static void TextInputClassHandler(object sender, TextEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnTextInput(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="KeyDown"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        internal virtual void OnKeyDown(KeyEventArgs args)
        {
        }

        private static void KeyReleasedClassHandler(object sender, KeyEventArgs args)
        {
            var uiElementSender = (UIElement)sender;
            if (uiElementSender.IsHierarchyEnabled)
                uiElementSender.OnKeyReleased(args);
        }

        /// <summary>
        /// The class handler of the event <see cref="KeyReleased"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        internal virtual void OnKeyReleased(KeyEventArgs args)
        {
        }

        #endregion
    }
}
