using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.BepuPhysics.Definitions;
public struct BodyShapeData
{
    public VertexPositionNormalTexture[] Vertex = new VertexPositionNormalTexture[0];
    public int[] Indices = new int[0];

    public BodyShapeData()
    {
    }
}
public struct BodyShapeTransform
{
    public Vector3 LinearOffset = Vector3.Zero;
    public Quaternion RotationOffset = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    public BodyShapeTransform()
    {
    }
}
