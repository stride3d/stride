using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Games;
using System;
using Microsoft.Extensions.DependencyInjection;
using Stride.Input;
using Stride.Core.IO;

namespace Stride.Engine.Builder;
public interface IGameBuilder
{
    Dictionary<Type, object> InternalServices { get; }

    IServiceCollection Services { get; }

    GameSystemCollection GameSystems { get; }

    List<LogListener> LogListeners { get; }

    List<IInputSource> InputSources { get; }

    GameBase Game { get; set; }

    GameContext Context { get; set; }
}
