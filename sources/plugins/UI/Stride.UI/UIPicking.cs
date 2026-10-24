// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.UI;

namespace Stride.UI
{
    /// <summary>
    /// What <see cref="UIPicking"/> needs to know about a UI component from the last frame it was drawn.
    /// </summary>
    internal struct UIPickingTarget
    {
        public RenderUIElement RenderObject;
        public RenderView View;
        public Matrix WorldViewProjection;
        public Vector3 VirtualResolution;
        public Viewport Viewport;
        public Vector2 BackBufferSize;
        public int Frame;
    }

    /// <summary>
    /// Hit-tests pointer input against the UI during the capture phase of <see cref="InputManager"/>, raises the UI
    /// touch and mouse-over events, and captures the pointers the UI uses.
    /// </summary>
    /// <remarks>
    /// Picking uses the matrices and layout of the last drawn frame, because it runs before the current frame is drawn.
    /// A press on the UI keeps the mouse, or the touch pointer, captured until it is released. The release takes effect at
    /// the start of the next capture phase, so the game does not see the release of a press it never saw.
    /// </remarks>
    internal sealed class UIPicking
    {
        private readonly UISystem system;
        private readonly InputManager input;
        private readonly HashSet<UIElement> newlySelectedElementParents = new HashSet<UIElement>();
        private readonly List<PointerEvent> compactedPointerEvents = new List<PointerEvent>();
        private readonly List<(IPointerDevice Device, int PointerId)> pointersToRelease = new List<(IPointerDevice, int)>();
        private readonly List<(RenderUIElement, RenderView)> staleTargets = new List<(RenderUIElement, RenderView)>();
        private bool releaseMouseDrag;

        public UIPicking(UISystem system, InputManager input)
        {
            this.system = system;
            this.input = input;
        }

        /// <summary>
        /// Gets a value indicating whether a mouse press started on the UI and has not been released yet.
        /// </summary>
        public bool MouseDragOwnedByUi { get; private set; }

        /// <summary>
        /// Processes this frame's pointer events against the UI components drawn last.
        /// </summary>
        /// <returns>The element under the mouse cursor, or <c>null</c>.</returns>
        public UIElement Run(List<PointerEvent> events, Dictionary<(RenderUIElement, RenderView), UIPickingTarget> targets, GameTime time)
        {
            ApplyDeferredReleases();

            compactedPointerEvents.Clear();
            CompactPointerEvents(events);

            var latestFrame = int.MinValue;
            foreach (var target in targets.Values)
                latestFrame = Math.Max(latestFrame, target.Frame);

            // Forget UI components that were not drawn in the last frame, so removed components are not kept alive
            staleTargets.Clear();
            foreach (var target in targets.Values)
            {
                if (target.Frame < latestFrame)
                    staleTargets.Add((target.RenderObject, target.View));
            }
            foreach (var key in staleTargets)
                targets.Remove(key);

            UIElement elementUnderMouseCursor = null;
            foreach (var target in targets.Values)
            {
                if (target.RenderObject.Page?.RootElement == null)
                    continue;

                var inverseZViewProj = target.WorldViewProjection;
                inverseZViewProj.Row3 = -inverseZViewProj.Row3;

                var element = UpdateMouseOver(target, ref inverseZViewProj);
                if (element != null)
                    elementUnderMouseCursor = element;

                UpdateTouchEvents(target, ref inverseZViewProj, time);
            }

            ScheduleReleases(events);

            return elementUnderMouseCursor;
        }

        private void ApplyDeferredReleases()
        {
            if (releaseMouseDrag)
            {
                MouseDragOwnedByUi = false;
                releaseMouseDrag = false;
            }

            foreach (var (device, pointerId) in pointersToRelease)
                input.ReleasePointer(device, pointerId, system);
            pointersToRelease.Clear();
        }

        private void ScheduleReleases(List<PointerEvent> events)
        {
            foreach (var pointerEvent in events)
            {
                if (pointerEvent.EventType != PointerEventType.Released && pointerEvent.EventType != PointerEventType.Canceled)
                    continue;

                if (pointerEvent.Device is IMouseDevice)
                {
                    if (MouseDragOwnedByUi)
                        releaseMouseDrag = true;
                }
                else if (ReferenceEquals(pointerEvent.Pointer.CaptureState.GetPointerOwner(pointerEvent.PointerId), system))
                {
                    pointersToRelease.Add((pointerEvent.Pointer, pointerEvent.PointerId));
                }
            }
        }

        private void CaptureOnPress(PointerEvent pointerEvent)
        {
            if (pointerEvent.Device is IMouseDevice)
                MouseDragOwnedByUi = true;
            else
                input.TryCapturePointer(pointerEvent.Pointer, pointerEvent.PointerId, system);
        }

