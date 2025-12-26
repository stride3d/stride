// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Input;

namespace Stride.Engine.Builder;

/// <summary>
/// Helps build the game and preps it to be able to run after <see cref="Build"/>.
/// </summary>
public class GameBuilder : IGameBuilder
{
    /// <summary>
    /// This is used to allow the same instance to be registered multiple times as different interfaces or types. This was done due to how <see cref="IServiceRegistry"/> works."/>
    /// </summary>
    public Dictionary<Type, object> InternalServices { get; internal set; } = [];

    /// <summary>
    /// This allows for Service to be registered through DI.
    /// </summary>
    public IServiceCollection Services { get; internal set; } = new ServiceCollection();

    /// <summary>
    /// This is a direct reference to the game systems collection of the <see cref="GameBase"/>.
    /// </summary>
    public GameSystemCollection GameSystems { get; internal set; }

    /// <summary>
    /// Adds log listeners to the game on <see cref="Build"/>. This is registered first so it will log build errors if they occur.
    /// </summary>
    public List<LogListener> LogListeners { get; internal set; } = [];

    /// <summary>
    /// Adds input sources to the game on <see cref="Build"/>.
    /// </summary>
    public List<IInputSource> InputSources { get; internal set; } = [];

    public GameBase Game { get; set; }

    public GameContext Context { get; set; }

    private static Logger _log => GlobalLogger.GetLogger(nameof(GameBuilder));

    internal GameBuilder(GameBase game)
    {
        Game = game ?? new MinimalGame(null);
        GameSystems = Game.GameSystems;
        Services.AddSingleton<IServiceRegistry>(Game.Services);
        InternalServices.Add(typeof(IServiceRegistry), Game.Services);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="GameBuilder"/> class.
    /// </summary>
    /// <returns></returns>
    public static GameBuilder Create(GameBase game = null)
    {
        return new GameBuilder(game);
    }

    public virtual GameBase Build()
    {
        foreach (var logListener in LogListeners)
        {
            GlobalLogger.GlobalMessageLogged += logListener;
        }

        var provider = Services.BuildServiceProvider();
        foreach (var service in InternalServices)
        {
            if (service.Key == typeof(IServiceRegistry) || service.Key == typeof(IServiceProvider))
                continue;

            try
            {
                if (service.Value == null)
                {
                    var instance = provider.GetService(service.Key);

                    if(instance == null)
                    {
                        //check if the type is inherited from another instance in the services.
                        foreach (var kvp in InternalServices)
                        {
                            if (kvp.Key.IsAssignableFrom(service.Key) && kvp.Value != null)
                            {
                                instance = provider.GetService(kvp.Key);
                                if(instance is not null)
                                    break;
                            }
                        }
                    }

                    _log.Info($"Registering service {service.Key.Name}.");
                    Game.Services.AddService(instance, service.Key);
                    InternalServices[service.Key] = instance;
                }
                else
                {
                    _log.Info($"Registering service {service.Key.Name}.");
                    Game.Services.AddService(service.Value, service.Key);
                }
            }
            catch (Exception ex)
            {
                // TODO: check if service is already registered first.
                _log.Error($"Failed to register service {service.Key.Name}.\n\n", ex);
            }
        }

        // Add all game systems to the game.
        foreach (var service in InternalServices)
        {
            var system = provider.GetService(service.Key);
            if (system is IGameSystemBase gameSystem && !Game.GameSystems.Contains(gameSystem))
            {
                _log.Info($"Adding game system {gameSystem.GetType().Name} to the game systems collection.");
                Game.GameSystems.Add(gameSystem);
            }
        }

        if (Context != null)
        {
            _log.Info($"Setting game context.");
            Game.SetGameContext(Context);
        }

        if(InputSources.Count > 0)
        {
            var inputManager = Game.Services.GetService<InputManager>();

            if (inputManager is null)
            {
                _log.Info("No InputManager found in the game services, creating default.");
                inputManager = new InputManager();
                Game.Services.AddService(inputManager);
            }

            foreach (var inputSource in InputSources)
            {
                _log.Info($"Adding input source {inputSource.GetType().Name} to the input manager.");
                inputManager.Sources.Add(inputSource);
            }
        }

        return Game;
    }
}
