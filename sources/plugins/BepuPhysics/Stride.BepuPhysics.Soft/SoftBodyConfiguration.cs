// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// What the soft bodies of a game do when they are not told otherwise, set in the game settings next to the configuration of Bepu.
/// </summary>
[DataContract("SoftBodyConfiguration")]
[Display("Bepu Soft Bodies")]
public class SoftBodyConfiguration : Configuration
{
    /// <summary>
    /// Where the models of the soft bodies whose <see cref="SoftBodyComponent.DeformationMode"/> is
    /// <see cref="SoftBodyDeformationMode.Default"/> are deformed.
    /// </summary>
    public SoftBodyDeformationMode DeformationMode { get; set; } = SoftBodyDeformationMode.Gpu;

    /// <summary>
    /// How the models of the volumetric soft bodies whose <see cref="SoftBodyComponent.DeformationQuality"/> is
    /// <see cref="SoftBodyDeformationQuality.Default"/> follow their particles.
    /// </summary>
    public SoftBodyDeformationQuality DeformationQuality { get; set; } = SoftBodyDeformationQuality.Smooth;
}