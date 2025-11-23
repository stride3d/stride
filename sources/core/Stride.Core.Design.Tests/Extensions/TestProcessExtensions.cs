// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for <see cref="ProcessExtensions"/> class.
/// </summary>
public class TestProcessExtensions
{
    [Fact]
    public async Task WaitForExitAsync_WithCompletedProcess_CompletesImmediately()
    {
        // Create a process that exits immediately
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c exit 0",
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit(); // Wait synchronously first to ensure it's done

        // Now test async wait on already-exited process
        var exitTask = process.WaitForExitAsync();
        await exitTask; // Should complete without blocking
    }

    [Fact]
    public async Task WaitForExitAsync_WithRunningProcess_WaitsForExit()
    {
        // Create a process that takes a short time to exit
        // Using ping with -n 2 sends 2 pings which takes about 1 second
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = "127.0.0.1 -n 2",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        process.Start();

        // Verify the process is actually running
        Assert.False(process.HasExited);

        // Wait asynchronously for exit
        var exitTask = process.WaitForExitAsync();
        await exitTask; // Should complete after process exits

        Assert.True(process.HasExited);
    }

    [Fact]
    public async Task WaitForExitAsync_WithCancellation_CancelsWait()
    {
        // Create a long-running process using ping with high count
        // This is more reliable than timeout command across different environments
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = "127.0.0.1 -n 100",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        process.Start();

        using var cts = new CancellationTokenSource();
        var exitTask = process.WaitForExitAsync(cts.Token);

        // Cancel after a short delay
        cts.CancelAfter(100);

        // Should throw TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await exitTask);

        // Clean up the process
        if (!process.HasExited)
        {
            process.Kill();
            process.WaitForExit();
        }
    }

    [Fact]
    public async Task WaitForExitAsync_WithoutCancellation_CompletesNormally()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c exit 0",
                CreateNoWindow = true,
                UseShellExecute = false
            }
        };

        process.Start();

        // Wait with default (no cancellation)
        var exitTask = process.WaitForExitAsync();
        await exitTask;

        Assert.True(process.HasExited);
    }
}
