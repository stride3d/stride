// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Engine;

namespace Stride.Engine.Mcp
{
    public class Module
    {
        private static bool initialized;

        [ModuleInitializer]
        public static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;

            Game.GameStarted += (sender, args) =>
            {
                var game = (Game)sender;
                var settings = game.Settings?.Configurations?.Get<McpSettings>() ?? new McpSettings();

                // Allow environment variable override for testing
                var envEnabled = Environment.GetEnvironmentVariable("STRIDE_MCP_GAME_ENABLED");
                if (string.Equals(envEnabled, "true", StringComparison.OrdinalIgnoreCase))
                    settings.Enabled = true;

                var envPort = Environment.GetEnvironmentVariable("STRIDE_MCP_GAME_PORT");
                if (int.TryParse(envPort, out var port))
                    settings.Port = port;

                if (!settings.Enabled)
                    return;

                var system = new GameMcpSystem(game.Services, settings.Port);
                game.GameSystems.Add(system);
            };
        }
    }
}
