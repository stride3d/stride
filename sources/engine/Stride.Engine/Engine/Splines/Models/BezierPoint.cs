using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    public class BezierPoint
    {
        public Vector3 Position;
        public float DistanceToPreviousPoint;
        public float TotalLengthOnCurve;
    }
}
