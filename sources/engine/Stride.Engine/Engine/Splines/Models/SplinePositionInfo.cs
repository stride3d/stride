//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    public class SplinePositionInfo
    {
        public SplineNode SplineNodeA { get; set; }
        public SplineNode SplineNodeB { get; set; }
        public Vector3 Position { get; set; }

        public float DistanceToOrigin;

        public float LengthOnCurve;

        public int SplineNodeAIndex { get; internal set; }

        public int SplineNodeBIndex { get; internal set; }

        public int ClosestBezierPointIndex { get; internal set; }

        public BezierPoint ClosestBezierPoint { get; internal set; }
    }
}
