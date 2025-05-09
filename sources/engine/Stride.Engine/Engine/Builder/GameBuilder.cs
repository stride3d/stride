using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Games;

namespace Stride.Engine.Builder;

/// <summary>
/// Helps build the game and preps it to be able to run after built.
/// </summary>
/// <typeparam name="T"></typeparam>
public class GameBuilder : IGameBuilder
{
    public IServiceRegistry Services { get; protected set; }

    public GameSystemCollection GameSystems { get; protected set; }

    public List<LogListener> LogListeners { get; protected set; } = [];

    public GameBase Game { get; protected set; }

    internal GameBuilder()
    {
        Game = new MinimalGame(null);
        Services = Game.Services;
        GameSystems = Game.GameSystems;
    }

    public static GameBuilder Create()
    {
        return new GameBuilder();
    }

    public virtual GameBase Build()
    {
        foreach (var logListener in LogListeners)
        {
            GlobalLogger.GlobalMessageLogged += logListener;
        }

        return Game; 
    }
}
