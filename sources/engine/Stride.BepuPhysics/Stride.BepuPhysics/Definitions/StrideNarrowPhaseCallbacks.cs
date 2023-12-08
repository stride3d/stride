using System;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Collisions;

namespace Stride.BepuPhysics.Definitions
{
    public unsafe struct StrideNarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public struct MaterialProperties
        {
            public SpringSettings SpringSettings;
            public float FrictionCoefficient;
            public float MaximumRecoveryVelocity;
            public byte colliderGroupMask;
        }

        internal CollidableProperty<MaterialProperties> CollidableMaterials { get; set; }
        internal ContactEvents ContactEvents { get; set; }

        public void Initialize(Simulation simulation)
        {
            CollidableMaterials.Initialize(simulation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            var a = CollidableMaterials[pair.A];
            var b = CollidableMaterials[pair.B];
            var com = a.colliderGroupMask & b.colliderGroupMask;
            return com == a.colliderGroupMask || com == b.colliderGroupMask && com != 0;
        }
        //Table of thruth. If the number in the table is present on X/Y (inside '()') collision occur exept if result is "0".
        //! indicate no collision

        //                  1111 1111 (255)     0000 0001 (1  )      0000 0011 (3  )        0000 0101 (5  )     0000 0000 (0)
        //1111 1111 (255)      255                  1                    3                      5                   0!
        //0000 0001 (1  )       1                   1                    1                      1                   0!
        //0000 0011 (3  )       3                   1                    3                      1!                  0!
        //0000 0101 (5  )       5                   1                    1!                     5                   0!
        //0000 1001 (9  )       9                   1                    1!                     1!                  0!
        //0000 1010 (10 )       10                  0!                   2!                     0!                  0!


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            //For the purposes of this demo, we'll use multiplicative blending for the friction and choose spring properties according to which collidable has a higher maximum recovery velocity.
            var a = CollidableMaterials[pair.A];
            var b = CollidableMaterials[pair.B];
            pairMaterial.FrictionCoefficient = a.FrictionCoefficient * b.FrictionCoefficient;
            pairMaterial.MaximumRecoveryVelocity = MathF.Max(a.MaximumRecoveryVelocity, b.MaximumRecoveryVelocity);
            pairMaterial.SpringSettings = pairMaterial.MaximumRecoveryVelocity == a.MaximumRecoveryVelocity ? a.SpringSettings : b.SpringSettings;
            ContactEvents.HandleManifold(workerIndex, pair, ref manifold);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
        }
    }

}
