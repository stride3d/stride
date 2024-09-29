// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.UI;

namespace Stride.UI
{
    public partial class UISystem
    {
        // object to avoid allocation at each element leave event
        private readonly HashSet<UIElement> newlySelectedElementParents = new HashSet<UIElement>();
        private readonly List<PointerEvent> compactedPointerEvents = new List<PointerEvent>();
        
        private readonly HashSet<UIDocument> documents = new HashSet<UIDocument>(); 
        
        /// <summary>
        /// Represents the UI-element that's currently under the mouse cursor.
        /// Only elements with CanBeHitByUser == true are taken into account.
        /// Last processed element_state / ?UIComponent? with a valid element will be used.
        /// </summary>
        public UIElement PointerOveredElement { get; internal set; }

        private partial void Pick(GameTime gameTime)
        {
            if (renderContext == null || sceneSystem == null || sceneSystem.GraphicsCompositor == null)
                return;

            Texture renderTarget = renderContext.GraphicsDevice.Presenter.BackBuffer;
            // TODO: Not sure if this would support VR. If it doesn't, look at ForwardRenderer.DrawCore for how to (potentially). 
            Viewport viewport = renderContext.ViewportState.Viewport0;
            UIElement currentPointerOveredElement = null;
            
            // Prepare content required for Picking and MouseOver events
            PickingPrepare();
            
            foreach (var cameraSlot in sceneSystem.GraphicsCompositor.Cameras)
            {
                foreach (var uiDocument in documents)
                {
                    if (!uiDocument.Enabled)
                        continue;
                    
                    Matrix worldViewProjection = uiDocument.GetWorldViewProjection(cameraSlot.Camera, renderTarget);
                    
                    // Check if the current UI component is being picked based on the current ViewParameters (used to draw this element)
                    using (Profiler.Begin(UIProfilerKeys.TouchEventsUpdate))
                    {
                        UIElement documentPointerOveredElement = null;
                        UpdateDocumentPointerInput(uiDocument, viewport, ref worldViewProjection, gameTime, ref documentPointerOveredElement);
                        
                        // only update result element, when this one has a value
                        if (documentPointerOveredElement != null)
                            currentPointerOveredElement = documentPointerOveredElement;
                    }
                }
            }
            
            PickingClear();
            
            PointerOveredElement = currentPointerOveredElement;
        }

        private void UpdateDocumentPointerInput(UIDocument uiDocument, Viewport viewport, ref Matrix worldViewProj, GameTime gameTime, ref UIElement elementUnderPointer)
        {
            if (uiDocument.Page?.RootElement == null)
                return;

            var inverseZViewProj = worldViewProj;
            inverseZViewProj.Row3 = -inverseZViewProj.Row3;

            elementUnderPointer = UpdatePointerOver(uiDocument, ref viewport, ref inverseZViewProj);
            UpdatePointerEvents(uiDocument, ref viewport, ref inverseZViewProj, gameTime);
        }

        private void PickingPrepare()
        {
            // compact all the pointer events that happened since last frame to avoid performing useless hit tests.
            CompactPointerEvents();
        }
        
