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

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
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
        public CollidableReference Collidable;
        public bool Hit;
    }
}
