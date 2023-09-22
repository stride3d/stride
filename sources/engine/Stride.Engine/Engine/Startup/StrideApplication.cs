// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Stride.Engine.Startup;

public class StrideApplication
{
    /// <summary>
    /// Creates a GameBuilder with the default functionality added.
    /// </summary>
    public static GameBuilder CreateBuilder()
    {
        var serviceCollection = new ServiceCollection();
        var gameBuilder = new GameBuilder(serviceCollection);

        gameBuilder.ServiceCollection.AddSingleton<Game>();

        return gameBuilder;
    }
}
