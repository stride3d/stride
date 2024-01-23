using System.Numerics;
using BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Contacts;

namespace Stride.BepuPhysics.Definitions;

public struct MaterialProperties
{
    private const int DEFAULT_DISTANCE = 1;

    //__Narrow__Settings__
    public SpringSettings SpringSettings;
    public float FrictionCoefficient;
    public float MaximumRecoveryVelocity;
    public bool IsTrigger;

    public CollisionMask ColliderCollisionMask;
    public FilterByDistance FilterByDistance;

    //__Pose__Settings__ (Warning, if UserPerBodiesAttribute == false, doesn't work)
    public bool IgnoreGlobalGravity;
    public Vector3 PersonalGravity;
#warning PersonalGravity not implemented;


    public static bool AllowContactGeneration(MaterialProperties a, MaterialProperties b)
    {
        bool result = a.ColliderCollisionMask.Collide(b.ColliderCollisionMask);

        if (result && a.FilterByDistance.Id == b.FilterByDistance.Id && a.FilterByDistance.Id != 0)
        {
            var differenceX = a.FilterByDistance.XAxis - b.FilterByDistance.XAxis;
            var differenceY = a.FilterByDistance.YAxis - b.FilterByDistance.YAxis;
            var differenceZ = a.FilterByDistance.ZAxis - b.FilterByDistance.ZAxis;

            if ((!(differenceX < -DEFAULT_DISTANCE || differenceX > DEFAULT_DISTANCE)) && (!(differenceY < -DEFAULT_DISTANCE || differenceY > DEFAULT_DISTANCE)) && (!(differenceZ < -DEFAULT_DISTANCE || differenceY > DEFAULT_DISTANCE)))
                result = false;
        }

        return result;
    }

}