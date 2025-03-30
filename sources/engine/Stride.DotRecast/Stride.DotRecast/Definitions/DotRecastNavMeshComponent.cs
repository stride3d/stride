using System.Numerics;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.DotRecast.Definitions;

[DataContract]
[DefaultEntityComponentProcessor(typeof(DotRecastNavMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class DotRecastNavMeshComponent : EntityComponent
{
    public List<DotRecastGeometryProvider> GeometryProviders = [];
}
