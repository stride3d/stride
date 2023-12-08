using BepuPhysics.Collidables;
using System.Numerics;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    public struct HitInformation
    {
        public Vector3 Normal;
        public float T;
        public CollidableReference? Collidable;
    }
}
