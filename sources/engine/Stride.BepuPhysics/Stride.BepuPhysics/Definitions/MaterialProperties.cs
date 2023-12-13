using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Definitions
{
    public struct MaterialProperties
    {
        //Narrow
        public SpringSettings SpringSettings;
        public float FrictionCoefficient;
        public float MaximumRecoveryVelocity;
        public byte ColliderGroupMask;
        public bool Trigger;

        //Pose
        public bool IgnoreGravity;
    }

}