        private void CompactPointerEvents(List<PointerEvent> events)
        {
            // compact all the move events of the frame together
            var aggregatedTranslation = Vector2.Zero;
            for (var index = 0; index < events.Count; ++index)
            {
                var pointerEvent = events[index];

                if (pointerEvent.EventType != PointerEventType.Moved)
                {
                    aggregatedTranslation = Vector2.Zero;
                    compactedPointerEvents.Add(pointerEvent.Clone());
                    continue;
                }

                aggregatedTranslation += pointerEvent.DeltaPosition;

                if (index + 1 >= events.Count || events[index + 1].EventType != PointerEventType.Moved)
                {
                    var compactedMoveEvent = pointerEvent.Clone();
                    compactedMoveEvent.DeltaPosition = aggregatedTranslation;
                    compactedPointerEvents.Add(compactedMoveEvent);
                }
            }
        }

        /// <summary>
        /// Creates a ray in object space based on a screen position and a previously rendered object's WorldViewProjection matrix
        /// </summary>
        private static Ray GetWorldRay(in UIPickingTarget target, Vector2 screenPos, ref Matrix worldViewProj)
        {
            screenPos *= target.BackBufferSize;

            var unprojectedNear = target.Viewport.Unproject(new Vector3(screenPos, 0.0f), ref worldViewProj);
            var unprojectedFar = target.Viewport.Unproject(new Vector3(screenPos, 1.0f), ref worldViewProj);

            var rayDirection = Vector3.Normalize(unprojectedFar - unprojectedNear);
            return new Ray(unprojectedNear, rayDirection);
        }

        /// <summary>
        /// Returns if a screen position is within the borders of a tested ui component
        /// </summary>
        private static bool GetTouchPosition(in UIPickingTarget target, ref Matrix worldViewProj, Vector2 screenPosition, out Ray uiRay)
        {
            uiRay = new Ray(new Vector3(float.NegativeInfinity), new Vector3(0, 1, 0));

            // TODO XK-3367 This only works for a single view

            // Get a touch ray in object (UI component) space
            var touchRay = GetWorldRay(target, screenPosition, ref worldViewProj);

            // If the click point is outside the canvas ignore any further testing
            var resolution = target.VirtualResolution;
            var dist = -touchRay.Position.Z / touchRay.Direction.Z;
            if (Math.Abs(touchRay.Position.X + touchRay.Direction.X * dist) > resolution.X * 0.5f ||
                Math.Abs(touchRay.Position.Y + touchRay.Direction.Y * dist) > resolution.Y * 0.5f)
                return false;

            uiRay = touchRay;
            return true;
        }

