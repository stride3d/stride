// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace Stride.Core.Tests.Diagnostics;

public class ConsoleLogListenerTests
{
    internal const string ChildVariable = "STRIDE_TEST_CONSOLE_LOG_CHILD";
    internal const string ChildMessage = "Message of the console log child";
    internal const int ConsoleWindowOpenedExitCode = 2;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Always_WithRedirectedOutputAndNoWindow_OpensNoConsoleWindow(bool redirectToFile)
    {
        if (!OperatingSystem.IsWindows())
            return;

        // A console process of its own, in which the StartupHook of this assembly logs before Main
        var testAssembly = typeof(ConsoleLogListenerTests).Assembly.Location;
        var dotnet = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "dotnet.exe"));
        var logFile = Path.Combine(Path.GetTempPath(), $"stride-console-log-{Guid.NewGuid():N}.txt");
        var startInfo = redirectToFile
            ? new ProcessStartInfo("cmd.exe", $"/c \"\"{dotnet}\" exec \"{testAssembly}\" > \"{logFile}\" 2>&1\"")
            : new ProcessStartInfo(dotnet) { ArgumentList = { "exec", testAssembly }, RedirectStandardOutput = true };
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.Environment[ChildVariable] = "1";
        startInfo.Environment["DOTNET_STARTUP_HOOKS"] = testAssembly;

        try
        {
            using var process = Process.Start(startInfo)!;
            var output = redirectToFile ? null : process.StandardOutput.ReadToEnd();
            Assert.True(process.WaitForExit(TimeSpan.FromMinutes(1)));
            output ??= File.ReadAllText(logFile);

            Assert.True(process.ExitCode == 0, process.ExitCode == ConsoleWindowOpenedExitCode
                ? "The listener opened or showed a console window"
                : $"The child process failed with exit code {process.ExitCode}: {output}");
            Assert.Contains(ChildMessage, output);
        }
        finally
        {
            File.Delete(logFile);
        }
    }
}