        private void PickingClear()
        {
            // clear the list of compacted pointer events of time frame
            ClearPointerEvents();
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
        /// Gets a ray from a position in screen space if it is within the bounds of the resolution.
        /// </summary>
        /// <param name="resolution">The bounds to test within</param>
        /// <param name="viewport">The <see cref="Viewport"/> in which the component is being rendered</param>
        /// <param name="worldViewProj"></param>
        /// <param name="screenPosition">The position of the lick on the screen in normalized (0..1, 0..1) range</param>
        /// <param name="uiRay"><see cref="Ray"/> from the click in object space of the ui component in (-Resolution.X/2 .. Resolution.X/2, -Resolution.Y/2 .. Resolution.Y/2) range</param>
        /// <returns><c>true</c> when the screen point of the ray would be within the bounds of the UI document; otherwise, <c>false</c>.</returns>
        private bool TryGetDocumentRay(Vector3 resolution, ref Viewport viewport, ref Matrix worldViewProj, Vector2 screenPosition, out Ray uiRay)
        {
            uiRay = new Ray(new Vector3(float.NegativeInfinity), new Vector3(0, 1, 0));

            // TODO XK-3367 This only works for a single view

            // Get a ray in object (RenderUIElement) space
            var ray = GetWorldRay(ref viewport, screenPosition, ref worldViewProj);

            // If the screen point is outside the canvas ignore any further testing
            var dist = -ray.Position.Z / ray.Direction.Z;
            if (Math.Abs(ray.Position.X + ray.Direction.X * dist) > resolution.X * 0.5f ||
                Math.Abs(ray.Position.Y + ray.Direction.Y * dist) > resolution.Y * 0.5f)
                return false;

            uiRay = ray;
            return true;
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
            if (GraphicsDevice == null)
                return new Ray(new Vector3(float.NegativeInfinity), new Vector3(0, 1, 0));

            screenPos.X *= GraphicsDevice.Presenter.BackBuffer.Width;
            screenPos.Y *= GraphicsDevice.Presenter.BackBuffer.Height;

            var unprojectedNear = viewport.Unproject(new Vector3(screenPos, 0.0f), ref worldViewProj);
            var unprojectedFar = viewport.Unproject(new Vector3(screenPos, 1.0f), ref worldViewProj);

            var rayDirection = Vector3.Normalize(unprojectedFar - unprojectedNear);
            var uiRay = new Ray(unprojectedNear, rayDirection);

            return uiRay;
        }

        private void UpdatePointerEvents(UIDocument uiDocument, ref Viewport viewport, ref Matrix worldViewProj, GameTime gameTime)
        {
            var rootElement = uiDocument.Page.RootElement;
            var intersectionPoint = Vector3.Zero;
            var lastTouchPosition = new Vector2(float.NegativeInfinity);

            // analyze pointer event input and trigger UI touch events depending on hit Tests
            foreach (var pointerEvent in compactedPointerEvents)
            {
                // performance optimization: skip all the events that started outside of the UI
                var lastTouchedElement = uiDocument.LastTouchedElement;
                if (lastTouchedElement == null && pointerEvent.EventType != PointerEventType.Pressed)
                    continue;

                var time = gameTime.Total;

                var currentTouchPosition = pointerEvent.Position;
                var currentTouchedElement = lastTouchedElement;

                // re-calculate the element under cursor if click position changed.
                if (lastTouchPosition != currentTouchPosition)
                {
                    Ray uiRay;
                    if (!TryGetDocumentRay(uiDocument.Resolution, ref viewport, ref worldViewProj, currentTouchPosition, out uiRay))
                        continue;

                    currentTouchedElement = GetElementAtScreenPosition(rootElement, ref uiRay, ref worldViewProj, ref intersectionPoint);
                }

                if (pointerEvent.EventType == PointerEventType.Pressed || pointerEvent.EventType == PointerEventType.Released)
                    uiDocument.LastIntersectionPoint = intersectionPoint;

                var uiPointerEvent = new PointerEventArgs()
                {
                    Device = pointerEvent.Device,
                    PointerId = pointerEvent.PointerId,
                    Position = pointerEvent.Position,
                    DeltaPosition = pointerEvent.DeltaPosition,
                    DeltaTime = pointerEvent.DeltaTime,
                    EventType = pointerEvent.EventType,
                    IsDown = pointerEvent.IsDown,
                    WorldPosition = intersectionPoint,
                    WorldDeltaPosition =  intersectionPoint - uiDocument.LastIntersectionPoint
                };

                switch (pointerEvent.EventType)
                {
                    case PointerEventType.Pressed:
                        currentTouchedElement?.RaisePointerPressedEvent(uiPointerEvent);
                        break;

                    case PointerEventType.Released:
                        // generate enter/leave events if we passed from an element to another without move events
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeavePointerEvents(currentTouchedElement, lastTouchedElement, uiPointerEvent);

                        // trigger the up event
                        currentTouchedElement?.RaisePointerReleasedEvent(uiPointerEvent);
                        break;

                    case PointerEventType.Moved:
                        // first notify the move event (even if the touched element changed in between it is still coherent in one of its parents)
                        currentTouchedElement?.RaisePointerMoveEvent(uiPointerEvent);

                        // then generate enter/leave events if we passed from an element to another
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeavePointerEvents(currentTouchedElement, lastTouchedElement, uiPointerEvent);
                        break;

                    case PointerEventType.Canceled:
                        // generate enter/leave events if we passed from an element to another without move events
                        if (currentTouchedElement != lastTouchedElement)
                            ThrowEnterAndLeavePointerEvents(currentTouchedElement, lastTouchedElement, uiPointerEvent);

                        // then raise leave event to all the hierarchy of the previously selected element.
                        var element = currentTouchedElement;
                        while (element != null)
                        {
                            if (element.IsPointerDown)
                                element.RaisePointerLeaveEvent(uiPointerEvent);
                            element = element.VisualParent;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                lastTouchPosition = currentTouchPosition;
                uiDocument.LastTouchedElement = currentTouchedElement;
                uiDocument.LastIntersectionPoint = intersectionPoint;
            }
        }

        private UIElement UpdatePointerOver(UIDocument uiDocument, ref Viewport viewport, ref Matrix worldViewProj)
        {
            if (input == null || !input.HasMouse)
                return null;

            var intersectionPoint = Vector3.Zero;
            var mousePosition = input.MousePosition;
            var rootElement = uiDocument.Page.RootElement;
            var lastPointerOverElement = uiDocument.LastPointerOverElement;

            UIElement pointerOveredElement = lastPointerOverElement;
            
            // determine currently overred element.
            if (mousePosition != uiDocument.LastMousePosition || (lastPointerOverElement?.RequiresMouseOverUpdate ?? false))
            {
                Ray uiRay;

                if (TryGetDocumentRay(uiDocument.Resolution, ref viewport, ref worldViewProj, mousePosition, out uiRay))
                    pointerOveredElement = GetElementAtScreenPosition(rootElement, ref uiRay, ref worldViewProj, ref intersectionPoint);
                else
                    pointerOveredElement = null;
            }
            
            
            // Find the common parent between current and last overed elements.
            var commonElement = FindCommonParent(pointerOveredElement, lastPointerOverElement);
            
            // Disable mouse over state to previously overed hierarchy.
            var parent = lastPointerOverElement;
            while (parent != commonElement && parent != null)
            {
                parent.RequiresMouseOverUpdate = false;

                parent.PointerOverState = PointerOverState.None;
                parent = parent.VisualParent;
            }
            
            // Enable pointer over state to currently overed hierarchy.
            if (pointerOveredElement != null)
            {
                // The element itself.
                pointerOveredElement.PointerOverState = PointerOverState.Self;

                // Its hierarchy.
                parent = pointerOveredElement.VisualParent;
                while (parent != null)
                {
                    if (parent.IsHierarchyEnabled)
                        parent.PointerOverState = PointerOverState.Child;

                    parent = parent.VisualParent;
                }
            }
            
            // update cached values
            uiDocument.LastPointerOverElement = pointerOveredElement;
            uiDocument.LastMousePosition = mousePosition;
            return pointerOveredElement;
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

        private void ThrowEnterAndLeavePointerEvents(UIElement currentElement, UIElement previousElement, PointerEventArgs pointerArgs)
        {
            var commonElement = FindCommonParent(currentElement, previousElement);

            // raise leave events to the hierarchy: previousElt -> commonElementParent
            var previousElementParent = previousElement;
            while (previousElementParent != commonElement && previousElementParent != null)
            {
                if (previousElementParent.IsHierarchyEnabled && previousElementParent.IsPointerDown)
                {
                    pointerArgs.Handled = false; // reset 'handled' because it corresponds to another event
                    previousElementParent.RaisePointerLeaveEvent(pointerArgs);
                }
                previousElementParent = previousElementParent.VisualParent;
            }

            // raise enter events to the hierarchy: newElt -> commonElementParent
            var newElementParent = currentElement;
            while (newElementParent != commonElement && newElementParent != null)
            {
                if (newElementParent.IsHierarchyEnabled && !newElementParent.IsPointerDown)
                {
                    pointerArgs.Handled = false; // reset 'handled' because it corresponds to another event
                    newElementParent.RaisePointerEnterEvent(pointerArgs);
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
        /// Adds the specified <see cref="UIDocument"/> to the <see cref="UISystem"/> to receive input events.
        /// </summary>
        /// <param name="document">The document to add.</param>
        /// <returns><c>true</c> if the document is added to the <see cref="UISystem"/>; <c>false</c> if the document is already present.</returns>
        /// <remarks>A <see cref="RenderUIDocument"/> render object needs to be added to the <see cref="RenderSystem"/> for the UI to be rendered.</remarks>
        public bool AddDocument(UIDocument document)
        {
            return documents.Add(document);
        }
        
        /// <summary>
        /// Removes the specified <see cref="UIDocument"/> from the <see cref="UISystem"/>.
        /// </summary>
        /// <param name="document">The document to remove.</param>
        /// <returns>
        /// <c>true</c> if the document was successfully removed; otherwise <c>false</c>.
        /// This method also returns <c>false</c> the document was not present.
        /// </returns>
        /// <remarks>A <see cref="RenderUIDocument"/> render object needs to be removed from the <see cref="RenderSystem"/> for the UI to stop being rendered.</remarks>
        public bool RemoveDocument(UIDocument document)
        {
            return documents.Remove(document);
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
