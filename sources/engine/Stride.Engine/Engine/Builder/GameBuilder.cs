using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Games;

namespace Stride.Engine.Builder;

/// <summary>
/// Helps build the game and preps it to be able to run after built.
/// </summary>
/// <typeparam name="T"></typeparam>
public class GameBuilder<T> : IGameBuilder where T : IGame
{
    public IServiceRegistry Services { get; protected set; }

    public GameSystemCollection GameSystems { get; protected set; }

    public List<LogListener> LogListeners { get; protected set; } = [];

    public GameBase Game { get; protected set; }

    internal GameBuilder()
    {
        Game = new MinimalGame();
        Services = Game.Services;
        GameSystems = Game.GameSystems;
    }

    public static GameBuilder<T> Create()
    {
        return new GameBuilder<T>();
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

/// <summary>
/// Creates a default GameBuilder for a <see cref="MinimalGame"/>.
/// </summary>
public class GameBuilder : GameBuilder<MinimalGame>
{

}
