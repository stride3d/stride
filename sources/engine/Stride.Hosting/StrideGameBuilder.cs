using System;
using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Core;

namespace Stride.Hosting;
public class StrideGameBuilder : IStrideGameBuilder
{
    public IServiceRegistry Services { get; protected set; }

    public GameSystemCollection GameSystems { get; protected set; }

    public List<LogListener> LogListeners { get; protected set; } = [];

    public GameBase Game { get; protected set; }

    internal StrideGameBuilder()
    {
        Game = new BasicGame();
        Services = Game.Services;
        GameSystems = Game.GameSystems;
    }

    public static StrideGameBuilder Create()
    {
        return new StrideGameBuilder();
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
