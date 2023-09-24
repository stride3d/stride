// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Stride.Engine.Startup;

public interface IGameBuilder
{
    public ConfigurationManager Configuration { get; }
    public IServiceCollection Services { get; }
    public ILoggingBuilder Logging { get; }
    public IAppEnvironment Environment { get; }

    IStrideApplication Build();
}

internal class GameBuilder : IGameBuilder
{
    public GameBuilder()
    {
        Configuration = new ConfigurationManager();

        Environment = new AppEnvironment { ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "", EnvironmentName = GetEnvironment(), ContentRootPath = AppContext.BaseDirectory };
        
        var physicalFileProvider = new PhysicalFileProvider(Environment.ContentRootPath);
        Environment.ContentRootFileProvider = physicalFileProvider;
        Configuration.SetFileProvider(physicalFileProvider);

        // As an example of how ASP handles appsettings.Development.json
        // overriding configuration values from appsettings.json
        Configuration
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.EnvironmentName}.json", optional: true);

        Services = new ServiceCollection();


        Services.AddSingleton(Environment);
        Services.AddSingleton(_ => Configuration);

        Services.AddLogging();
        Logging = new LoggingBuilder(Services);
    }
    
    public ConfigurationManager Configuration { get; }
    public IServiceCollection Services { get; }
    public ILoggingBuilder Logging { get; }
    public IAppEnvironment Environment { get; }
    
    private static string GetEnvironment()
    {
#if DEBUG
        return Environments.Development;
#elif STAGING
        return Environments.Staging;
#endif
        return Environments.Production;
    }

    public IStrideApplication Build() =>
        new StrideApplication(
            Configuration,
            Environment,
            Services.BuildServiceProvider()
        );

    ///<remarks>
    /// Is private in the library, so I can't just use that one.
    /// However, this is all it was.
    /// </remarks>
    private sealed class LoggingBuilder : ILoggingBuilder
    {
        public LoggingBuilder(IServiceCollection services)
        {
            Services = services;
        }


        public IServiceCollection Services { get; }
    }
}
