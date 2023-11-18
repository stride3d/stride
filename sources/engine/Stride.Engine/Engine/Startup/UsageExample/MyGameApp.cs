// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stride.Engine.Startup;
using Stride.Rendering.Lights;

namespace MyGame;

// The OS-specific project (what would be in the project MyGame.Windows.csproj)
// would still do minimal work.
// (I have only checked windows, I assume the other OSes are similar)
class MyGameApp
{
    static void Main()
    {
        using (var game = new MyGameSetup().SetUpGame())
        {
            game.Run();
        }
    }
}

// The game specific project (what would be in the project MyGame.csproj)
// would have the setup class that is called by the different OS-specific
// projects. However, at this point, the different parts are not hooked
// up to the engine yet, so it would not do anything significant.
public class MyGameSetup : IGameSetup
{
    public IStrideApplication SetUpGame()
    {
        var gameBuilder = StrideApplication.CreateBuilder();
        return gameBuilder.Build();
    }
}

// Later on, the setup can compose the application parts following
// the patter of how ASP Core does it:
public class MyGameSetupLater : IGameSetup
{
    public IStrideApplication SetUpGame()
    {
        var gameBuilder = StrideApplication.CreateBuilder();

        // Use configuration values
        var meta = gameBuilder.Configuration.GetSection("Meta");
        var version = meta["Version"];

        if (gameBuilder.Environment.IsDevelopment())
        {
            // set up logging
            // The game imports Microsoft.Extensions.Logging.Console,
            // But the engine only needs Microsoft.Extensions.Logging.
            // (This example file is added to the engine so now it also needs console)
            gameBuilder.Logging.AddConsole();
        }

        // Register dependencies
        gameBuilder.Services.AddSingleton<Dependency>();

        // It is customary to use extension methods to group functionality.
        gameBuilder.AddMyComponent();

        // This creates the app that can run the game.
        // This also builds the dependency tree, and no further dependencies
        // can be added after this.
        var app = gameBuilder.Build();

        // Now we can resolve dependencies from app.Services.
        // If we're missing one, we know it's not a race condition.
        var lightProcessor = app.Services.GetRequiredService<LightProcessor>();
        if (app.Environment.IsDevelopment())
        {
            lightProcessor.Enabled = false; // stupid example, but hey
        }

        // Again, these setup steps can be summarized into extension methods 
        app.UseMyComponent();

        return app;
    }
}

// The stuff below here is just to make the examples above compile
// while looking at it in the editor.

public static class MyComponentInstaller
{
    public static void AddMyComponent(this IGameBuilder gameBuilder)
    {
    }


    public static void UseMyComponent(this IStrideApplication strideApplication)
    {
    }
}

public class Dependency
{
}
