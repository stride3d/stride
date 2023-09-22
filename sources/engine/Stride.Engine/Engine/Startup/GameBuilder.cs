// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Stride.Engine.Startup;

public class GameBuilder
{
    public GameBuilder(ServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;
    }

    public ServiceCollection ServiceCollection { get; }

    public GameRunner Build() => new(ServiceCollection);
}
