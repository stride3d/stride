// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Xunit;

namespace Stride.Engine.Mcp.Tests;

/// <summary>
/// xUnit collection fixture that manages a game process lifecycle for MCP integration tests.
/// Launches a test game with MCP enabled, waits for the SSE endpoint to become ready,
/// and kills the process after all tests in the collection complete.
/// </summary>
public sealed class GameFixture : IAsyncLifetime
{
    private const string RepoRootMarker = @"build\Stride.sln";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    private Process? _process;
    private HttpClient? _httpClient;
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();

    /// <summary>
    /// The port the MCP server is running on.
    /// </summary>
    public int Port { get; private set; } = 5272;

    /// <summary>
    /// Whether the fixture successfully started the game and the MCP server is ready.
    /// </summary>
    public bool IsReady { get; private set; }

    private static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("STRIDE_MCP_GAME_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
            return;

        // Read port override
        var portStr = Environment.GetEnvironmentVariable("STRIDE_MCP_GAME_PORT");
        if (int.TryParse(portStr, out var port))
            Port = port;

        // Resolve game executable path
        var exePath = ResolveGameExePath();

        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
        {
            throw new InvalidOperationException(
                $"Game executable not found at: {exePath}\n\n" +
                "Set STRIDE_MCP_GAME_EXE to the path of a built game with Stride.Engine.Mcp enabled.");
        }

        // Launch the game with MCP enabled
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false,
        };
        startInfo.Environment["STRIDE_MCP_GAME_ENABLED"] = "true";
        startInfo.Environment["STRIDE_MCP_GAME_PORT"] = Port.ToString();

        _process = Process.Start(startInfo);
        if (_process == null)
        {
            throw new InvalidOperationException("Failed to start game process.");
        }

        _process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
                lock (_stdout) { _stdout.AppendLine(args.Data); }
        };
        _process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
                lock (_stderr) { _stderr.AppendLine(args.Data); }
        };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Wait for MCP server to become ready
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var endpoint = $"http://localhost:{Port}/sse";
        var deadline = DateTime.UtcNow + StartupTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                var output = GetCapturedOutput();
                throw new InvalidOperationException(
                    $"Game exited prematurely with code {_process.ExitCode}.\n\n" +
                    $"Captured output:\n{output}");
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var response = await _httpClient.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                if (response.IsSuccessStatusCode)
                {
                    IsReady = true;
                    return;
                }
            }
            catch (Exception) when (!_process.HasExited)
            {
                // Server not ready yet
            }

            await Task.Delay(PollInterval);
        }

        var timeoutOutput = GetCapturedOutput();
        await KillProcessAsync();
        throw new TimeoutException(
            $"Game MCP server did not become ready within {StartupTimeout.TotalSeconds}s.\n" +
            $"Endpoint: {endpoint}\n\n" +
            $"Captured output:\n{timeoutOutput}");
    }

    public async Task DisposeAsync()
    {
        await KillProcessAsync();
        _httpClient?.Dispose();
    }

    private async Task KillProcessAsync()
    {
        if (_process == null || _process.HasExited)
        {
            _process?.Dispose();
            _process = null;
            return;
        }

        try
        {
            _process.Kill(entireProcessTree: true);
            using var cts = new CancellationTokenSource(ShutdownTimeout);
            await _process.WaitForExitAsync(cts.Token);
        }
        catch (Exception)
        {
            // Best effort
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private static string? ResolveGameExePath()
    {
        // Explicit override takes priority
        var envPath = Environment.GetEnvironmentVariable("STRIDE_MCP_GAME_EXE");
        if (!string.IsNullOrEmpty(envPath))
            return envPath;

        // Auto-discover: walk up from the test assembly to find the repo root,
        // then resolve the TestGame exe at its known build output path.
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, RepoRootMarker)))
            {
                var candidate = Path.Combine(dir,
                    "sources", "engine", "Stride.Engine.Mcp.Tests", "TestGame",
                    "bin", "Debug", "net10.0", "Direct3D11", "TestGame.exe");
                if (File.Exists(candidate))
                    return candidate;
                break;
            }
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    private string GetCapturedOutput()
    {
        var sb = new StringBuilder();
        lock (_stdout)
        {
            if (_stdout.Length > 0)
            {
                sb.AppendLine("--- stdout ---");
                sb.Append(_stdout);
            }
        }
        lock (_stderr)
        {
            if (_stderr.Length > 0)
            {
                sb.AppendLine("--- stderr ---");
                sb.Append(_stderr);
            }
        }
        return sb.Length > 0 ? sb.ToString() : "(no output captured)";
    }
}
