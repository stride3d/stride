using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Core;
using Stride.Games;

namespace Stride.Engine.Builder;
public interface IGameBuilder
{
    IServiceRegistry Services { get; }

    GameSystemCollection GameSystems { get; }

    List<LogListener> LogListeners { get; }

    GameBase Game { get; }
}
