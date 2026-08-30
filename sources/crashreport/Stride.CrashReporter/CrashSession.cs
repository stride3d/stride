// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.CrashReport;

namespace Stride.CrashReporter;

/// <summary>
/// A run to report: the pending crash groups, the store they came from, and the destination to send to.
/// The reporter is a separate process from the tool that crashed, so identity and the dump travel in the
/// files; only the DSN (a public client key, not a secret) comes from this reporter's own build.
/// </summary>
internal sealed class CrashSession
{
    private readonly CrashStore store;
    private readonly CrashRun run;
    private readonly string dsn;

    private CrashSession(CrashStore store, CrashRun run, string dsn, IReadOnlyList<StoredCrash> groups)
    {
        this.store = store;
        this.run = run;
        this.dsn = dsn;
        Groups = groups;
    }

    /// <summary>The deduped crash groups in this run that are not already suppressed.</summary>
    public IReadOnlyList<StoredCrash> Groups { get; }

    /// <summary>The run directory on disk (for a "reveal in file manager" that also keeps the files).</summary>
    public string RunDirectory => run.Directory;

    /// <summary>Crash sending was turned off at build time (StrideSentryDsn=false); offer no Send.</summary>
    public bool IsDisabled => CrashReportSender.IsDisabled;

    /// <summary>
    /// Loads a run directory. The store layout is <c>&lt;base&gt;/&lt;app&gt;/run-*</c>, so the app id and
    /// base are the run's parent and grandparent; that is all the reporter needs to also reach the app's
    /// suppression list. The DSN is an explicit override, else this reporter's baked DSN, else the dev channel.
    /// </summary>
    public static CrashSession Load(string runDirectory, string? dsnOverride)
    {
        var full = Path.GetFullPath(runDirectory);
        var appDir = Directory.GetParent(full) ?? throw new ArgumentException($"'{runDirectory}' has no parent app directory.");
        var baseDir = appDir.Parent ?? throw new ArgumentException($"'{runDirectory}' has no store base directory.");

        var store = new CrashStore(appDir.Name, baseDir.FullName);
        var run = CrashStore.OpenRun(full);
        var dsn = dsnOverride
            ?? (string.IsNullOrEmpty(CrashReportSender.BuildDsn) ? CrashReportSender.DevChannelDsn : CrashReportSender.BuildDsn);

        // Defensive: capture already skips suppressed signatures, but never re-surface one that slipped through.
        var groups = run.Read().Where(crash => !store.IsSuppressed(crash.Signature, crash.Version)).ToList();
        return new CrashSession(store, run, dsn, groups);
    }

    public Task SendAsync(StoredCrash crash, bool includeDump)
        => CrashReportSender.SendAsync(crash, includeDump ? run.ReadDump(crash) : null, dsn);

    /// <summary>Size in bytes of a crash's dump, or 0 if it has none, so the window can show it before sending.</summary>
    public long DumpSize(StoredCrash crash)
    {
        if (string.IsNullOrEmpty(crash.DumpFileName))
            return 0;
        var path = Path.Combine(run.Directory, crash.DumpFileName);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    /// <summary>Silence this signature for the current version, so it never pops up again until an upgrade.</summary>
    public void Suppress(StoredCrash crash) => store.Suppress(crash.Signature, crash.Version);

    /// <summary>Drop a group's files once it has been sent or dismissed.</summary>
    public void Remove(StoredCrash crash) => run.Remove(crash);

    /// <summary>Remove the run directory once nothing is left in it.</summary>
    public void CleanupIfEmpty()
    {
        if (run.IsEmpty)
            run.Delete();
    }
}
