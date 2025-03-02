// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class WeldConstraintComponent : TwoBodyConstraintComponent<Weld>
{
    public WeldConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    public Vector3 LocalOffset
    {
        get
        {
            return BepuConstraint.LocalOffset.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffset = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    public Quaternion LocalOrientation
    {
        get
        {
            return BepuConstraint.LocalOrientation.ToStride();
        }
        set
        {
            BepuConstraint.LocalOrientation = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    public float SpringFrequency
    {
        get
        {
            return BepuConstraint.SpringSettings.Frequency;
        }
        set
        {
            BepuConstraint.SpringSettings.Frequency = value;
            TryUpdateDescription();
        }
    }

    public float SpringDampingRatio
    {
        get
        {
            return BepuConstraint.SpringSettings.DampingRatio;
        }
        set
        {
            BepuConstraint.SpringSettings.DampingRatio = value;
            TryUpdateDescription();
        }
    }
}
