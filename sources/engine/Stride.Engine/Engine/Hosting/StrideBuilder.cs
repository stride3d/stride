using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stride.Games;

namespace Stride.Engine.Hosting;

public class StrideBuilder : IStrideBuilder
{
    public GameContext Context { get; set; }

    public GameBase Game { get; set; }

    public IDictionary<object, object> Properties { get; set; }

    public IHost Build()
    {
        throw new NotImplementedException();
    }

    public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
    {
        throw new NotImplementedException();
    }

    public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
    {
        throw new NotImplementedException();
    }

    public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
    {
        throw new NotImplementedException();
    }

    public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
    {
        throw new NotImplementedException();
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
    {
        throw new NotImplementedException();
    }

    public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
    {
        throw new NotImplementedException();
    }
}
