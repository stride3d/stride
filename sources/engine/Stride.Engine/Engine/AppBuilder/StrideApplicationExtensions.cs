using System;
using Stride.Core;
using Stride.Games;
using Stride.Graphics.Rendering;

namespace Stride.Engine.AppBuilder;
public static class StrideApplicationExtensions
{

    public static void AddGameSystem(this StrideApplicationBuilder app, GameSystemBase gameSystem)
    {
        app.GameSystems.Add(gameSystem);
    }

    public static T AddGameSystem<T>(this StrideApplicationBuilder app) where T : GameSystemBase, new()
    {
        var instance = (T)Activator.CreateInstance(typeof(T), app.Services);
        app.GameSystems.Add(instance);
        return instance;
    }

    public static T AddSurface<T>(this StrideApplicationBuilder app) where T : IRenderSurface, new()
    {
        var instance = (T)Activator.CreateInstance(typeof(T));
        app.Surfaces.Add(instance);
        return instance;
    }
}
