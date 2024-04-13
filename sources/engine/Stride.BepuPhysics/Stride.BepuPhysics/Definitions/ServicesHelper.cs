using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics.Definitions;

internal static class ServicesHelper
{
    public static void LoadBepuServices(IServiceRegistry services, out BepuConfiguration configOut, out ShapeCacheSystem shapeCacheOut, out PhysicsGameSystem systemOut)
    {
        var config = services.GetService<BepuConfiguration>();
        if (config == null)
        {
            var settings = services.GetService<IGameSettingsService>();
            if (settings != null)
                config = settings.Settings.Configurations.Get<BepuConfiguration>();
            else
                config = new();

            if (config.BepuSimulations.Count == 0)
            {
                config.BepuSimulations.Add(new BepuSimulation());
            }
            services.AddService(config);
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
