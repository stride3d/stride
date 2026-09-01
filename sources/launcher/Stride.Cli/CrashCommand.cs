// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.CrashReport;

// crash: work with crashes that headless tools (the asset compiler, this CLI) wrote to disk when they
// could not ask for consent in-band. 'list' gives an overview of what is pending, 'show' prints one report's
// full details, and 'send' submits it. All console-only; the GUI reporter owns interactive review.
internal static class CrashCommand
{
    public static Command Create()
    {
        var dir = new Option<string?>("--dir") { Description = "Crash store base directory. Defaults to STRIDE_CRASH_DIR or the per-user location." };
        var dsn = new Option<string?>("--dsn") { Description = "Sentry DSN to send to. Defaults to the build's baked-in DSN, else the public Stride dev channel." };

        // list: enumerate pending runs across every app's store, so the user can copy a run path into 'send'.
        var listCommand = new Command("list", "List crash reports waiting to be sent.");
        listCommand.Options.Add(dir);
        listCommand.SetAction(parseResult =>
        {
            var baseDir = parseResult.GetValue(dir);
            var apps = CrashStore.EnumerateApps(baseDir);
            var total = 0;

            foreach (var app in apps)
            {
                var runs = new CrashStore(app, baseDir).ListRuns();
                if (runs.Count == 0)
                    continue;

                Console.WriteLine(app);
                foreach (var run in runs)
                {
                    var crashes = run.Read();
                    if (crashes.Count == 0)
                        continue;
                    total += crashes.Count;
                    Console.WriteLine($"  {run.Directory}  ({crashes.Count} crash{(crashes.Count == 1 ? "" : "es")})");
                    foreach (var crash in crashes)
                    {
                        var assets = crash.AffectedAssets.Count > 0 ? $"   [{string.Join(", ", crash.AffectedAssets)}]" : "";
                        Console.WriteLine($"      {crash.Title()}  x{crash.Count}{assets}");
                    }
                }
            }

            if (total == 0)
                Console.WriteLine("No crash reports waiting to be sent.");
            else
                Console.WriteLine($"\nSubmit one with: stride crash send <run-directory>");
        });

        // send: submit a saved run directory (or a single crash file) via the shared sender, then remove it.
        var target = new Argument<string>("path") { Description = "A run directory from 'crash list', or a single crash-*.json file." };
        var sendCommand = new Command("send", "Send a saved crash report, then delete it.");
        sendCommand.Arguments.Add(target);
        sendCommand.Options.Add(dsn);
        sendCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            if (CrashReportSender.IsDisabled)
            {
                Console.Error.WriteLine("Crash sending is disabled in this build.");
                return 1;
            }

            var destination = CrashReportSender.ResolveDsn(parseResult.GetValue(dsn));
            var path = parseResult.GetValue(target)!;

            try
            {
                if (Directory.Exists(path))
                    return await SendRun(CrashStore.OpenRun(path), destination);
                if (File.Exists(path))
                    return await SendFile(path, destination);

                Console.Error.WriteLine($"No crash report at '{path}'.");
                return 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Failed to send: {exception.Message}");
                return 1;
            }
        });

        // show: print one saved report in full (metadata + the whole report body incl. the exception/stack),
        // read-only — for inspecting a crash offline or deciding whether to send it, without a GUI.
        var showTarget = new Argument<string>("path") { Description = "A run directory from 'crash list', or a single crash-*.json file." };
        var showCommand = new Command("show", "Print a saved crash report's details, without sending it.");
        showCommand.Arguments.Add(showTarget);
        showCommand.SetAction(int (parseResult) =>
        {
            var path = parseResult.GetValue(showTarget)!;
            try
            {
                if (Directory.Exists(path))
                    return ShowRun(CrashStore.OpenRun(path));
                if (File.Exists(path))
                    return ShowFile(path);

                Console.Error.WriteLine($"No crash report at '{path}'.");
                return 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Failed to read: {exception.Message}");
                return 1;
            }
        });

        // test: deliberately throw, so the whole crash pipeline (store, consent prompt, send) can be
        // exercised end to end without a real bug.
        var testCommand = new Command("test", "Throw a test exception to exercise the crash-report pipeline.") { Hidden = true };
        testCommand.SetAction(int (ParseResult _) => throw new InvalidOperationException("Test crash from 'stride crash test'."));

        var crash = new Command("crash", "List, show and send crash reports saved by Stride's headless tools.");
        crash.Subcommands.Add(listCommand);
        crash.Subcommands.Add(showCommand);
        crash.Subcommands.Add(sendCommand);
        crash.Subcommands.Add(testCommand);
        return crash;
    }

    // A whole run: print every deduped group it holds.
    private static int ShowRun(CrashRun run)
    {
        var crashes = run.Read();
        if (crashes.Count == 0)
        {
            Console.Error.WriteLine($"No crashes in '{run.Directory}'.");
            return 1;
        }
        foreach (var crash in crashes)
            PrintCrash(crash);
        return 0;
    }

    // A single crash file.
    private static int ShowFile(string jsonPath)
    {
        PrintCrash(StoredCrash.FromJson(File.ReadAllText(jsonPath)));
        return 0;
    }

    // The routing metadata (kept out of the report body) followed by the full, already-anonymized report —
    // the exception/stack and every tag — so a crash can be read without opening the reporter or sending it.
    private static void PrintCrash(StoredCrash crash)
    {
        Console.WriteLine($"=== {crash.Title()} ===");
        Console.WriteLine($"signature   {crash.Signature}  (x{crash.Count})");
        Console.WriteLine($"application {crash.Application} {crash.Version} ({crash.Environment})");
        Console.WriteLine($"captured    {crash.TimestampUtc}");
        Console.WriteLine($"dump        {(string.IsNullOrEmpty(crash.DumpFileName) ? "(none)" : crash.DumpFileName)}");
        if (crash.AffectedAssets.Count > 0)
            Console.WriteLine($"assets      {string.Join(", ", crash.AffectedAssets)}");
        Console.WriteLine();

        foreach (var entry in crash.Report)
        {
            if (entry.Key == "Application")
                continue; // already shown above
            if (!string.IsNullOrEmpty(entry.Value) && entry.Value.Contains('\n'))
            {
                Console.WriteLine($"{entry.Key}:");
                foreach (var line in entry.Value.Split('\n'))
                    Console.WriteLine($"  {line.TrimEnd('\r')}");
            }
            else
            {
                Console.WriteLine($"{entry.Key}: {entry.Value}");
            }
        }
        Console.WriteLine();
    }

    // A whole run: send every deduped group, attaching its dump, then delete the run (a decision was made).
    private static async Task<int> SendRun(CrashRun run, string dsn)
    {
        var crashes = run.Read();
        if (crashes.Count == 0)
        {
            Console.Error.WriteLine($"No crashes in '{run.Directory}'.");
            return 1;
        }

        foreach (var crash in crashes)
        {
            await CrashReportSender.SendAsync(crash, run.ReadDump(crash), dsn);
            Console.WriteLine($"Sent: {crash.Title()}");
        }

        run.Delete();
        Console.WriteLine($"Sent {crashes.Count} crash{(crashes.Count == 1 ? "" : "es")} and removed {run.Directory}.");
        return 0;
    }

    // A single crash file: send it (its dump sits beside it), then remove just that file and its dump.
    private static async Task<int> SendFile(string jsonPath, string dsn)
    {
        var crash = StoredCrash.FromJson(File.ReadAllText(jsonPath));
        var run = CrashStore.OpenRun(Path.GetDirectoryName(Path.GetFullPath(jsonPath))!);

        await CrashReportSender.SendAsync(crash, run.ReadDump(crash), dsn);

        File.Delete(jsonPath);
        if (!string.IsNullOrEmpty(crash.DumpFileName))
        {
            var dumpPath = Path.Combine(run.Directory, crash.DumpFileName);
            if (File.Exists(dumpPath))
                File.Delete(dumpPath);
        }
        Console.WriteLine($"Sent: {crash.Title()}");
        return 0;
    }
}
