using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;
public struct BodyShapeData
{
    public List<Vector3> Points { get; set; } = new List<Vector3>();
    public List<int> Indices { get; set; } = new List<int>();
    //public Matrix Transform { get; set; } = Matrix.Zero;

    public BodyShapeData()
    {
    }
}
