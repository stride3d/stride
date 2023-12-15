using System.Numerics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    public struct HitInformation
    {
        public Vector3 Normal; //Sweep & ray
        public Vector3 HitLocation; //Sweep
        public float Distance; 
        public ContainerComponent? Container;
        public CollidableReference? collidableRef;
    }
}
