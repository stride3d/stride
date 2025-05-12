using System;
using Microsoft.Extensions.DependencyInjection;
using Stride.Audio;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;
using Stride.Rendering.Fonts;
using Stride.Rendering.Sprites;
using Stride.Shaders.Compiler;
using Stride.Streaming;

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
        gameBuilder.Services.Add(typeof(T), service);
        gameBuilder.DiServices.AddSingleton<T>(service);
        return gameBuilder;
    }

    public static IGameBuilder AddService<T>(this IGameBuilder gameBuilder) where T : class
    {
        gameBuilder.Services.Add(typeof(T), null);
        gameBuilder.DiServices.AddSingleton<T>();
        return gameBuilder;
    }

    public static IGameBuilder AddLogListener(this IGameBuilder gameBuilder, LogListener logListener)
    {
        gameBuilder.LogListeners.Add(logListener);
        return gameBuilder;
    }

    public static IGameBuilder AddStrideInput(this IGameBuilder gameBuilder)
    {
        var services = gameBuilder.Services[typeof(IServiceRegistry)] as IServiceRegistry;

        var inputSystem = new InputSystem(services);

        gameBuilder
            .AddGameSystem(inputSystem)
            .AddService(inputSystem)
            .AddService(inputSystem.Manager);

        return gameBuilder;
    }

    public static IGameBuilder SetGameContext(this IGameBuilder gameBuilder, GameContext context)
    {
        gameBuilder.Game.SetGameContext(context);
        return gameBuilder;
    }

    /// <summary>
    /// Allows the user to add a custom database file provider to the game.
    /// </summary>
    /// <param name="gameBuilder"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IGameBuilder AddDbFileProvider(this IGameBuilder gameBuilder, DatabaseFileProvider provider)
    {
        // Gets initialized by the GameBase constructor.
        gameBuilder.DatabaseFileProvider = provider;
        return gameBuilder;
    }

    /// <summary>
    /// Creates a default database to be used in the game.
    /// </summary>
    /// <param name="gameBuilder"></param>
    /// <returns></returns>
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

    public static IGameBuilder UseDefaultContentManager(this IGameBuilder gameBuilder)
    {
        var services = gameBuilder.Game.Services;
        var content = new ContentManager(services);
        services.AddService<IContentManager>(content);
        services.AddService(content);
        return gameBuilder;
    }

    /// <summary>
    /// Adds a default effect compiler to the game. This is used to compile shaders and effects.
    /// </summary>
    /// <param name="effectCompiler"></param>
    /// <param name="fileProvider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
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

    public static IGameBuilder UseGameContext(this IGameBuilder gameBuilder, GameContext context)
    {
        gameBuilder.Game.SetGameContext(context);
        return gameBuilder;
    }

    public static IGameBuilder AddInput(this IGameBuilder gameBuilder, IInputSource inputSource)
    {
        gameBuilder.InputSources.Add(inputSource);
        return gameBuilder;
    }
}
