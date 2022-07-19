using Stride.Core.Mathematics;

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
