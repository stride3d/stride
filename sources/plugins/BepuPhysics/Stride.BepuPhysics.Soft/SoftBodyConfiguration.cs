// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Soft;

/// <summary>
/// The settings of the soft bodies of a game, set in the game settings next to the configuration of Bepu
/// and reached the same way: <c>Services.GetOrCreate&lt;SoftBodyConfiguration&gt;()</c>. Changes apply to the bodies already there.
/// </summary>
[DataContract("SoftBodyConfiguration")]
[Display("Bepu Soft Bodies")]
public class SoftBodyConfiguration : Configuration, IService
{
    /// <summary>
    /// Whether the debug view of the physics shows a cross on every particle of the soft bodies.
    /// </summary>
    public bool ShowDebugParticles { get; set; } = true;

    /// <summary>
    /// Whether the debug view of the physics shows a line along every spring of the soft bodies: white ones stretch, cyan ones shear, yellow ones bend.
    /// </summary>
    public bool ShowDebugSprings { get; set; } = true;

    /// <summary>
    /// Half the length of the cross the debug view shows on a particle, in world units.
    /// </summary>
    public float DebugParticleSize { get; set; } = 0.015f;

    /// <summary> The section of the game settings, or the defaults for a game without game settings </summary>
    public static IService NewInstance(IServiceRegistry services)
    {
        return services.GetService<IGameSettingsService>()?.Settings is { } settings ? settings.GetOrCreateConfiguration<SoftBodyConfiguration>() : new SoftBodyConfiguration();
    }
}
