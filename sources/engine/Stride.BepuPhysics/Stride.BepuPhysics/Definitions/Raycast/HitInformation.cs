using System.Numerics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    public struct HitInformation
    {
        public Vector3 Normal;
        public float T;
        public ContainerComponent? Container;
        public CollidableReference? collidableRef;
    }
}
