namespace Stride.BepuPhysics.Definitions;

public struct BasicMeshBuffers
{
    #warning maybe get rid of this ?
    public VertexPosition3[] Vertices = Array.Empty<VertexPosition3>();
    public int[] Indices = Array.Empty<int>();

    public BasicMeshBuffers()
    {
    }
}