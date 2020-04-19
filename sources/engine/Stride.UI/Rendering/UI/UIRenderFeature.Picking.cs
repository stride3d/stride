// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.UI;

namespace Stride.Rendering.UI
{
    public partial class UIRenderFeature 
    {
        // object to avoid allocation at each element leave event
        private readonly HashSet<UIElement> newlySelectedElementParents = new HashSet<UIElement>();

        private readonly List<PointerEvent> compactedPointerEvents = new List<PointerEvent>();


        

        partial void PickingUpdate(RenderUIElement renderUIElement, Viewport viewport, ref Matrix worldViewProj, GameTime drawTime, ref UIElement elementUnderMouseCursor)

        {
            if (renderUIElement.Page?.RootElement == null)
                return;

            var inverseZViewProj = worldViewProj;
            inverseZViewProj.Row3 = -inverseZViewProj.Row3;

            elementUnderMouseCursor = UpdateMouseOver(ref viewport, ref inverseZViewProj, renderUIElement);
            UpdateTouchEvents(ref viewport, ref inverseZViewProj, renderUIElement, drawTime);
        }

        partial void PickingClear()
        {
            // clear the list of compacted pointer events of time frame
            ClearPointerEvents();
        }

        partial void PickingPrepare()
        {
            // compact all the pointer events that happened since last frame to avoid performing useless hit tests.
            CompactPointerEvents();
        }

        private void CompactPointerEvents()
        {
            if (input == null) // no input for thumbnails
                return;

            // compact all the move events of the frame together
            var aggregatedTranslation = Vector2.Zero;
            for (var index = 0; index < input.PointerEvents.Count; ++index)
            {
                var pointerEvent = input.PointerEvents[index];

                if (pointerEvent.EventType != PointerEventType.Moved)
                {
                    aggregatedTranslation = Vector2.Zero;
                    compactedPointerEvents.Add(pointerEvent.Clone());
                    continue;
                }

                aggregatedTranslation += pointerEvent.DeltaPosition;

                if (index + 1 >= input.PointerEvents.Count || input.PointerEvents[index + 1].EventType != PointerEventType.Moved)
                {
                    var compactedMoveEvent = pointerEvent.Clone();
                    compactedMoveEvent.DeltaPosition = aggregatedTranslation;
                    compactedPointerEvents.Add(compactedMoveEvent);
                }
            }
        }

        private void ClearPointerEvents()
        {
            compactedPointerEvents.Clear();
        }

        /// <summary>
        /// Creates a ray in object space based on a screen position and a previously rendered object's WorldViewProjection matrix
        /// </summary>
        /// <param name="viewport">The viewport in which the object was rendered</param>
        /// <param name="screenPos">The click position on screen in normalized (0..1, 0..1) range</param>
        /// <param name="worldViewProj">The WorldViewProjection matrix with which the object was last rendered in the view</param>
        /// <returns></returns>
        private Ray GetWorldRay(ref Viewport viewport, Vector2 screenPos, ref Matrix worldViewProj)
        {
            var graphicsDevice = graphicsDeviceService?.GraphicsDevice;
            if (graphicsDevice == null)
                return new Ray(new Vector3(float.NegativeInfinity), new Vector3(0, 1, 0));

            screenPos.X *= graphicsDevice.Presenter.BackBuffer.Width;
            screenPos.Y *= graphicsDevice.Presenter.BackBuffer.Height;

            var unprojectedNear = viewport.Unproject(new Vector3(screenPos, 0.0f), ref worldViewProj);

            var unprojectedFar = viewport.Unproject(new Vector3(screenPos, 1.0f), ref worldViewProj);

            var rayDirection = Vector3.Normalize(unprojectedFar - unprojectedNear);
            var clickRay = new Ray(unprojectedNear, rayDirection);

            return clickRay;
        }

        /// <summary>
        /// Returns if a screen position is within the borders of a tested ui component
        /// </summary>
        /// <param name="uiComponent">The <see cref="UIComponent"/> to be tested</param>
        /// <param name="viewport">The <see cref="Viewport"/> in which the component is being rendered</param>
        /// <param name="worldViewProj"></param>
        /// <param name="screenPosition">The position of the lick on the screen in normalized (0..1, 0..1) range</param>
        /// <param name="uiRay"><see cref="Ray"/> from the click in object space of the ui component in (-Resolution.X/2 .. Resolution.X/2, -Resolution.Y/2 .. Resolution.Y/2) range</param>
        /// <returns></returns>
        private bool GetTouchPosition(Vector3 resolution, ref Viewport viewport, ref Matrix worldViewProj, Vector2 screenPosition, out Ray uiRay)
        {
            uiRay = new Ray(new Vector3(float.NegativeInfinity), new Vector3(0, 1, 0));

            // TODO XK-3367 This only works for a single view

            // Get a touch ray in object (UI component) space
            var touchRay = GetWorldRay(ref viewport, screenPosition, ref worldViewProj);

            // If the click point is outside the canvas ignore any further testing
            var dist = -touchRay.Position.Z / touchRay.Direction.Z;
            if (Math.Abs(touchRay.Position.X + touchRay.Direction.X * dist) > resolution.X * 0.5f ||
                Math.Abs(touchRay.Position.Y + touchRay.Direction.Y * dist) > resolution.Y * 0.5f)
                return false;

            uiRay = touchRay;
            return true;
        }

