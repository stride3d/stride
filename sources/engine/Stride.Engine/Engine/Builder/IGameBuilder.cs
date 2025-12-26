// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Games;
using System;
using Microsoft.Extensions.DependencyInjection;
using Stride.Input;

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
