// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Data;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics;
[DataContract]
[Display("Bepu Configuration")]
public class BepuConfiguration : Configuration, IService
{
    public List<BepuSimulation> BepuSimulations = new();

    private static readonly Logger _logger = GlobalLogger.GetLogger("BepuService");

    private static BepuConfiguration CreateBlankConfiguration() =>
        new() { BepuSimulations = [new BepuSimulation()] };

    public static IService NewInstance(IServiceRegistry services)
    {
        BepuConfiguration config;
        if (services.GetService<IGameSettingsService>() is { } settings)
        {
            if (settings.Settings.Configurations.OfType<BepuConfiguration>().Any() == false)
            {
                _logger.Warning("Creating a default configuration for Bepu as none were set up in your game's settings.");
                settings.Settings.Configurations.Add(CreateBlankConfiguration());
            }

            config = settings.Settings.GetOrCreateConfiguration<BepuConfiguration>();
        }
        else
            config = CreateBlankConfiguration();

        if (config.BepuSimulations.Count == 0)
        {
            _logger.Warning("No simulations configured for Bepu, please add one in your game's configuration.");
            config.BepuSimulations.Add(new BepuSimulation());
        }

        var systems = services.GetSafeServiceAs<IGameSystemCollection>();
        PhysicsGameSystem? physicsGameSystem = null;
        foreach (var system in systems)
        {
            if (system is PhysicsGameSystem pgs)
                physicsGameSystem = pgs;
        }
        if (physicsGameSystem == null)
            systems.Add(new PhysicsGameSystem(config, services));

        return config;
    }
}