        private void UpdateTouchEvents(ref Viewport viewport, ref Matrix worldViewProj, RenderUIElement state, GameTime gameTime)
        {
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
                    Ray uiRay;
                    if (!GetTouchPosition(state.Resolution, ref viewport, ref worldViewProj, currentTouchPosition, out uiRay))
                        continue;

                    currentTouchedElement = GetElementAtScreenPosition(rootElement, ref uiRay, ref worldViewProj, ref intersectionPoint);
                }

                if (pointerEvent.EventType == PointerEventType.Pressed || pointerEvent.EventType == PointerEventType.Released)
                    state.LastIntersectionPoint = intersectionPoint;

                // TODO: add the pointer type to the event args?
                var touchEvent = new TouchEventArgs
                {
                    Action = TouchAction.Down,
                    Timestamp = time,
                    ScreenPosition = currentTouchPosition,
                    ScreenTranslation = pointerEvent.DeltaPosition,
                    WorldPosition = intersectionPoint,
                    WorldTranslation = intersectionPoint - state.LastIntersectionPoint
                };

                switch (pointerEvent.EventType)
                {
                    case PointerEventType.Pressed:
                        touchEvent.Action = TouchAction.Down;
                        currentTouchedElement?.RaiseTouchDownEvent(touchEvent);
                        break;

                    case PointerEventType.Released:
                        touchEvent.Action = TouchAction.Up;

                        // generate enter/leave events if we passed from an element to another without move events
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeaveTouchEvents(currentTouchedElement, lastTouchedElement, touchEvent);

                        // trigger the up event
                        currentTouchedElement?.RaiseTouchUpEvent(touchEvent);
                        break;

                    case PointerEventType.Moved:
                        touchEvent.Action = TouchAction.Move;

                        // first notify the move event (even if the touched element changed in between it is still coherent in one of its parents)
                        currentTouchedElement?.RaiseTouchMoveEvent(touchEvent);

                        // then generate enter/leave events if we passed from an element to another
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeaveTouchEvents(currentTouchedElement, lastTouchedElement, touchEvent);
                        break;

                    case PointerEventType.Canceled:
                        touchEvent.Action = TouchAction.Move;

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

        private UIElement UpdateMouseOver(ref Viewport viewport, ref Matrix worldViewProj, RenderUIElement state)
        {
            if (input == null || !input.HasMouse)
                return null;

            var intersectionPoint = Vector3.Zero;
            var mousePosition = input.MousePosition;
            var rootElement = state.Page.RootElement;
            var lastMouseOverElement = state.LastMouseOverElement;

            UIElement mouseOverElement = lastMouseOverElement;


            // determine currently overred element.
            if (mousePosition != state.LastMousePosition
                || (lastMouseOverElement?.RequiresMouseOverUpdate ?? false))
            {
                Ray uiRay;
                if (!GetTouchPosition(state.Resolution, ref viewport, ref worldViewProj, mousePosition, out uiRay))
                    return null;

                mouseOverElement = GetElementAtScreenPosition(rootElement, ref uiRay, ref worldViewProj, ref intersectionPoint);
                

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

            UIElementUnderMouseCursor = mouseOverElement;

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

        /// <summary>
        /// Gets the element with which the clickRay intersects, or null if none is found
        /// </summary>
        /// <param name="rootElement">The root <see cref="UIElement"/> from which it should test</param>
        /// <param name="clickRay"><see cref="Ray"/> from the click in object space of the ui component in (-Resolution.X/2 .. Resolution.X/2, -Resolution.Y/2 .. Resolution.Y/2) range</param>
        /// <param name="worldViewProj"></param>
        /// <param name="intersectionPoint">Intersection point between the ray and the element</param>
        /// <returns>The <see cref="UIElement"/> with which the ray intersects</returns>
        public static UIElement GetElementAtScreenPosition(UIElement rootElement, ref Ray clickRay, ref Matrix worldViewProj, ref Vector3 intersectionPoint)
        {
            UIElement clickedElement = null;
            var smallestDepth = float.PositiveInfinity;
            var highestDepthBias = -1.0f;
            PerformRecursiveHitTest(rootElement, ref clickRay, ref worldViewProj, ref clickedElement, ref intersectionPoint, ref smallestDepth, ref highestDepthBias);

            return clickedElement;
        }

        /// <summary>
        /// Gets all elements that the given <paramref name="ray"/> intersects.
        /// </summary>
        /// <param name="rootElement">The root <see cref="UIElement"/> from which it should test</param>
        /// <param name="ray"><see cref="Ray"/> from the click in object space of the ui component in (-Resolution.X/2 .. Resolution.X/2, -Resolution.Y/2 .. Resolution.Y/2) range</param>
        /// <param name="worldViewProj"></param>
        /// <returns>A collection of all elements hit by this ray, or an empty collection if no hit.</returns>
        public static ICollection<HitTestResult> GetElementsAtPosition(UIElement rootElement, ref Ray ray, ref Matrix worldViewProj)
        {
            var results = new List<HitTestResult>();
            PerformRecursiveHitTest(rootElement, ref ray, ref worldViewProj, results);
            return results;
        }
        
        private static void PerformRecursiveHitTest(UIElement element, ref Ray ray, ref Matrix worldViewProj, ref UIElement hitElement, ref Vector3 intersectionPoint, ref float smallestDepth, ref float highestDepthBias)
        {
            // if the element is not visible, we also remove all its children
            if (!element.IsVisible)
                return;

            var canBeHit = element.CanBeHitByUser;
            if (canBeHit || element.ClipToBounds)
            {
                Vector3 intersection;
                var intersect = element.Intersects(ref ray, out intersection);

                // don't perform the hit test on children if clipped and parent no hit
                if (element.ClipToBounds && !intersect)
                    return;

                // Calculate the depth of the element with the depth bias so that hit test corresponds to visuals.
                Vector4 projectedIntersection;
                var intersection4 = new Vector4(intersection, 1);
                Vector4.Transform(ref intersection4, ref worldViewProj, out projectedIntersection);
                var depth = projectedIntersection.Z/projectedIntersection.W;

                // update the closest element hit
                if (canBeHit && intersect)
                {
                    if (depth < smallestDepth || (depth == smallestDepth && element.DepthBias > highestDepthBias))
                    {
                        smallestDepth = depth;
                        highestDepthBias = element.DepthBias;
                        intersectionPoint = intersection;
                        hitElement = element;
                    }
                }
            }

            // test the children
            foreach (var child in element.HitableChildren)
                PerformRecursiveHitTest(child, ref ray, ref worldViewProj, ref hitElement, ref intersectionPoint, ref smallestDepth, ref highestDepthBias);
        }

        private static void PerformRecursiveHitTest(UIElement element, ref Ray ray, ref Matrix worldViewProj, ICollection<HitTestResult> results)
        {
            // if the element is not visible, we also remove all its children
            if (!element.IsVisible)
                return;

            var canBeHit = element.CanBeHitByUser;
            if (canBeHit || element.ClipToBounds)
            {
                Vector3 intersection;
                var intersect = element.Intersects(ref ray, out intersection);

                // don't perform the hit test on children if clipped and parent no hit
                if (element.ClipToBounds && !intersect)
                    return;

                // Calculate the depth of the element with the depth bias so that hit test corresponds to visuals.
                Vector4 projectedIntersection;
                var intersection4 = new Vector4(intersection, 1);
                Vector4.Transform(ref intersection4, ref worldViewProj, out projectedIntersection);

                // update the hit results
                if (canBeHit && intersect)
                {
                    results.Add(new HitTestResult(element.DepthBias, element, intersection));
                }
            }

            // test the children
            foreach (var child in element.HitableChildren)
                PerformRecursiveHitTest(child, ref ray, ref worldViewProj, results);
        }

        /// <summary>
        /// Represents the result of a hit test on the UI.
        /// </summary>
        public class HitTestResult
        {
            public HitTestResult(float depthBias, UIElement element, Vector3 intersection)
            {
                DepthBias = depthBias;
                Element = element;
                IntersectionPoint = intersection;
            }

            public float DepthBias { get; }

            /// <summary>
            /// Element that was hit.
            /// </summary>
            public UIElement Element { get; }

            /// <summary>
            /// Point of intersection between the ray and the hit element.
            /// </summary>
            public Vector3 IntersectionPoint { get; }
        }
    }
}
