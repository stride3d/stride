// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
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
    private CrashRun run;
    private readonly string dsn;
    private readonly bool sessionScoped;

    private CrashSession(CrashStore store, CrashRun run, string dsn, bool sessionScoped, IReadOnlyList<StoredCrash> groups)
    {
        this.store = store;
        this.run = run;
        this.dsn = dsn;
        this.sessionScoped = sessionScoped;
        Groups = groups;
    }

    /// <summary>The deduped crash groups in this run that are not already suppressed.</summary>
    public IReadOnlyList<StoredCrash> Groups { get; }

    /// <summary>The run directory on disk (for a "reveal in file manager" that also keeps the files).</summary>
    public string RunDirectory => run.Directory;

    /// <summary>Crash sending was turned off at build time (StrideSentryDsn=false); offer no Send.</summary>
    public bool IsDisabled => CrashReportSender.IsDisabled;

    /// <summary>
    /// Loads a run directory. The store layout is <c>&lt;base&gt;/&lt;app&gt;/run-*</c>, so the app id and base
    /// are the run's parent and grandparent — enough to also reach the app's suppression list.
    /// </summary>
    public static CrashSession Load(string runDirectory, string? dsnOverride, bool sessionScoped)
    {
        var full = Path.GetFullPath(runDirectory);
        var appDir = Directory.GetParent(full) ?? throw new ArgumentException($"'{runDirectory}' has no parent app directory.");
        var baseDir = appDir.Parent ?? throw new ArgumentException($"'{runDirectory}' has no store base directory.");

        var store = new CrashStore(appDir.Name, baseDir.FullName);
        var run = CrashStore.OpenRun(full);
        var dsn = CrashReportSender.ResolveDsn(dsnOverride);

        // Defensive: capture already skips suppressed signatures, but never re-surface one that slipped through.
        var groups = run.Read().Where(crash => !store.IsSuppressed(crash.Signature, crash.Version)).ToList();
        return new CrashSession(store, run, dsn, sessionScoped, groups);
    }

    // Cap on an opt-in asset attachment: a definition is small YAML, so a low cap keeps a stray large file out.
    internal const long MaxAssetAttachmentBytes = 1_000_000;

    public Task SendAsync(StoredCrash crash, bool includeDump, bool includeAssetDefinition,
        string feedbackName, string feedbackEmail, string feedbackMessage)
    {
        var dump = includeDump ? run.ReadSendableDump(crash) : null;
        var attachments = includeAssetDefinition ? BuildAssetDefinitionAttachment(crash) : null;
        return CrashReportSender.SendAsync(crash, dump, dsn, attachments, feedbackName, feedbackEmail, feedbackMessage);
    }

    // The failing asset's definition file, read from disk and scrubbed of the user name/path (its YAML can
    // reference a source path under the home folder). Null if missing or over the size cap.
    private static IReadOnlyList<(string Name, byte[] Bytes)> BuildAssetDefinitionAttachment(StoredCrash crash)
    {
        var path = crash.AssetDefinitionPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;
        try
        {
            if (new FileInfo(path).Length > MaxAssetAttachmentBytes)
                return null;
            var scrubbed = CrashReportAnonymizer.Scrub(File.ReadAllText(path));
            return new[] { (Path.GetFileName(path), Encoding.UTF8.GetBytes(scrubbed)) };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Size in bytes of a crash's dump, or 0 if it has none, so the window can show it before sending.</summary>
    public long DumpSize(StoredCrash crash)
    {
        if (string.IsNullOrEmpty(crash.DumpFileName))
            return 0;
        var path = Path.Combine(run.Directory, crash.DumpFileName);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    /// <summary>True when this run belongs to a live host session (a GameStudio-routed build), so a sent crash can
    /// be quietened just for that session; false for a crashed or one-shot host, where only "Don't show again" lasts.</summary>
    public bool IsSessionScoped => sessionScoped;

    /// <summary>On send: quieten this signature for the rest of the current GameStudio session only. A no-op when the
    /// report is not session-scoped (the host crashed or exits now), where sending durably suppresses nothing.</summary>
    public void SuppressForSession(StoredCrash crash)
    {
        if (sessionScoped)
            store.SuppressSession(crash.Signature, crash.Version);
    }

    /// <summary>On "Don't show again": silence this signature in the per-user store until the next version.</summary>
    public void SuppressPersistent(StoredCrash crash) => store.SuppressPersistent(crash.Signature, crash.Version);

    /// <summary>Drop a group's files once it has been sent or dismissed.</summary>
    public void Remove(StoredCrash crash) => run.Remove(crash);

    /// <summary>On "Keep": if the run lives in a transient (session) store, move it to the durable store so it
    /// survives the session and <c>stride crash send</c> can find it. A durable or empty run is left as-is.</summary>
    public void KeepRun()
    {
        if (!sessionScoped || run.IsEmpty)
            return;
        try { run = store.MoveRunToDurable(run); }
        catch { /* best effort: on failure the crash simply stays in the temp store */ }
    }

    /// <summary>Remove the run directory once nothing is left in it.</summary>
    public void CleanupIfEmpty()
    {
        if (run.IsEmpty)
            run.Delete();
    }
}
