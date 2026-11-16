// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#:property OutputType=Library
#:property TargetFramework=net10.0
#:property PublishAot=false

// .NET startup hook for the Windows Sandbox kit (see README.md): logs every first-chance exception of Game Studio to
// $FCHOOK_DIR\fchook-<pid>.log, as a debugger's Output window shows them. It finds the exceptions Game Studio catches
// and never logs. Start-Gs -FirstChanceLog sets DOTNET_STARTUP_HOOKS to it.

using System.Diagnostics;

internal class StartupHook
{
    private static readonly Lock Gate = new();
    [ThreadStatic] private static bool inHandler;

    public static void Initialize()
    {
        // Child processes (the asset compiler, the game) inherit the variable: only Game Studio is logged
        if (!Path.GetFileName(Environment.ProcessPath ?? "").Equals("Stride.GameStudio.exe", StringComparison.OrdinalIgnoreCase))
            return;
        var directory = Environment.GetEnvironmentVariable("FCHOOK_DIR") ?? AppContext.BaseDirectory;
        var path = Path.Combine(directory, $"fchook-{Environment.ProcessId}.log");
        var writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
        writer.WriteLine($"=== {DateTime.Now:HH:mm:ss.fff} pid {Environment.ProcessId} {Environment.CommandLine}");
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
        {
            // Writing can throw too: never log from inside the handler
            if (inHandler) return;
            inHandler = true;
            try
            {
                var ex = e.Exception;
                // Frequent kinds that Game Studio expects get no stack, to keep the log short
                var stack = ex is DirectoryNotFoundException or UnauthorizedAccessException or OperationCanceledException or FormatException
                    ? ""
                    : Environment.NewLine + new StackTrace(1, false);
                lock (Gate)
                    writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {ex.GetType().FullName}: {ex.Message}{stack}");
            }
            catch (IOException) { }
            finally { inHandler = false; }
        };
    }
}
