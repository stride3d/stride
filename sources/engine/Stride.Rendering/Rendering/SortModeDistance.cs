// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Buffers;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Rendering.Materials;

namespace Stride.Rendering
{
    /// <summary>
    /// Helper class to sort objects based on distance.
    /// </summary>
    public abstract class SortModeDistance : SortMode
    {
        private bool reverseDistance;
        protected int distancePosition = 32;
        protected int distancePrecision = 16;

        protected int statePosition = 0;
        protected int statePrecision = 32;

        protected SortModeDistance(bool reverseDistance)
        {
            this.reverseDistance = reverseDistance;
        }

        public static unsafe uint ComputeDistance(float distance)
        {
            // Compute uint sort key (http://aras-p.info/blog/2014/01/16/rough-sorting-by-depth/)
            var distanceI = *((uint*)&distance);
            return ((uint)(-(int)(distanceI >> 31)) | 0x80000000) ^ distanceI;
        }

        public static SortKey CreateSortKey(float distance)
        {
            var distanceI = ComputeDistance(distance);

            return new SortKey { Value = distanceI };
        }

        public override unsafe void GenerateSortKey(RenderView renderView, RenderViewStage renderViewStage, SortKey* sortKeys)
        {
            Matrix.Invert(ref renderView.View, out var viewInverse);
            var plane = new Plane(viewInverse.Forward, Vector3.Dot(viewInverse.TranslationVector, viewInverse.Forward)); // TODO: Point-normal-constructor seems wrong. Check.

            var renderNodes = renderViewStage.RenderNodes;

            int distanceShift = 32 - distancePrecision;
            int stateShift = 32 - statePrecision;

            var distances = ArrayPool<float>.Shared.Rent(renderNodes.Count);
            for (int i = 0; i < renderNodes.Count; ++i)
                distances[i] = CollisionHelper.DistancePlanePoint(ref plane, ref renderNodes[i].RenderObject.BoundingBox.Center);
            if (reverseDistance)
                DrawContainingVolumesLast(renderNodes, distances);

            for (int i = 0; i < renderNodes.Count; ++i)
            {
                var renderNode = renderNodes[i];

                var renderObject = renderNode.RenderObject;
                var distance = distances[i];
                var distanceI = ComputeDistance(distance);
                if (reverseDistance)
                    distanceI = ~distanceI;

                // Compute sort key
                sortKeys[i] = new SortKey { Value = ((ulong)renderNode.RootRenderFeature.SortKey << 56) | ((ulong)(distanceI >> distanceShift) << distancePosition) | ((ulong)(renderObject.StateSortKey >> stateShift) << statePosition), Index = i, StableIndex = renderObject.Index };
            }
            ArrayPool<float>.Shared.Return(distances);
        }

        // A see-through volume whose bounds hold another's draws after it, so the inner one is seen through the outer one
        private static void DrawContainingVolumesLast(ConcurrentCollector<RenderNodeFeatureReference> renderNodes, float[] distances)
        {
            const float Nearer = 1e-3f;
            var volumes = ArrayPool<int>.Shared.Rent(renderNodes.Count);
            var volumeCount = 0;
            for (int i = 0; i < renderNodes.Count; ++i)
            {
                if (renderNodes[i].RenderObject is RenderMesh { MaterialPass: { } materialPass } && materialPass.Parameters.Get(MaterialVolumeKeys.Absorption) > 0)
                    volumes[volumeCount++] = i;
            }

            // Twice, for a volume inside one that is itself inside another
            for (int pass = 0; pass < 2 && volumeCount > 1; pass++)
            {
                for (int a = 0; a < volumeCount; a++)
                {
                    ref var outer = ref renderNodes[volumes[a]].RenderObject.BoundingBox;
                    for (int b = 0; b < volumeCount; b++)
                    {
                        ref var inner = ref renderNodes[volumes[b]].RenderObject.BoundingBox;
                        var offset = inner.Center - outer.Center;
                        // Strictly larger: the passes of one mesh share its bounds
                        if (MathF.Abs(offset.X) + inner.Extent.X <= outer.Extent.X && MathF.Abs(offset.Y) + inner.Extent.Y <= outer.Extent.Y && MathF.Abs(offset.Z) + inner.Extent.Z <= outer.Extent.Z
                            && inner.Extent.X * inner.Extent.Y * inner.Extent.Z < outer.Extent.X * outer.Extent.Y * outer.Extent.Z)
                            distances[volumes[a]] = MathF.Min(distances[volumes[a]], distances[volumes[b]] - Nearer);
                    }
                }
            }
            ArrayPool<int>.Shared.Return(volumes);
        }
    }
}
