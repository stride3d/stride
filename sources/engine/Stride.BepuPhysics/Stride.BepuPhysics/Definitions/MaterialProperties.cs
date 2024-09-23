// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions.Contacts;

namespace Stride.BepuPhysics.Definitions;

public struct MaterialProperties
{
    //__Narrow__Settings__
    public SpringSettings SpringSettings;
    public float FrictionCoefficient;
    public float MaximumRecoveryVelocity;
    public bool IsTrigger;

    public CollisionLayer Layer;
    public CollisionGroup CollisionGroup;

    //__Pose__Settings__ conditionally enabled by UsePerBodyAttributes state
    public bool Gravity;
}
