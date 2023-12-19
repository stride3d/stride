using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;
public struct BodyShapeData
{
	public List<Vector3> Points = new List<Vector3>();
	public List<int> Indices = new List<int>();
	public Matrix Transform = Matrix.Zero;

	public BodyShapeData()
	{
	}
}
