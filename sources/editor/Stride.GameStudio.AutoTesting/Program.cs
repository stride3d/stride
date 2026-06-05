// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Stride.GameStudio.AutoTesting;

/// <summary>
/// Stride.GameStudio.AutoTesting runner entry point. Hosts Stride.GameStudio in-process and
/// wires <see cref="UITestHost"/> into the WPF Application via the appHosted callback.
///
/// CLI:
///   AutoTesting.exe --test-dll &lt;path&gt; --test-name &lt;class&gt; [project.sln] [GS args...]
/// </summary>
internal static class Program
{
    [STAThread]
    public static int Main(string[] osArgs)
    {
        // Bypasses the adapter-needs-an-output filter in GraphicsDeviceManager.FindBestDevices,
        // so headless runners (no DXGI outputs) can still pick a hardware adapter or fall back
        // to WARP. Must be set before any Stride code runs.
        Environment.SetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING", "1");

        // Pre-accept the Stride 4.0 privacy policy: PrivacyPolicyHelper would otherwise pop a
        // modal at startup with no one to click Accept on CI.
        try
        {
            using var subkey = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"SOFTWARE\Stride\Agreements\", writable: true)
                ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Stride\Agreements\");
            subkey?.SetValue("Stride-4.0", "True");
        }
        catch { /* best-effort — failure shows up as the privacy-policy hang */ }

        // Clear the "last startup-session load crashed" sticky flag — a previous AutoTesting run
        // that timed out / was killed leaves it on, which makes OpenInitialSession pop a "try
        // again?" MessageBox with no one to click. Always reset before launching GS.
        try
        {
            Stride.Core.Assets.Editor.Settings.InternalSettings.LoadingStartupSession.SetValue(false);
            Stride.Core.Assets.Editor.Settings.InternalSettings.Save();
        }
        catch { /* best-effort — at worst we re-prompt on the next run */ }

        // Parse our own args. Anything we don't recognise is forwarded to Stride.GameStudio.Run.
        string? testDll = null;
        string? testName = null;
        var gsArgs = new List<string>();
        for (var i = 0; i < osArgs.Length; i++)
        {
            if (osArgs[i] == "--test-dll" && i + 1 < osArgs.Length)
            {
                testDll = osArgs[++i];
            }
            else if (osArgs[i] == "--test-name" && i + 1 < osArgs.Length)
            {
                testName = osArgs[++i];
            }
            else
            {
                gsArgs.Add(osArgs[i]);
            }
        }
        if (testDll is null)
        {
            Console.Error.WriteLine("usage: Stride.GameStudio.AutoTesting --test-dll <path> [--test-name <class>] [project.sln] [GS args...]");
            return 2;
        }
        if (!File.Exists(testDll))
        {
            Console.Error.WriteLine($"Test DLL not found: {testDll}");
            return 2;
        }

        // GS's CrashReport ends with Environment.Exit(0) which masks the underlying error;
        // capture every exception (including the swallowed ones) to a diag log.
        var diagPath = Path.Combine(Path.GetTempPath(), "autotest-diag.log");
        try { File.Delete(diagPath); } catch { }
        void Diag(string msg) { try { File.AppendAllText(diagPath, $"{DateTime.UtcNow:HH:mm:ss.fff} {msg}\n"); } catch { } }
        Diag($"AutoTesting.Main entered. testDll={testDll} testName={testName} gsArgs=[{string.Join(", ", gsArgs)}]");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Diag($"UnhandledException terminating={e.IsTerminating}: {e.ExceptionObject}");
        AppDomain.CurrentDomain.FirstChanceException += (_, e) =>
            Diag($"FirstChance: {e.Exception.GetType().Name}: {e.Exception.Message}");
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Diag("ProcessExit");

        UITestHost? host = null;
        try
        {
            Stride.GameStudio.Program.Run(gsArgs, (app, dispatcher) =>
            {
                Diag("appHosted callback fired; constructing UITestHost");
                host = new UITestHost(dispatcher, testDll, testName);
                host.Start();
                Diag("UITestHost.Start returned");
            });
            Diag("Stride.GameStudio.Program.Run returned");
        }
        catch (Exception ex)
        {
            Diag($"EXCEPTION escaped Program.Run: {ex}");
            throw;
        }

        return host?.ExitCode ?? 0;
    }
}
