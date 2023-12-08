using System.Numerics;
using BepuPhysics.Collidables;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    public struct HitInformation
    {
        public Vector3 Normal;
        public float T;
        public CollidableReference? Collidable;
    }
}
