using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    public struct HitHandler : IRayHitHandler
    {
        private readonly BepuSimulation _sim;
        public bool StopAtFirstHit { get; set; } = false; 
        public byte CollisionMask { get; set; } = 255;

        public HitHandler(BepuSimulation sim)
        {
            Prepare();
            this._sim = sim;
        }

        public HitResult Hit = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            var result = GetContainerFromCollidable(collidable);
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

        public void Prepare(bool stopAtFirstHit = false, byte collisionMask = 255)
        {
            if (Hit.HitInformations == null)
                Hit.HitInformations = new();
            Hit.HitInformations.Clear();
            Hit.Hit = false;
            StopAtFirstHit = stopAtFirstHit;
            CollisionMask = collisionMask;
        }
        void IRayHitHandler.OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            Hit.Hit = true;
            Hit.HitInformations.Add(new() { Container = GetContainerFromCollidable(collidable), collidableRef = collidable, Normal = normal, T = t });

            if (StopAtFirstHit)
                maximumT = t;
        }

        private ContainerComponent? GetContainerFromCollidable(CollidableReference collidable)
        {
            ContainerComponent? container = null;
            if (collidable.Mobility == CollidableMobility.Static && _sim.StaticsContainers.ContainsKey(collidable.StaticHandle))
            {
                container = _sim.StaticsContainers[collidable.StaticHandle];
            }
            else if (collidable.Mobility != CollidableMobility.Static && _sim.BodiesContainers.ContainsKey(collidable.BodyHandle))
            {
                container = _sim.BodiesContainers[collidable.BodyHandle];
            }
            return container;
        }
    }
}