        private void UpdateTouchEvents(in UIPickingTarget target, ref Matrix worldViewProj, GameTime gameTime)
        {
            var state = target.RenderObject;
            var rootElement = state.Page.RootElement;
            var intersectionPoint = Vector3.Zero;
            var lastTouchPosition = new Vector2(float.NegativeInfinity);

            // analyze pointer event input and trigger UI touch events depending on hit Tests
            foreach (var pointerEvent in compactedPointerEvents)
            {
                // performance optimization: skip all the events that started outside of the UI
                var lastTouchedElement = state.LastTouchedElement;
                if (lastTouchedElement == null && pointerEvent.EventType != PointerEventType.Pressed)
                    continue;

                var time = gameTime.Total;

                var currentTouchPosition = pointerEvent.Position;
                var currentTouchedElement = lastTouchedElement;

                // re-calculate the element under cursor if click position changed.
                if (lastTouchPosition != currentTouchPosition)
                {
                    if (!GetTouchPosition(target, ref worldViewProj, currentTouchPosition, out var uiRay))
                        continue;

                    currentTouchedElement = UIRenderFeature.GetElementAtScreenPosition(rootElement, ref uiRay, ref worldViewProj, ref intersectionPoint);
                }

                if (pointerEvent.EventType == PointerEventType.Pressed || pointerEvent.EventType == PointerEventType.Released)
                    state.LastIntersectionPoint = intersectionPoint;

                // TODO: add the pointer type to the event args?
                var touchEvent = new TouchEventArgs
                {
                    Action = pointerEvent.EventType switch
                    {
                        PointerEventType.Pressed => TouchAction.Down,
                        PointerEventType.Moved => TouchAction.Move,
                        PointerEventType.Released => TouchAction.Up,
                        PointerEventType.Canceled => TouchAction.Move,
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    Timestamp = time,
                    ScreenPosition = currentTouchPosition,
                    ScreenTranslation = pointerEvent.DeltaPosition,
                    WorldPosition = intersectionPoint,
                    WorldTranslation = intersectionPoint - state.LastIntersectionPoint
                };

                switch (pointerEvent.EventType)
                {
                    case PointerEventType.Pressed:
                        if (currentTouchedElement != null)
                            CaptureOnPress(pointerEvent);
                        currentTouchedElement?.RaiseTouchDownEvent(touchEvent);
                        break;

                    case PointerEventType.Released:
                        // generate enter/leave events if we passed from an element to another without move events
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeaveTouchEvents(currentTouchedElement, lastTouchedElement, touchEvent);

                        // trigger the up event
                        currentTouchedElement?.RaiseTouchUpEvent(touchEvent);
                        break;

                    case PointerEventType.Moved:
                        // first notify the move event (even if the touched element changed in between it is still coherent in one of its parents)
                        currentTouchedElement?.RaiseTouchMoveEvent(touchEvent);

                        // then generate enter/leave events if we passed from an element to another
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeaveTouchEvents(currentTouchedElement, lastTouchedElement, touchEvent);
                        break;

                    case PointerEventType.Canceled:
                        // generate enter/leave events if we passed from an element to another without move events
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeaveTouchEvents(currentTouchedElement, lastTouchedElement, touchEvent);

                        // then raise leave event to all the hierarchy of the previously selected element.
                        var element = currentTouchedElement;
                        while (element != null)
                        {
                            if (element.IsTouched)
                                element.RaiseTouchLeaveEvent(touchEvent);
                            element = element.VisualParent;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                lastTouchPosition = currentTouchPosition;
                state.LastTouchedElement = currentTouchedElement;
                state.LastIntersectionPoint = intersectionPoint;
            }
        }

        private UIElement UpdateMouseOver(in UIPickingTarget target, ref Matrix worldViewProj)
        {
            if (input == null || !input.HasMouse)
                return null;

            var state = target.RenderObject;
            var intersectionPoint = Vector3.Zero;
            var mousePosition = input.MousePosition;
            var rootElement = state.Page.RootElement;
            var lastMouseOverElement = state.LastMouseOverElement;

            UIElement mouseOverElement = lastMouseOverElement;

            // determine currently overred element.
            if (mousePosition != state.LastMousePosition
                || (lastMouseOverElement?.RequiresMouseOverUpdate ?? false))
            {
                if (!GetTouchPosition(target, ref worldViewProj, mousePosition, out var uiRay))
                    return null;

                mouseOverElement = UIRenderFeature.GetElementAtScreenPosition(rootElement, ref uiRay, ref worldViewProj, ref intersectionPoint);
            }

            // find the common parent between current and last overred elements
            var commonElement = FindCommonParent(mouseOverElement, lastMouseOverElement);

            // disable mouse over state to previously overred hierarchy
            var parent = lastMouseOverElement;
            while (parent != commonElement && parent != null)
            {
                parent.RequiresMouseOverUpdate = false;

                parent.MouseOverState = MouseOverState.MouseOverNone;
                parent = parent.VisualParent;
            }

            // enable mouse over state to currently overred hierarchy
            if (mouseOverElement != null)
            {
                // the element itself
                mouseOverElement.MouseOverState = MouseOverState.MouseOverElement;

                // its hierarchy
                parent = mouseOverElement.VisualParent;
                while (parent != null)
                {
                    if (parent.IsHierarchyEnabled)
                        parent.MouseOverState = MouseOverState.MouseOverChild;

                    parent = parent.VisualParent;
                }
            }

            // update cached values
            state.LastMouseOverElement = mouseOverElement;
            state.LastMousePosition = mousePosition;
            return mouseOverElement;
        }

        private UIElement FindCommonParent(UIElement element1, UIElement element2)
        {
            // build the list of the parents of the newly selected element
            newlySelectedElementParents.Clear();
            var newElementParent = element1;
            while (newElementParent != null)
            {
                newlySelectedElementParents.Add(newElementParent);
                newElementParent = newElementParent.VisualParent;
            }

            // find the common element into the previously and newly selected element hierarchy
            var commonElement = element2;
            while (commonElement != null && !newlySelectedElementParents.Contains(commonElement))
                commonElement = commonElement.VisualParent;

            return commonElement;
        }

        private void ThrowEnterAndLeaveTouchEvents(UIElement currentElement, UIElement previousElement, TouchEventArgs touchEvent)
        {
            var commonElement = FindCommonParent(currentElement, previousElement);

            // raise leave events to the hierarchy: previousElt -> commonElementParent
            var previousElementParent = previousElement;
            while (previousElementParent != commonElement && previousElementParent != null)
            {
                if (previousElementParent.IsHierarchyEnabled && previousElementParent.IsTouched)
                {
                    touchEvent.Handled = false; // reset 'handled' because it corresponds to another event
                    previousElementParent.RaiseTouchLeaveEvent(touchEvent);
                }
                previousElementParent = previousElementParent.VisualParent;
            }

            // raise enter events to the hierarchy: newElt -> commonElementParent
            var newElementParent = currentElement;
            while (newElementParent != commonElement && newElementParent != null)
            {
                if (newElementParent.IsHierarchyEnabled && !newElementParent.IsTouched)
                {
                    touchEvent.Handled = false; // reset 'handled' because it corresponds to another event
                    newElementParent.RaiseTouchEnterEvent(touchEvent);
                }
                newElementParent = newElementParent.VisualParent;
            }
        }
    }
}
