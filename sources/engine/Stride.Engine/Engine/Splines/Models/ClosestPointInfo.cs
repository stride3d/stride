using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    public class ClosestPointInfo
    {
        public Vector3 ClosestPosition;
        public SplineNode SplineNodeA;
        public SplineNode SplineNodeB;
        public float Distance;
        public float Percentage;
    }
}
