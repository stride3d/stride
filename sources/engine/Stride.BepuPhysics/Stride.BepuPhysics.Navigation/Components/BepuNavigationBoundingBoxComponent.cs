using Stride.BepuPhysics.Navigation.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Navigation;

namespace Stride.BepuPhysics.Navigation.Components;
[DefaultEntityComponentProcessor(typeof(RecastMeshProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Bepu - Navigation")]
[DataContract("BepuNavigationBoundingBoxComponent")]
public class BepuNavigationBoundingBoxComponent : NavigationBoundingBoxComponent
{
}
