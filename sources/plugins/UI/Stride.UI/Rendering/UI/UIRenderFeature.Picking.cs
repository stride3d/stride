// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.UI;

namespace Stride.Rendering.UI
{
    public partial class UIRenderFeature 
    {
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
