using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics.Rendering;

namespace Stride.Engine.AppBuilder;
public class StrideApplicationBuilder
{
    public GameSettings Configuration => new();

    public ILogger Logging { get; set;  }

    public IServiceRegistry Services { get; set; } = new ServiceRegistry();

    public List<GameSystemBase> GameSystems { get; set; } = [];

    public List<IRenderSurface> Surfaces { get; set; } = [];
}
