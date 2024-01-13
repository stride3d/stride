using System.Numerics;
using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Definitions
{
    internal struct MaterialProperties
    {
        private const int DEFAULT_DISTANCE = 1;

        //__Narrow__Settings__
        public SpringSettings SpringSettings;
        public float FrictionCoefficient;
        public float MaximumRecoveryVelocity;
        public bool IsTrigger;

        public byte ColliderGroupMask;
        public ushort FilterByDistanceId; //0 => no check by distance
        public ushort FilterByDistanceX;
        public ushort FilterByDistanceY;
        public ushort FilterByDistanceZ;

        //__Pose__Settings__ (Warning, if UserPerBodiesAttribute == false, doesn't work)
        public bool IgnoreGlobalGravity;
        public Vector3 PersonalGravity;
#warning PersonalGravity not implemented;


        public static bool AllowContactGeneration(MaterialProperties a, MaterialProperties b)
        {
            byte colliderGroupAnd = (byte)(a.ColliderGroupMask & b.ColliderGroupMask);
            bool result = colliderGroupAnd == a.ColliderGroupMask || colliderGroupAnd == b.ColliderGroupMask && colliderGroupAnd != 0;

            if (result && a.FilterByDistanceId == b.FilterByDistanceId && a.FilterByDistanceId != 0)
            {
                var differenceX = a.FilterByDistanceX - b.FilterByDistanceX;
                var differenceY = a.FilterByDistanceY - b.FilterByDistanceY;
                var differenceZ = a.FilterByDistanceZ - b.FilterByDistanceZ;

                if ((!(differenceX < -DEFAULT_DISTANCE || differenceX > DEFAULT_DISTANCE)) && (!(differenceY < -DEFAULT_DISTANCE || differenceY > DEFAULT_DISTANCE)) && (!(differenceZ < -DEFAULT_DISTANCE || differenceY > DEFAULT_DISTANCE)))
                    result = false;
            }

            return result;
        }

    }

}
