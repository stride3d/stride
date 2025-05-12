using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Games;
using Stride.Input;

namespace Stride.Engine.Builder;

/// <summary>
/// Helps build the game and preps it to be able to run after built.
/// </summary>
/// <typeparam name="T"></typeparam>
public class GameBuilder : IGameBuilder
{
    public Dictionary<Type, object> Services { get; internal set; } = [];

    public IServiceCollection DiServices { get; internal set; } = new ServiceCollection();

    public GameSystemCollection GameSystems { get; internal set; }

    public List<LogListener> LogListeners { get; internal set; } = [];

    public List<IInputSource> InputSources { get; internal set; } = [];

    public DatabaseFileProvider DatabaseFileProvider { get; set; }

    public GameBase Game { get; set; }

    public GameContext Context { get; set; }

    internal GameBuilder()
    {
        Game = new MinimalGame(null);
        GameSystems = Game.GameSystems;
        DiServices.AddSingleton<IServiceRegistry>(Game.Services);
        Services.Add(typeof(IServiceRegistry), Game.Services);
    }

    public static GameBuilder Create()
    {
        return new GameBuilder();
    }

    public virtual GameBase Build()
    {
        var provider = DiServices.BuildServiceProvider();
        foreach (var service in Services)
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
                        foreach (var kvp in Services)
                        {
                            if (kvp.Key.IsAssignableFrom(service.Key) && kvp.Value != null)
                            {
                                instance = provider.GetService(kvp.Key);
                                if(instance is not null)
                                    break;
                            }
                        }
                    }

                    Game.Services.AddService(instance, service.Key);
                    Services[service.Key] = instance;
                }
                else
                {
                    Game.Services.AddService(service.Value, service.Key);
                }
            }
            catch (Exception ex)
            {
                // TODO: check if service is already registered first.
            }
        }

        // Add all game systems to the game.
        foreach (var service in Services)
        {
            var system = provider.GetService(service.Key);
            if (system is IGameSystemBase gameSystem && !Game.GameSystems.Contains(gameSystem))
            {
                Game.GameSystems.Add(gameSystem);
            }
        }

        foreach (var logListener in LogListeners)
        {
            GlobalLogger.GlobalMessageLogged += logListener;
        }

        if (Context != null)
        {
            Game.SetGameContext(Context);
        }

        if(InputSources.Count > 0)
        {
            var inputManager = Game.Services.GetService<InputManager>() ?? throw new InvalidOperationException("InputManager is not registered in the service registry.");
            foreach (var inputSource in InputSources)
            {
                inputManager.Sources.Add(inputSource);
            }
        }

        var dataBase = Game.Services.GetService<IDatabaseFileProviderService>();
        dataBase.FileProvider = DatabaseFileProvider;

        return Game; 
    }
}
