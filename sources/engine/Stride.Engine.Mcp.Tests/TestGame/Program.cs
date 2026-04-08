// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Engine.Mcp;

namespace Stride.Engine.Mcp.TestGame
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            // Explicitly trigger module initialization (ensures assembly is loaded)
            Module.Initialize();

            using var game = new Game();

            // The scene, graphics compositor, and game settings are loaded
            // from compiled assets (Assets/ folder + TestGame.sdpkg)
            game.Run();
        }
    }
}
