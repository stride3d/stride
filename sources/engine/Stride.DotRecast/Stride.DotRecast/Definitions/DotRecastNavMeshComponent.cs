using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.DotRecast.Definitions;

[DataContract]
[DefaultEntityComponentProcessor(typeof(DotRecastNavMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class DotRecastNavMeshComponent : EntityComponent
{
    public List<DotRecastGeometryProvider> GeometryProviders = [];

    public DotRecastCollectionMethod CollectionMethod = DotRecastCollectionMethod.Scene;

    [DataMemberIgnore]
    public HashSet<Guid> EntityIds = [];
}

public enum DotRecastCollectionMethod
{
    /// <summary>
    /// Collects all entities in the scene of the entity with the <see cref="DotRecastNavMeshComponent"/>"/>
    /// </summary>
    Scene,

    /// <summary>
    /// Collects all children of the entity with the <see cref="DotRecastNavMeshComponent"/>"/>
    /// </summary>
    Children,

    /// <summary>
    /// Collects all entitys with a valid component in a boundingbox volume
    /// </summary>
    BoundingBox,
}
