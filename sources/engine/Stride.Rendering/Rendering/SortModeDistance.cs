// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

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

            for (int i = 0; i < renderNodes.Count; ++i)
            {
                var renderNode = renderNodes[i];

                var renderObject = renderNode.RenderObject;
                var distance = CollisionHelper.DistancePlanePoint(ref plane, ref renderObject.BoundingBox.Center);
                var distanceI = ComputeDistance(distance);
                if (reverseDistance)
                    distanceI = ~distanceI;

                // Compute sort key
                sortKeys[i] = new SortKey { Value = ((ulong)renderNode.RootRenderFeature.SortKey << 56) | ((ulong)(distanceI >> distanceShift) << distancePosition) | ((ulong)(renderObject.StateSortKey >> stateShift) << statePosition), Index = i, StableIndex = renderObject.Index };
            }
        }
    }
}
