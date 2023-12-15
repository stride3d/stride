using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    internal struct RayHitHandler : IRayHitHandler
    {
        private readonly BepuSimulation _sim;
        public bool StopAtFirstHit { get; set; } = false; 
        public byte CollisionMask { get; set; } = 255;

        public HitResult Hit = new();

        public RayHitHandler(BepuSimulation sim)
        {
            Prepare();
            this._sim = sim;
        }

        public void Prepare(bool stopAtFirstHit = false, byte collisionMask = 255)
        {
            if (Hit.HitInformations == null)
                Hit.HitInformations = new();
            Hit.HitInformations.Clear();
            Hit.Hit = false;
            StopAtFirstHit = stopAtFirstHit;
            CollisionMask = collisionMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            var result = collidable.GetContainerFromCollidable(_sim);
            if (result == null)
                return true;

            var a = CollisionMask;
            var b = result.ColliderGroupMask;
            var com = a & b;
            return com == a || com == b && com != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        void IRayHitHandler.OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            Hit.Hit = true;
            Hit.HitInformations.Add(new() { Container = collidable.GetContainerFromCollidable(_sim), collidableRef = collidable, Normal = normal, Distance = t, HitLocation = Vector3.Zero });

            if (StopAtFirstHit)
                maximumT = t;
        }
        
    }
}
