// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class GetGameStatusTool
    {
        [McpServerTool(Name = "get_game_status"), Description("Returns the current game status including state, FPS, resolution, active scene, entity count, and uptime.")]
        public static async Task<string> GetGameStatus(
            GameBridge bridge,
            CancellationToken cancellationToken)
        {
            var status = await bridge.RunOnGameThread(game =>
            {
                var sceneSystem = game.SceneSystem;
                var rootScene = sceneSystem?.SceneInstance?.RootScene;

                var entityCount = 0;
                if (rootScene != null)
                {
                    entityCount = CountEntities(rootScene);
                }

                var graphicsDevice = game.GraphicsDevice;
                var presenter = graphicsDevice?.Presenter;

                return new
                {
                    status = game.IsRunning ? "running" : "stopped",
                    fps = game.UpdateTime.FramePerSecond,
                    resolution = presenter?.BackBuffer != null
                        ? new { width = presenter.BackBuffer.Width, height = presenter.BackBuffer.Height }
                        : null,
                    activeSceneUrl = game.Settings?.DefaultSceneUrl ?? "(unknown)",
                    entityCount,
                    totalLoadedScenes = rootScene != null ? 1 + CountChildScenes(rootScene) : 0,
                    uptimeSeconds = game.UpdateTime.Total.TotalSeconds,
                };
            }, cancellationToken);

            return JsonSerializer.Serialize(status, new JsonSerializerOptions { WriteIndented = true });
        }

        private static int CountEntities(Scene scene)
        {
            var count = scene.Entities.Count;
            foreach (var child in scene.Children)
            {
                count += CountEntities(child);
            }
            return count;
        }

        private static int CountChildScenes(Scene scene)
        {
            var count = scene.Children.Count;
            foreach (var child in scene.Children)
            {
                count += CountChildScenes(child);
            }
            return count;
        }
    }
}
