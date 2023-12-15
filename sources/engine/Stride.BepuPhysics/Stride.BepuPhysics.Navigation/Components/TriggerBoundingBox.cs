using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Navigation.Processors;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Navigation.Components;
[DefaultEntityComponentProcessor(typeof(RecastMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class TriggerBoundingBox : TriggerContainerComponent
{
}
