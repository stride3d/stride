using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics.Collidables;
using BepuPhysics;
using System.Numerics;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using SharpDX.DXGI;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    internal struct SweepHitHandler : ISweepHitHandler
    {
        private readonly BepuSimulation _sim;
        public bool StopAtFirstHit { get; set; } = false;
        public byte CollisionMask { get; set; } = 255;

        public HitResult Hit = new();

        public SweepHitHandler(BepuSimulation sim)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
        {
            Hit.Hit = true;
            Hit.HitInformations.Add(new() { Container = collidable.GetContainerFromCollidable(_sim), collidableRef = collidable, Normal = hitNormal, Distance = t, HitLocation = hitLocation });

            if (StopAtFirstHit)
                maximumT = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            Hit.Hit = true;
            Hit.HitInformations.Add(new() { Container = collidable.GetContainerFromCollidable(_sim), collidableRef = collidable, Normal = Vector3.Zero, Distance = 0, HitLocation = Vector3.Zero });

            if (StopAtFirstHit)
                maximumT = 0;
        }
    }
}
