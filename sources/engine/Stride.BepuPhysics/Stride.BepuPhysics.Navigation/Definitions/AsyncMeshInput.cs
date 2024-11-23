using Stride.BepuPhysics.Definitions;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Navigation.Definitions;

internal class AsyncMeshInput
{
    public readonly List<BasicMeshBuffers> ShapeData = [];
    public readonly List<ShapeTransform> TransformsOut = [];
    public readonly List<(Matrix entity, int count)> Matrices = [];
}
