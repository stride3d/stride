// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Assets.Models.Humanoid;

/// <summary>
/// The structure of the standard humanoid skeleton: required bones and the hierarchy of <see cref="HumanoidBone"/>.
/// </summary>
public static class HumanoidBones
{
    /// <summary>
    /// The number of values of <see cref="HumanoidBone"/>.
    /// </summary>
    internal const int Count = (int)HumanoidBone.RightLittleDistal + 1;

    private static readonly HumanoidBone?[] Parents = BuildParents();

    /// <summary>
    /// Gets the bones a skeleton must map to be retargeted.
    /// </summary>
    public static IReadOnlyList<HumanoidBone> Required { get; } =
    [
        HumanoidBone.Hips, HumanoidBone.Spine, HumanoidBone.Head,
        HumanoidBone.LeftUpperArm, HumanoidBone.LeftLowerArm, HumanoidBone.LeftHand,
        HumanoidBone.RightUpperArm, HumanoidBone.RightLowerArm, HumanoidBone.RightHand,
        HumanoidBone.LeftUpperLeg, HumanoidBone.LeftLowerLeg, HumanoidBone.LeftFoot,
        HumanoidBone.RightUpperLeg, HumanoidBone.RightLowerLeg, HumanoidBone.RightFoot,
    ];

    /// <summary>
    /// Gets the required bones that have no node in the given mapping, in the order of <see cref="Required"/>.
    /// </summary>
    /// <param name="mappings">The mapping of a skeleton.</param>
    /// <returns>The required bones with no entry, or whose entry has no node name.</returns>
    public static IReadOnlyList<HumanoidBone> GetMissingRequired(IEnumerable<HumanoidBoneMapping> mappings)
    {
        ArgumentNullException.ThrowIfNull(mappings);
        var mapped = mappings.Where(x => !string.IsNullOrEmpty(x.NodeName)).Select(x => x.Bone).ToHashSet();
        return Required.Where(x => !mapped.Contains(x)).ToList();
    }

    /// <summary>
    /// Gets the parent of a bone in the standard humanoid hierarchy.
    /// </summary>
    /// <param name="bone">The bone.</param>
    /// <returns>The parent bone, or <c>null</c> for <see cref="HumanoidBone.Hips"/>.</returns>
    /// <remarks>The parent may be an optional bone: skip the parents a skeleton does not map.</remarks>
    internal static HumanoidBone? GetParent(HumanoidBone bone) => Parents[(int)bone];

    /// <summary>
    /// Gets whether the bone is on the left side of the character.
    /// </summary>
    /// <param name="bone">The bone.</param>
    /// <returns><c>true</c> if the name of the bone starts with <c>Left</c>.</returns>
    internal static bool IsLeft(HumanoidBone bone) => bone.ToString().StartsWith("Left", StringComparison.Ordinal);

    /// <summary>
    /// Gets whether the bone is on the right side of the character.
    /// </summary>
    /// <param name="bone">The bone.</param>
    /// <returns><c>true</c> if the name of the bone starts with <c>Right</c>.</returns>
    internal static bool IsRight(HumanoidBone bone) => bone.ToString().StartsWith("Right", StringComparison.Ordinal);

    private static HumanoidBone?[] BuildParents()
    {
        var parents = new HumanoidBone?[Count];
        void Chain(params HumanoidBone[] bones)
        {
            for (var i = 1; i < bones.Length; ++i)
                parents[(int)bones[i]] = bones[i - 1];
        }

        Chain(HumanoidBone.Hips, HumanoidBone.Spine, HumanoidBone.Chest, HumanoidBone.UpperChest, HumanoidBone.Neck, HumanoidBone.Head, HumanoidBone.Jaw);
        parents[(int)HumanoidBone.LeftEye] = HumanoidBone.Head;
        parents[(int)HumanoidBone.RightEye] = HumanoidBone.Head;

        foreach (var left in new[] { true, false })
        {
            HumanoidBone Side(string name) => Enum.Parse<HumanoidBone>((left ? "Left" : "Right") + name);

            Chain(HumanoidBone.UpperChest, Side("Shoulder"), Side("UpperArm"), Side("LowerArm"), Side("Hand"));
            Chain(HumanoidBone.Hips, Side("UpperLeg"), Side("LowerLeg"), Side("Foot"), Side("Toes"));
            Chain(Side("Hand"), Side("ThumbMetacarpal"), Side("ThumbProximal"), Side("ThumbDistal"));
            foreach (var finger in new[] { "Index", "Middle", "Ring", "Little" })
                Chain(Side("Hand"), Side(finger + "Proximal"), Side(finger + "Intermediate"), Side(finger + "Distal"));
        }
        return parents;
    }
}
