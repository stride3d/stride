using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Games;

namespace Stride.Hosting;
public interface IStrideGameBuilder
{
    public IServiceRegistry Services { get; }

    GameSystemCollection GameSystems { get; }

    List<LogListener> LogListeners { get; }

    GameBase Game { get; }
}
