using Stride.Core;

namespace Stride.DotRecast.Definitions;

/// <summary>
/// Used to determine geometry/shapes that can be used with a navigation mesh.
/// </summary>
[DataContract(Inherited = true)]
public abstract class DotRecastGeometryProvider
{

    /// <summary>
    /// Tries to get the shape information for the geometry.
    /// </summary>
    /// <returns></returns>
    public abstract NavigationColliderData TryGetShapeInfo();

}
