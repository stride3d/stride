// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Core.Diagnostics;
using Stride.Core.Tests.Diagnostics;

/// <summary>
/// The child process of <see cref="ConsoleLogListenerTests"/>: <c>DOTNET_STARTUP_HOOKS</c> runs it before the test runner's Main.
/// </summary>
internal static partial class StartupHook
{
    public static void Initialize()
    {
        if (Environment.GetEnvironmentVariable(ConsoleLogListenerTests.ChildVariable) != "1")
            return;

        var windowAtStart = GetConsoleWindow();
        var visibleAtStart = windowAtStart != IntPtr.Zero && IsWindowVisible(windowAtStart);

        // Two listeners, as two games of one test run create
        for (var i = 0; i < 2; ++i)
        {
            Action<ILogMessage> log = new ConsoleLogListener { LogMode = ConsoleLogMode.Always };
            log(new LogMessage("Test", LogMessageType.Info, ConsoleLogListenerTests.ChildMessage));
        }

        // A new console window counts even when hidden: on a desktop nobody looks at, new console windows can start hidden
        var window = GetConsoleWindow();
        var openedWindow = window != windowAtStart || (window != IntPtr.Zero && !visibleAtStart && IsWindowVisible(window));
        Environment.Exit(openedWindow ? ConsoleLogListenerTests.ConsoleWindowOpenedExitCode : 0);
    }

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);
}
