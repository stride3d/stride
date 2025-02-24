// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions;
using Stride.Engine;

// Extension methods are not visible before the user imports the namespace,
// we're purposefully skipping the namespace definition here to ensure that it is picked up by the user/intellisense right away
// ReSharper disable once CheckNamespace
public static class BepuSimulationExtensions
{
    /// <summary>
    /// Returns the physics simulation this entity lives in.
    /// </summary>
    public static BepuSimulation GetSimulation(this Entity entity)
    {
        var services = entity.EntityManager.Services;
        var config = services.GetOrCreate<BepuConfiguration>();
        return SceneBasedSimulationSelector.Shared.Pick(config, entity);
    }
}
