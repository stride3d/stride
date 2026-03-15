// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    private const int DefaultPort = 5271;
    private const int MaxPortRetries = 10;

    private readonly SessionViewModel _session;
    private readonly DispatcherBridge _dispatcherBridge;
    private WebApplication? _webApp;
    private CancellationTokenSource? _cts;

    public int Port { get; private set; }
    public bool IsRunning => _webApp != null;

    public McpServerService(SessionViewModel session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));

        var dispatcher = session.ServiceProvider.Get<IDispatcherService>();
        _dispatcherBridge = new DispatcherBridge(dispatcher);
    }

    /// <summary>
    /// Determines whether the MCP server should start based on settings and environment variables.
    /// Priority: env var > per-project .sdpkg.user setting > default (false).
    /// </summary>
    private bool IsEnabled()
    {
        // Env var takes highest priority (for CI, tests, and command-line override)
        var envEnabled = Environment.GetEnvironmentVariable("STRIDE_MCP_ENABLED");
        if (envEnabled != null)
            return !string.Equals(envEnabled, "false", StringComparison.OrdinalIgnoreCase);

        // Fall back to per-project setting (default: false)
        var project = _session.CurrentProject;
        if (project != null)
            return project.UserSettings.GetValue(McpProjectSettings.McpServerEnabled);

        return false;
    }

    /// <summary>
    /// Resolves the port to use. Priority: env var > per-project .sdpkg.user setting > auto-select.
    /// A setting/env value of 0 means auto-select starting from DefaultPort.
    /// </summary>
    private int ResolveConfiguredPort()
    {
        // Env var takes highest priority
        var portStr = Environment.GetEnvironmentVariable("STRIDE_MCP_PORT");
        if (int.TryParse(portStr, out var envPort))
            return envPort;

        // Fall back to per-project setting (default: 0 = auto)
        var project = _session.CurrentProject;
        if (project != null)
            return project.UserSettings.GetValue(McpProjectSettings.McpServerPort);

        return 0;
    }

    public async Task StartAsync()
    {
        if (!IsEnabled())
        {
            Log.Info("MCP server is disabled. Enable it in Tools settings or set STRIDE_MCP_ENABLED=true.");
            return;
        }

        _cts = new CancellationTokenSource();

        var configuredPort = ResolveConfiguredPort();
        if (configuredPort > 0)
        {
            // Fixed port requested — try it once
            await TryStartOnPort(configuredPort);
        }
        else
        {
            // Auto-select: try DefaultPort, then increment
            var started = false;
            for (int i = 0; i < MaxPortRetries; i++)
            {
                var candidatePort = DefaultPort + i;
                if (!IsPortAvailable(candidatePort))
                {
                    Log.Info($"Port {candidatePort} is in use, trying next...");
                    continue;
                }

                try
                {
                    await TryStartOnPort(candidatePort);
                    started = true;
                    break;
                }
                catch (IOException)
                {
                    // Port may have been taken between check and bind — try next
                    Log.Info($"Port {candidatePort} could not be bound, trying next...");
                }
            }

            if (!started)
            {
                Log.Error($"Failed to start MCP server: could not find an available port in range {DefaultPort}-{DefaultPort + MaxPortRetries - 1}");
                _cts?.Dispose();
                _cts = null;
            }
        }
    }

    private async Task TryStartOnPort(int port)
    {
        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(port);
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

            Log.Info($"MCP server starting on http://localhost:{port}/sse");
            await _webApp.StartAsync(_cts!.Token);
            Port = port;
            Log.Info($"MCP server started successfully on http://localhost:{port}/sse");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to start MCP server on port {port}", ex);
            _webApp = null;
            throw;
        }
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
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
