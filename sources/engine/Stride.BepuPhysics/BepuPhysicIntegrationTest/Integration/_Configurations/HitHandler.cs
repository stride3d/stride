using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuPhysics;
using System.Numerics;

namespace BepuPhysicIntegrationTest.Integration.Configurations
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
        void IRayHitHandler.OnRayHit(in BepuPhysics.Trees.RayData ray, ref float maximumT, float t, System.Numerics.Vector3 normal, BepuPhysics.Collidables.CollidableReference collidable, int childIndex)
        {
            Hit.HitInformations.Add(new() { Collidable = collidable, Normal = normal, T = t});
            Hit.Hit = true;
        }
    }

    public struct HitInformation
    {
        public Vector3 Normal;
        public float T;
        public CollidableReference? Collidable;
    }

    public struct HitResult
    {
        public List<HitInformation> HitInformations;
        public bool Hit;
    }
}
