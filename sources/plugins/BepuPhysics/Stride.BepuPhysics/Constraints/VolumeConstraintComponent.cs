// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class VolumeConstraintComponent : FourBodyConstraintComponent<VolumeConstraint>, ISpring
{
    public VolumeConstraintComponent() => BepuConstraint = new() { SpringSettings = new SpringSettings(30, 5) };

    /// <summary>
    /// 6 times the target volume of the tetrahedra. Computed from (ab x ac) * ad; this may be negative depending on the winding of the tetrahedron.
    /// </summary>
    /// <userdoc>
    /// 6 times the target volume of the tetrahedra. Computed from (ab x ac) * ad; this may be negative depending on the winding of the tetrahedron.
    /// </userdoc>
    public float TargetScaledVolume
    {
        get { return BepuConstraint.TargetScaledVolume; }
        set
        {
            BepuConstraint.TargetScaledVolume = value;
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
