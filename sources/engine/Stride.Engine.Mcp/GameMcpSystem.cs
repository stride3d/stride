// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Games;
using Stride.Input;

namespace Stride.Engine.Mcp
{
    public class GameMcpSystem : GameSystemBase
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("GameMcpSystem");

        private readonly int port;
        private readonly ConcurrentQueue<GameThreadRequest> pendingRequests = new();

        private WebApplication webApp;
        private CancellationTokenSource cts;
        private LogRingBuffer logRingBuffer;

        private InputSourceSimulated inputSourceSimulated;
        private KeyboardSimulated keyboardSimulated;
        private MouseSimulated mouseSimulated;
        private GamePadSimulated gamepadSimulated;

        public GameMcpSystem(IServiceRegistry registry, int port) : base(registry)
        {
            this.port = port;
            DrawOrder = int.MaxValue;
            Enabled = true;
            Visible = true;
        }

        internal void EnqueueRequest(GameThreadRequest request)
        {
            pendingRequests.Enqueue(request);
        }

        public override void Initialize()
        {
            base.Initialize();

            var game = (Game)Game;

            // Set up additive input (don't clear existing sources)
            var input = Services.GetSafeServiceAs<InputManager>();
            inputSourceSimulated = new InputSourceSimulated();
            input.Sources.Add(inputSourceSimulated);
            keyboardSimulated = inputSourceSimulated.AddKeyboard();
            mouseSimulated = inputSourceSimulated.AddMouse();
            gamepadSimulated = inputSourceSimulated.AddGamePad();

            // Start log capture
            logRingBuffer = new LogRingBuffer();

            // Create bridge
            var bridge = new GameBridge(game, this, keyboardSimulated, mouseSimulated, gamepadSimulated);

            // Start Kestrel MCP server
            cts = new CancellationTokenSource();
            Task.Run(() => StartMcpServer(bridge), cts.Token);

            Log.Info($"GameMcpSystem initialized, MCP server starting on port {port}");
        }

        private async Task StartMcpServer(GameBridge bridge)
        {
            try
            {
                var builder = WebApplication.CreateSlimBuilder();
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenLocalhost(port);
                });

                // Suppress ASP.NET Core console logging
                builder.Logging.ClearProviders();
                builder.Logging.SetMinimumLevel(LogLevel.Warning);

                // Register DI services for tool methods
                builder.Services.AddSingleton(bridge);
                builder.Services.AddSingleton(logRingBuffer);

                builder.Services
                    .AddMcpServer(options =>
                    {
                        options.ServerInfo = new()
                        {
                            Name = "Stride Game Runtime",
                            Version = typeof(GameMcpSystem).Assembly.GetName().Version?.ToString() ?? "0.1.0",
                        };
                    })
                    .WithHttpTransport()
                    .WithToolsFromAssembly(typeof(GameMcpSystem).Assembly);

                webApp = builder.Build();
                webApp.MapMcp();

                Log.Info($"MCP server starting on http://localhost:{port}/sse");
                await webApp.StartAsync(cts.Token);
                Log.Info($"MCP server started on http://localhost:{port}/sse");

                // Wait until cancellation
                try
                {
                    await Task.Delay(Timeout.Infinite, cts.Token);
                }
                catch (OperationCanceledException) { }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start MCP server", ex);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            while (pendingRequests.TryDequeue(out var request))
            {
                try
                {
                    var result = request.Action((Game)Game);
                    request.Completion.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    request.Completion.TrySetException(ex);
                }
            }
        }

        protected override void Destroy()
        {
            base.Destroy();

            try
            {
                cts?.Cancel();
                if (webApp != null)
                {
                    using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    webApp.StopAsync(stopCts.Token).Wait(TimeSpan.FromSeconds(3));
                    webApp.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error stopping MCP server", ex);
            }
            finally
            {
                cts?.Dispose();
                logRingBuffer?.Dispose();
            }

            Log.Info("GameMcpSystem destroyed");
        }
    }
}
