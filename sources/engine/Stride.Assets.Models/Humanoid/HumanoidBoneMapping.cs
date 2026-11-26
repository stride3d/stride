// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Models.Humanoid;

/// <summary>
/// Maps a standard humanoid bone to a node of a skeleton.
/// </summary>
[DataContract("HumanoidBoneMapping")]
[DataStyle(DataStyle.Compact)]
public sealed class HumanoidBoneMapping
{
    /// <summary>
    /// Gets or sets the standard humanoid bone.
    /// </summary>
    /// <userdoc>The standard humanoid bone.</userdoc>
    [DataMember(10)]
    public HumanoidBone Bone { get; set; }

    /// <summary>
    /// Gets or sets the name of the skeleton node that plays this bone.
    /// </summary>
    /// <userdoc>The node of the skeleton that plays this bone. The node must be preserved.</userdoc>
    [DataMember(20)]
    public string NodeName { get; set; }

    /// <inheritdoc/>
    public override string ToString() => $"{Bone}: {NodeName}";
}
