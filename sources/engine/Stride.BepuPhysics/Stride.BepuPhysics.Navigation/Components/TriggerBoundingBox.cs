using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Navigation.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Navigation.Components;
[DefaultEntityComponentProcessor(typeof(RecastMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Navigation")]
[DataContract("TriggerBoundingBox")]
public class TriggerBoundingBox : TriggerContainerComponent
{
}
