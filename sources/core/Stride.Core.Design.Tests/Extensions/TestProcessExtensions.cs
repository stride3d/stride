// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for <see cref="ProcessExtensions"/> class.
/// </summary>
public class TestProcessExtensions
{
    private static ProcessStartInfo ExitImmediately() => OperatingSystem.IsWindows()
        ? new ProcessStartInfo { FileName = "cmd.exe", Arguments = "/c exit 0", CreateNoWindow = true, UseShellExecute = false }
        : new ProcessStartInfo { FileName = "/bin/sh", Arguments = "-c \"exit 0\"", CreateNoWindow = true, UseShellExecute = false };

    private static ProcessStartInfo ShortLived() => OperatingSystem.IsWindows()
        ? new ProcessStartInfo { FileName = "ping", Arguments = "127.0.0.1 -n 2", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true }
        : new ProcessStartInfo { FileName = "sleep", Arguments = "1", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true };

    private static ProcessStartInfo LongRunning() => OperatingSystem.IsWindows()
        ? new ProcessStartInfo { FileName = "ping", Arguments = "127.0.0.1 -n 100", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true }
        : new ProcessStartInfo { FileName = "sleep", Arguments = "100", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true };

    [Fact]
    public async Task WaitForExitAsync_WithCompletedProcess_CompletesImmediately()
    {
        using var process = new Process { StartInfo = ExitImmediately() };

        process.Start();
        process.WaitForExit(); // Wait synchronously first to ensure it's done

        // Now test async wait on already-exited process
        var exitTask = process.WaitForExitAsync();
        await exitTask; // Should complete without blocking
    }

    [Fact]
    public async Task WaitForExitAsync_WithRunningProcess_WaitsForExit()
    {
        using var process = new Process { StartInfo = ShortLived() };

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
        using var process = new Process { StartInfo = LongRunning() };

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
        using var process = new Process { StartInfo = ExitImmediately() };

        process.Start();

        // Wait with default (no cancellation)
        var exitTask = process.WaitForExitAsync();
        await exitTask;

        Assert.True(process.HasExited);
    }
}
