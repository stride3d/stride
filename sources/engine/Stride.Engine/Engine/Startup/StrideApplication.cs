// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stride.Games;

namespace Stride.Engine.Startup;

public interface IStrideApplication : IDisposable, IAsyncDisposable
{
    ConfigurationManager Configuration { get; }
    IAppEnvironment Environment { get; }
    ServiceProvider Services { get; }
    ILogger Logger { get; }

    void Run();
}

public class StrideApplication : IStrideApplication
{
    public StrideApplication(
        ConfigurationManager configuration,
        IAppEnvironment environment,
        ServiceProvider services
    )
    {
        Configuration = configuration;
        Environment = environment;
        Services = services;
        Logger = 
            Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(Environment.ApplicationName ?? nameof(StrideApplication));

    }


    public ConfigurationManager Configuration { get; }
    public IAppEnvironment Environment { get; }
    public ServiceProvider Services { get; }
    public ILogger Logger { get; }

    public static IGameBuilder CreateBuilder() => new GameBuilder();

    public void Run()
    {
        using (var game = new Game())
        {
            game.Run();
        }
    }


    public void Dispose()
    {
        Services.Dispose();
    }


    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
    }
}
