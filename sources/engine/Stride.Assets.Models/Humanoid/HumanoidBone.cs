// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Models.Humanoid;

/// <summary>
/// A standard bone of a humanoid skeleton, following the humanoid bone list of VRM 1.0.
/// </summary>
/// <remarks>
/// Left and right are the character's own sides. <see cref="HumanoidBones.Required"/> lists the bones a skeleton must map to be retargeted.
/// </remarks>
[DataContract("HumanoidBone")]
public enum HumanoidBone
{
    /// <summary>The pelvis, root of the humanoid hierarchy.</summary>
    Hips,
    /// <summary>The first spine bone, above the hips.</summary>
    Spine,
    /// <summary>The chest, above the spine.</summary>
    Chest,
    /// <summary>The upper chest, above the chest.</summary>
    UpperChest,
    /// <summary>The neck.</summary>
    Neck,
    /// <summary>The head.</summary>
    Head,
    /// <summary>The jaw.</summary>
    Jaw,
    /// <summary>The left eye.</summary>
    LeftEye,
    /// <summary>The right eye.</summary>
    RightEye,

    /// <summary>The left shoulder (clavicle).</summary>
    LeftShoulder,
    /// <summary>The left upper arm.</summary>
    LeftUpperArm,
    /// <summary>The left forearm.</summary>
    LeftLowerArm,
    /// <summary>The left hand (wrist).</summary>
    LeftHand,
    /// <summary>The right shoulder (clavicle).</summary>
    RightShoulder,
    /// <summary>The right upper arm.</summary>
    RightUpperArm,
    /// <summary>The right forearm.</summary>
    RightLowerArm,
    /// <summary>The right hand (wrist).</summary>
    RightHand,

    /// <summary>The left thigh.</summary>
    LeftUpperLeg,
    /// <summary>The left shin.</summary>
    LeftLowerLeg,
    /// <summary>The left foot (ankle).</summary>
    LeftFoot,
    /// <summary>The left toes.</summary>
    LeftToes,
    /// <summary>The right thigh.</summary>
    RightUpperLeg,
    /// <summary>The right shin.</summary>
    RightLowerLeg,
    /// <summary>The right foot (ankle).</summary>
    RightFoot,
    /// <summary>The right toes.</summary>
    RightToes,

    /// <summary>The first bone of the left thumb, in the palm.</summary>
    LeftThumbMetacarpal,
    /// <summary>The second bone of the left thumb.</summary>
    LeftThumbProximal,
    /// <summary>The last bone of the left thumb.</summary>
    LeftThumbDistal,
    /// <summary>The first bone of the left index finger.</summary>
    LeftIndexProximal,
    /// <summary>The middle bone of the left index finger.</summary>
    LeftIndexIntermediate,
    /// <summary>The last bone of the left index finger.</summary>
    LeftIndexDistal,
    /// <summary>The first bone of the left middle finger.</summary>
    LeftMiddleProximal,
    /// <summary>The middle bone of the left middle finger.</summary>
    LeftMiddleIntermediate,
    /// <summary>The last bone of the left middle finger.</summary>
    LeftMiddleDistal,
    /// <summary>The first bone of the left ring finger.</summary>
    LeftRingProximal,
    /// <summary>The middle bone of the left ring finger.</summary>
    LeftRingIntermediate,
    /// <summary>The last bone of the left ring finger.</summary>
    LeftRingDistal,
    /// <summary>The first bone of the left little finger.</summary>
    LeftLittleProximal,
    /// <summary>The middle bone of the left little finger.</summary>
    LeftLittleIntermediate,
    /// <summary>The last bone of the left little finger.</summary>
    LeftLittleDistal,

    /// <summary>The first bone of the right thumb, in the palm.</summary>
    RightThumbMetacarpal,
    /// <summary>The second bone of the right thumb.</summary>
    RightThumbProximal,
    /// <summary>The last bone of the right thumb.</summary>
    RightThumbDistal,
    /// <summary>The first bone of the right index finger.</summary>
    RightIndexProximal,
    /// <summary>The middle bone of the right index finger.</summary>
    RightIndexIntermediate,
    /// <summary>The last bone of the right index finger.</summary>
    RightIndexDistal,
    /// <summary>The first bone of the right middle finger.</summary>
    RightMiddleProximal,
    /// <summary>The middle bone of the right middle finger.</summary>
    RightMiddleIntermediate,
    /// <summary>The last bone of the right middle finger.</summary>
    RightMiddleDistal,
    /// <summary>The first bone of the right ring finger.</summary>
    RightRingProximal,
    /// <summary>The middle bone of the right ring finger.</summary>
    RightRingIntermediate,
    /// <summary>The last bone of the right ring finger.</summary>
    RightRingDistal,
    /// <summary>The first bone of the right little finger.</summary>
    RightLittleProximal,
    /// <summary>The middle bone of the right little finger.</summary>
    RightLittleIntermediate,
    /// <summary>The last bone of the right little finger.</summary>
    RightLittleDistal,
}
