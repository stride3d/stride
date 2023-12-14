using Stride.BepuPhysics.Components.Colliders;
using Stride.BepuPhysics.Components.Containers;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Navigation.Components;
public class TriggerBoundingBox : TriggerContainerComponent
{

	public Vector3 Size { get; set; } = Vector3.One;

	public BoxColliderComponent? Box;
}
