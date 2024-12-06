// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics.Definitions;

internal static class ServicesHelper
{
    private static readonly Logger _logger = GlobalLogger.GetLogger("BepuService");

    public static void LoadBepuServices(IServiceRegistry services, out BepuConfiguration configOut, out ShapeCacheSystem shapeCacheOut, out PhysicsGameSystem systemOut)
    {
        var config = services.GetService<BepuConfiguration>();
        if (config == null)
        {
            if (services.GetService<IGameSettingsService>() is { } settings)
            {
                config = settings.Settings.Configurations.Get<BepuConfiguration>();
                if (settings.Settings.Configurations.Configurations.Any(x => x.Configuration is BepuConfiguration) == false)
                    _logger.Warning("Creating a default configuration for Bepu as none were set up in your game's settings.");
            }
            else
                config = new BepuConfiguration{ BepuSimulations = [new BepuSimulation()] };

            services.AddService(config);

            if (config.BepuSimulations.Count == 0)
            {
                _logger.Warning("No simulations configured for Bepu, please add one in your game's configuration.");
                config.BepuSimulations.Add(new BepuSimulation());
            }
        }

        configOut = config;

        var shapeCache = services.GetService<ShapeCacheSystem>();
        if (shapeCache == null)
            services.AddService(shapeCache = new(services));

        shapeCacheOut = shapeCache;

        var systems = services.GetSafeServiceAs<IGameSystemCollection>();
        PhysicsGameSystem? physicsGameSystem = null;
        foreach (var system in systems)
        {
            if (system is PhysicsGameSystem pgs)
                physicsGameSystem = pgs;
        }
        if (physicsGameSystem == null)
            systems.Add(physicsGameSystem = new PhysicsGameSystem(services));

        systemOut = physicsGameSystem;
    }
}
