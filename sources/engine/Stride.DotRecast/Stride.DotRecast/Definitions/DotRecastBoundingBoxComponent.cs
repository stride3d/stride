using System.Numerics;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.DotRecast.Definitions;

[DataContract]
[DefaultEntityComponentProcessor(typeof(DotRecastNavigationProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class DotRecastBoundingBoxComponent : EntityComponent
{
    public Vector3 Size { get; set; } = Vector3.One;
}
