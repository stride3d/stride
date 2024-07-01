// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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

    //__Pose__Settings__ conditionally enabled by UsePerBodyAttributes state
    public bool Gravity;


    public static bool AllowContactGeneration(in MaterialProperties a, in MaterialProperties b)
    {
        if (a.ColliderCollisionMask.Collide(b.ColliderCollisionMask) == false)
            return false;
        
        if (a.FilterByDistance.Id == b.FilterByDistance.Id && a.FilterByDistance.Id != 0)
        {
            var differenceX = a.FilterByDistance.XAxis - b.FilterByDistance.XAxis;
            var differenceY = a.FilterByDistance.YAxis - b.FilterByDistance.YAxis;
            var differenceZ = a.FilterByDistance.ZAxis - b.FilterByDistance.ZAxis;

            if ((!(differenceX < -DEFAULT_DISTANCE || differenceX > DEFAULT_DISTANCE)) && (!(differenceY < -DEFAULT_DISTANCE || differenceY > DEFAULT_DISTANCE)) && (!(differenceZ < -DEFAULT_DISTANCE || differenceY > DEFAULT_DISTANCE)))
                return false;
        }

        return true;
    }

}
