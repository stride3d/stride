// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Engine;

namespace Stride.BepuPhysics.Definitions;

/// <summary>
/// This selector picks the simulation based on the scene the entity lives in, see <see cref="BepuSimulation.AssociatedScene"/>.
/// </summary>
/// <remarks>
/// <para> - Pick the first simulation whose scene matches the entity's scene. </para>
/// <para> - If that fails, the first simulation whose scene is null. </para>
/// <para> - If that failed too, The first simulation in the list. </para>
/// </remarks>
[DataContract]
public record SceneBasedSimulationSelector : ISimulationSelector
{
    /// <summary> A read only instance which you can use instead of newing an instance </summary>
    public static readonly SceneBasedSimulationSelector Shared = new();

    public BepuSimulation Pick(BepuConfiguration configuration, Entity target)
    {
        if (configuration.BepuSimulations.Count == 1)
            return configuration.BepuSimulations[0];

        BepuSimulation? sim = null;
        ContentManager? contentManager = null;
        foreach (var simulation in configuration.BepuSimulations)
        {
            if (simulation.AssociatedScene is null)
            {
                sim ??= simulation;
                continue;
            }

            contentManager ??= target.EntityManager.Services.GetService<ContentManager>();
            if (contentManager is not null && contentManager.TryGetLoadedAsset(simulation.AssociatedScene.Url, out object? simScene))
            {
                for (var entityScene = target.Scene; entityScene != null; entityScene = entityScene.Parent)
                {
                    if (entityScene == simScene)
                        return simulation;
                }
            }
        }

        return sim ?? configuration.BepuSimulations[0];
    }
}
