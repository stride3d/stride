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
            Hit.Normal = Vector3.Zero;
            Hit.T = 0;
            Hit.Collidable = null;
            Hit.Hit = false;
        }
        void IRayHitHandler.OnRayHit(in BepuPhysics.Trees.RayData ray, ref float maximumT, float t, System.Numerics.Vector3 normal, BepuPhysics.Collidables.CollidableReference collidable, int childIndex)
        {
            Hit.Normal = normal;
            Hit.T = t;
            Hit.Collidable = collidable;
            Hit.Hit = true;
        }
    }

    public struct HitResult
    {
        public Vector3 Normal;
        public float T;
        public CollidableReference? Collidable;
        public bool Hit;
    }
}
