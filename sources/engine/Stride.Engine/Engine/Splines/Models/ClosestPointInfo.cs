//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Engine.Splines.Models
{
    public class ClosestPointInfo : SplinePositionInfo
    {
        public float DistanceToOrigin;

        public float LengthOnCurve;

        public int SplineNodeAIndex { get; internal set; }
        public int SplineNodeBIndex { get; internal set; }
    }
}
