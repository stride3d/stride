using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuPhysics;
using System.Numerics;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    public struct HitHandler : IRayHitHandler
    {
        public bool StopAtFirstHit { get; set; } = false; //TODO
        public byte CollisionMask { get; set; } = 255; //TODO

        public HitHandler()
        {
            Reset();
        }

        public HitResult Hit = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void Reset()
        {
            if (Hit.HitInformations == null)
                Hit.HitInformations = new();
            Hit.HitInformations.Clear();
            Hit.Hit = false;
        }
        void IRayHitHandler.OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            Hit.HitInformations.Add(new() { Collidable = collidable, Normal = normal, T = t });
            Hit.Hit = true;
        }
    }
}
