using Stride.Core;
using Stride.Engine;

namespace Stride.DotRecast.Definitions;

/// <summary>
/// Used to determine geometry/shapes that can be used with a navigation mesh.
/// </summary>
[DataContract(Inherited = true)]
public abstract class DotRecastGeometryProvider
{
    [DataMemberIgnore]
    public IServiceRegistry Services;

    internal void Initialize(IServiceRegistry registry)
    {
        Services = registry;
    }

    /// <summary>
    /// Tries to get the shape information for the geometry.
    /// </summary>
    /// <returns></returns>
    public abstract bool TryGetTransformedShapeInfo(Entity entity, out DotRecastShapeData shapeData);

}
