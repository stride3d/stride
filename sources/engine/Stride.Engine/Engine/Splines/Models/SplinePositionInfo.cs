using Stride.Core.Mathematics;

namespace Stride.Engine.Splines
{
    public partial class Spline
    {
        public struct SplinePositionInfo
        {
            public SplineNode CurrentSplineNode { get; set; }
            public SplineNode TargetSplineNode { get; set; }
            public Vector3 Position { get; set; }
            public int CurrentSplineNodeIndex { get; internal set; }
        }
    }
}
