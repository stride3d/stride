// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;

namespace Stride.GameStudio.Mcp;

/// <summary>
/// Hosts an MCP server inside GameStudio using Kestrel with SSE transport.
/// The server exposes editor functionality as MCP tools that LLM agents can call.
/// </summary>
public sealed class McpServerService : IDisposable
{
    private static readonly Logger Log = GlobalLogger.GetLogger("McpServer");

    private readonly SessionViewModel _session;
    private readonly DispatcherBridge _dispatcherBridge;
    private WebApplication? _webApp;
    private CancellationTokenSource? _cts;

    public int Port { get; }
    public bool IsRunning => _webApp != null;

    public McpServerService(SessionViewModel session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));

        var dispatcher = session.ServiceProvider.Get<IDispatcherService>();
        _dispatcherBridge = new DispatcherBridge(dispatcher);

        // Read port from environment variable, default to 5271
        var portStr = Environment.GetEnvironmentVariable("STRIDE_MCP_PORT");
        Port = int.TryParse(portStr, out var port) ? port : 5271;
    }

    public async Task StartAsync()
    {
        var enabled = Environment.GetEnvironmentVariable("STRIDE_MCP_ENABLED");
        if (string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase))
        {
            Log.Info("MCP server disabled via STRIDE_MCP_ENABLED=false");
            return;
        }

        _cts = new CancellationTokenSource();

        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(Port);
            });

            // Suppress ASP.NET Core console logging to avoid polluting GameStudio output
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            // Register our services for DI into tool methods
            builder.Services.AddSingleton(_session);
            builder.Services.AddSingleton(_dispatcherBridge);

            builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new()
                    {
                        Name = "Stride Game Studio",
                        Version = typeof(McpServerService).Assembly.GetName().Version?.ToString() ?? "0.1.0",
                    };
                })
                .WithHttpTransport()
                .WithToolsFromAssembly(typeof(McpServerService).Assembly);

            _webApp = builder.Build();
            _webApp.MapMcp();

            Log.Info($"MCP server starting on http://localhost:{Port}/sse");
            await _webApp.StartAsync(_cts.Token);
            Log.Info($"MCP server started successfully on http://localhost:{Port}/sse");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to start MCP server", ex);
            _webApp = null;
            _cts?.Dispose();
            _cts = null;
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_webApp == null)
            return;

        Log.Info("MCP server stopping...");
        try
        {
            using var stopCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _webApp.StopAsync(stopCts.Token);
            await _webApp.DisposeAsync();
            Log.Info("MCP server stopped");
        }
        catch (Exception ex)
        {
            Log.Error("Error stopping MCP server", ex);
        }
        finally
        {
            _webApp = null;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void Dispose()
    {
        StopAsync().Wait(TimeSpan.FromSeconds(5));
    }
}
