using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    public class ClosestPointInfo
    {
        public Vector3 ClosestPosition;
        public Vector3 APosition;
        public int AIndex;
        public Vector3 BPosition;
        public int BIndex;
        public float Distance;
    }
}
