// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
using Stride.Graphics.Font;
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

    /// <summary>
    /// Adds core systems to the game. Does not register the systems into the <see cref="IServiceRegistry"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gameBuilder"></param>
    /// <param name="gameSystem"></param>
    /// <returns></returns>
    public static IGameBuilder AddGameSystem<T>(this IGameBuilder gameBuilder, T gameSystem) where T : IGameSystemBase
    {
        gameBuilder.GameSystems.Add(gameSystem);
        return gameBuilder;
    }

    /// <summary>
    /// Registers a service into the <see cref="IServiceRegistry"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gameBuilder"></param>
    /// <param name="service"></param>
    /// <returns></returns>
    public static IGameBuilder AddService<T>(this IGameBuilder gameBuilder, T service) where T : class
    {
        gameBuilder.InternalServices.Add(typeof(T), service);
        gameBuilder.Services.AddSingleton<T>(service);
        return gameBuilder;
    }

    /// <summary>
    /// Registers a service into the <see cref="IServiceRegistry"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="gameBuilder"></param>
    /// <returns></returns>
    public static IGameBuilder AddService<T>(this IGameBuilder gameBuilder) where T : class
    {
        gameBuilder.InternalServices.Add(typeof(T), null);
        gameBuilder.Services.AddSingleton<T>();
        return gameBuilder;
    }

    /// <summary>
    /// Registers a service and its interface into the <see cref="IServiceRegistry"/>.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TClass"></typeparam>
    /// <param name="gameBuilder"></param>
    /// <returns></returns>
    public static IGameBuilder AddService<TInterface, TClass>(this IGameBuilder gameBuilder) where TClass : class, TInterface where TInterface : class
    {
        // This is a work around to allow DI to work the same way as the ServiceRegistry expects.
        // Without registering both the interface and the class, the DI will not be able to resolve the interface on build.
        gameBuilder.InternalServices.Add(typeof(TInterface), null);
        gameBuilder.InternalServices.Add(typeof(TClass), null);
        gameBuilder.Services.AddSingleton<TInterface, TClass>();
        return gameBuilder;
    }

    /// <summary>
    /// Adds a log listener to the game. This is used thoughout Stride systems for logging events.
    /// </summary>
    /// <param name="gameBuilder"></param>
    /// <param name="logListener"></param>
    /// <returns></returns>
    public static IGameBuilder AddLogListener(this IGameBuilder gameBuilder, LogListener logListener)
    {
        gameBuilder.LogListeners.Add(logListener);
        return gameBuilder;
    }

    /// <summary>
    /// Adds the Stride input system to the game with no sources.
    /// </summary>
    /// <param name="gameBuilder"></param>
    /// <returns></returns>
    public static IGameBuilder UseDefaultInput(this IGameBuilder gameBuilder)
    {
        var services = gameBuilder.InternalServices[typeof(IServiceRegistry)] as IServiceRegistry;

        var inputSystem = new InputSystem(services);

        gameBuilder
            .AddGameSystem(inputSystem)
            .AddService(inputSystem)
            .AddService(inputSystem.Manager);

        return gameBuilder;
    }

    public static IGameBuilder SetGameContext(this IGameBuilder gameBuilder, GameContext context)
    {
        gameBuilder.Context = context;
        return gameBuilder;
    }

    /// <summary>
    /// Add a custom database file provider to the game.
    /// </summary>
    /// <param name="gameBuilder"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static IGameBuilder SetDbFileProvider(this IGameBuilder gameBuilder, DatabaseFileProvider provider)
    {
        // Gets initialized by the GameBase constructor.
        var fileProviderService = gameBuilder.Game.Services.GetService<IDatabaseFileProviderService>();

        fileProviderService.FileProvider = provider;
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

            gameBuilder.SetDbFileProvider(result);
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
    /// <param name="effectSystem"></param>
    /// <param name="fileProvider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static EffectSystem CreateDefaultEffectCompiler(this EffectSystem effectSystem, IVirtualFileProvider fileProvider)
    {
        EffectCompilerBase compiler = new EffectCompiler(fileProvider)
        {
            SourceDirectories = { EffectCompilerBase.DefaultSourceShaderFolder },
        };

        if(fileProvider is DatabaseFileProvider databaseFileProvider)
        {
            effectSystem.Compiler = new EffectCompilerCache(compiler, databaseFileProvider);
            return effectSystem;
        }

        throw new ArgumentException("The file provider must be a DatabaseFileProvider", nameof(fileProvider));
    }

    /// <summary>
    /// Adds an input source to the game. This requires the Stride input system to be used.
    /// </summary>
    /// <param name="gameBuilder"></param>
    /// <param name="inputSource"></param>
    /// <returns></returns>
    public static IGameBuilder AddInputSource(this IGameBuilder gameBuilder, IInputSource inputSource)
    {
        gameBuilder.InputSources.Add(inputSource);
        return gameBuilder;
    }

    /// <summary>
    /// Default <see cref="GameSystem"/>s for a Stride game.
    /// </summary>
    /// <param name="gameBuilder"></param>
    /// <returns></returns>
    public static IGameBuilder UseDefaultGameSystems(this IGameBuilder gameBuilder)
    {
        gameBuilder
            .AddService<ScriptSystem>()
            .AddService<SceneSystem>()
            .AddService<SpriteAnimationSystem>()
            .AddService<DebugTextSystem>()
            .AddService<GameProfilingSystem>()
            .AddService<EffectSystem>()
            .AddService<StreamingManager>()
            .AddService<IAudioEngineProvider, AudioSystem>()
            .AddService<GameFontSystem>()
            .AddService<FontSystem>();

        return gameBuilder;
    }
}
