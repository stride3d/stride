// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Xunit;

namespace Stride.GameStudio.Mcp.Tests;

/// <summary>
/// xUnit collection fixture that manages the GameStudio process lifecycle.
/// Launches GameStudio with a sample project, waits for the MCP server to become
/// ready, and kills the process after all tests in the collection complete.
///
/// This fixture does NOT build GameStudio — run bootstrap.ps1 first.
/// </summary>
public sealed class GameStudioFixture : IAsyncLifetime
{
    private const string RelativeExePath = @"sources\editor\Stride.GameStudio\bin\Debug\net10.0-windows\Stride.GameStudio.exe";
    private const string RelativeProjectPath = @"samples\Templates\FirstPersonShooter\FirstPersonShooter.sln";
    private const string RepoRootMarker = @"build\Stride.sln";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    private Process? _process;
    private HttpClient? _httpClient;
    private string? _tempProjectDir;
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();

    /// <summary>
    /// The port the MCP server is running on.
    /// </summary>
    public int Port { get; private set; } = 5271;

    /// <summary>
    /// Whether the fixture successfully started GameStudio and the MCP server is ready.
    /// </summary>
    public bool IsReady { get; private set; }

    private static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("STRIDE_MCP_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
            return;

        // Read port
        var portStr = Environment.GetEnvironmentVariable("STRIDE_MCP_PORT");
        if (int.TryParse(portStr, out var port))
            Port = port;

        // Resolve paths
        var exePath = ResolveGameStudioExePath();
        var sourceProjectPath = ResolveTestProjectPath();

        // Validate
        if (!File.Exists(exePath))
        {
            throw new InvalidOperationException(
                $"Stride.GameStudio.exe not found at: {exePath}\n\n" +
                "Run the bootstrap script first to build Game Studio:\n" +
                "  .\\sources\\editor\\Stride.GameStudio.Mcp.Tests\\bootstrap.ps1\n\n" +
                "Or set STRIDE_GAMESTUDIO_EXE to the path of a pre-built executable.");
        }

        if (!File.Exists(sourceProjectPath))
        {
            throw new InvalidOperationException(
                $"Test project not found at: {sourceProjectPath}\n\n" +
                "The FirstPersonShooter sample is expected at:\n" +
                $"  {sourceProjectPath}\n\n" +
                "Or set STRIDE_TEST_PROJECT to the path of a .sln to open.");
        }

        // Copy the project to a temporary directory so tests don't pollute the source tree.
        // GameStudio modifies project files (scene saves, .sln changes, etc.) during normal operation.
        var sourceProjectDir = Path.GetDirectoryName(sourceProjectPath)!;
        _tempProjectDir = Path.Combine(Path.GetTempPath(), "StrideMcpTests_" + Path.GetRandomFileName());
        CopyDirectory(sourceProjectDir, _tempProjectDir);
        var projectPath = Path.Combine(_tempProjectDir, Path.GetFileName(sourceProjectPath));

        // Launch GameStudio
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"\"{projectPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false,
        };

        // Enable MCP and pass the port to the child process
        startInfo.Environment["STRIDE_MCP_ENABLED"] = "true";
        startInfo.Environment["STRIDE_MCP_PORT"] = Port.ToString();

        _process = Process.Start(startInfo);
        if (_process == null)
        {
            throw new InvalidOperationException("Failed to start GameStudio process.");
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
                    $"GameStudio exited prematurely with code {_process.ExitCode}.\n\n" +
                    $"Captured output:\n{output}");
            }

            try
            {
                // A successful connection to the SSE endpoint means the MCP server is up.
                // We don't need to read the SSE stream — just verify it responds.
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
                // Server not ready yet — expected during startup
            }

            await Task.Delay(PollInterval);
        }

        // Timeout — kill process and report
        var timeoutOutput = GetCapturedOutput();
        await KillProcessAsync();
        throw new TimeoutException(
            $"GameStudio MCP server did not become ready within {StartupTimeout.TotalSeconds}s.\n" +
            $"Endpoint: {endpoint}\n\n" +
            $"Captured output:\n{timeoutOutput}");
    }

    public async Task DisposeAsync()
    {
        await KillProcessAsync();
        _httpClient?.Dispose();

        // Clean up the temporary project copy
        if (_tempProjectDir != null && Directory.Exists(_tempProjectDir))
        {
            try
            {
                Directory.Delete(_tempProjectDir, recursive: true);
            }
            catch
            {
                // Best effort — files may still be locked briefly after process exit
            }
        }
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
            // Best effort — process may have already exited
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private static string ResolveGameStudioExePath()
    {
        var envPath = Environment.GetEnvironmentVariable("STRIDE_GAMESTUDIO_EXE");
        if (!string.IsNullOrEmpty(envPath))
            return envPath;

        var repoRoot = FindRepoRoot();
        return Path.Combine(repoRoot, RelativeExePath);
    }

    private static string ResolveTestProjectPath()
    {
        var envPath = Environment.GetEnvironmentVariable("STRIDE_TEST_PROJECT");
        if (!string.IsNullOrEmpty(envPath))
            return envPath;

        var repoRoot = FindRepoRoot();
        return Path.Combine(repoRoot, RelativeProjectPath);
    }

    private static string FindRepoRoot()
    {
        // Walk up from the test assembly directory to find the repo root
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, RepoRootMarker)))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            $"Could not locate repository root (looking for '{RepoRootMarker}').\n" +
            "Set STRIDE_GAMESTUDIO_EXE and STRIDE_TEST_PROJECT environment variables explicitly.");
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

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
        }
    }
}
