using System;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Games;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Engine.Builder;
public static class GameBuilderExtensions
{
    public static IGameBuilder AddGameSystem<T>(this IGameBuilder gameBuilder, T gameSystem) where T : IGameSystemBase
    {
        gameBuilder.GameSystems.Add(gameSystem);
        return gameBuilder;
    }

    public static IGameBuilder AddService<T>(this IGameBuilder gameBuilder, T service) where T : class
    {
        gameBuilder.Services.AddService(service);
        return gameBuilder;
    }

    public static IGameBuilder AddLogListener(this IGameBuilder gameBuilder, LogListener logListener)
    {
        gameBuilder.LogListeners.Add(logListener);
        return gameBuilder;
    }

    public static IGameBuilder AddDbFileProvider(this IGameBuilder gameBuilder, DatabaseFileProvider provider)
    {
        // Gets initialized by the GameBase constructor.
        var dataBase = gameBuilder.Services.GetService<IDatabaseFileProviderService>();
        // There should probably be a change to the interface to avoid the below casting.
        ((DatabaseFileProviderService)dataBase).FileProvider = provider;
        return gameBuilder;
    }

    public static IGameBuilder UseDefaultDb(this IGameBuilder gameBuilder)
    {
        using (Profiler.Begin(GameProfilingKeys.ObjectDatabaseInitialize))
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();

            // Only set a mount path if not mounted already
            var mountPath = VirtualFileSystem.ResolveProviderUnsafe("/asset", true).Provider == null ? "/asset" : null;
            var result = new DatabaseFileProvider(objDatabase, mountPath);

            gameBuilder.AddDbFileProvider(result);
        }

        return gameBuilder;
    }

    public static EffectSystem AddDefaultEffectCompiler(this EffectSystem effectCompiler, IVirtualFileProvider fileProvider)
    {
        EffectCompilerBase compiler = new EffectCompiler(fileProvider)
        {
            SourceDirectories = { EffectCompilerBase.DefaultSourceShaderFolder },
        };

        if(fileProvider is DatabaseFileProvider databaseFileProvider)
        {
            effectCompiler.Compiler = new EffectCompilerCache(compiler, databaseFileProvider);
            return effectCompiler;
        }

        throw new ArgumentException("The file provider must be a DatabaseFileProvider", nameof(fileProvider));
    }

}
