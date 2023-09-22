// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stride.Engine.Startup;

public class GameRunner : IDisposable, IAsyncDisposable
{
    public GameRunner(ServiceCollection serviceCollection)
    {
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public ServiceProvider ServiceProvider { get; }

    public void Run()
    {
        using var game = ServiceProvider.GetService<Game>();

        game.Run();
    }

    public void Dispose()
    {
        ServiceProvider?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (ServiceProvider != null)
        {
            await ServiceProvider.DisposeAsync();
        }
    }
}
