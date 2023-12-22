using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;
public struct BodyShapeData
{
#warning Should we use VertexPositionNormalTexture[] instead of multiple List ?
    public List<Vector3> Points { get; set; } = new List<Vector3>();
    public List<Vector3> Normals { get; set; } = new List<Vector3>();
    public List<int> Indices { get; set; } = new List<int>();

    public BodyShapeData()
    {
    }
}
