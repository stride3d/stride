// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Build version generators, compiled at runtime by RoslynCodeTaskFactory (no prebuilt task assembly).
// Referenced from both Stride.WorktreeVersion.targets and Stride.GitVersion.targets via <Code Source>, so the
// shared version-resolution logic lives once here:
//
//   build version = max(MinVersion, latest releases/<MajorMinor>.* + 1, [local override])   (StrideVersionUtil)
//
// read from sources/shared/SharedAssemblyInfo.cs and overlaid into a generated copy. The two tasks differ only in
// their wrapper concerns:
//   ResolveStrideWorktreeVersion - dev builds: per-checkout ledger -> -devN suffix, tag-state cache, no metadata.
//   StrideGitVersion             - release/package builds: release suffix + "+g<sha>" build metadata, no ledger.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Shared version-resolution logic for both the dev and release generators.
internal static class StrideVersionUtil
{
    // Reads the editable inputs from SharedAssemblyInfo.cs. The floor version is composed MajorMinor.MinPatch
    // (MinPatch is the only patch source; MajorMinor the only major.minor source — no duplication to drift).
    public static void ReadInputs(string sourceFile, out string majorMinor, out string minVersion, out string suffix)
    {
        string data = File.ReadAllText(sourceFile);
        majorMinor = Field(data, "MajorMinor");
        minVersion = majorMinor + "." + Field(data, "MinPatch");
        suffix = Field(data, "NuGetVersionSuffix");
    }

    private static string Field(string data, string name)
        => Regex.Match(data, name + " = \"([^\"]*)\";").Groups[1].Value;

    // The build version: the highest of the committed floor, the release tags, and the optional override - so a
    // committed bump or a local override is honored immediately but a newer release tag always wins once it passes
    // them. Tag candidate = the exact releases/<mm>.<N> on HEAD (idempotent re-release, no +1), else the highest
    // reachable + 1; absent any tag it contributes nothing (the floor stands). Ancestor-scoped so branches and
    // fork-only tags don't leak in; all-local so it works offline.
    public static string ResolveFloor(string workingDir, string majorMinor, string minVersion, string overrideVersion, TaskLoggingHelper log)
    {
        string best = minVersion;
        if (!string.IsNullOrWhiteSpace(overrideVersion))
            best = MaxVersion(best, overrideVersion.Trim());
        try
        {
            int patch;
            if (TryMaxReleasePatch(Git(workingDir, "tag --points-at HEAD --list releases/" + majorMinor + ".*"), majorMinor, out patch))
                best = MaxVersion(best, majorMinor + "." + patch);
            else if (TryMaxReleasePatch(Git(workingDir, "tag --list releases/" + majorMinor + ".* --merged HEAD"), majorMinor, out patch))
                best = MaxVersion(best, majorMinor + "." + (patch + 1));
            else
            {
                bool shallow = Git(workingDir, "rev-parse --is-shallow-repository") == "true";
                log.LogMessage(MessageImportance.High, "No releases/" + majorMinor + ".* tags reachable from HEAD - using " + best + ". " +
                    (shallow ? "Shallow clone; run `git fetch --unshallow` for the real version."
                             : "Run `git fetch --tags` for the real version."));
            }
        }
        catch (Exception e)
        {
            log.LogMessage(MessageImportance.Low, "Release-tag version lookup skipped: " + e.Message);
        }
        return best;
    }

    // Higher of two dotted version strings (numeric, not lexical so "4.4.10" > "4.4.9"). A non-parseable side loses.
    public static string MaxVersion(string a, string b)
    {
        bool oka = Version.TryParse(a, out var va);
        bool okb = Version.TryParse(b, out var vb);
        if (!oka) return okb ? b : a;
        if (!okb) return a;
        return va >= vb ? a : b;
    }

    // Highest patch N among "releases/<mm>.<N>" lines (stable releases only; suffixed prerelease tags are ignored,
    // so the suffix is the pre-release of the same N). Returns false if none.
    public static bool TryMaxReleasePatch(string tagList, string mm, out int best)
    {
        best = -1;
        var rx = new Regex("^releases/" + Regex.Escape(mm) + "\\.(\\d+)$");
        foreach (var tag in tagList.Split('\n'))
        {
            var m = rx.Match(tag.Trim());
            if (m.Success)
            {
                int p = int.Parse(m.Groups[1].Value);
                if (p > best) best = p;
            }
        }
        return best >= 0;
    }

    public static string Git(string workingDir, string args)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using (var p = Process.Start(psi))
        {
            string output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            if (p.ExitCode != 0) throw new Exception("git " + args + " exited with " + p.ExitCode);
            return output;
        }
    }

    // Overlay the computed version + suffix (+ optional build metadata) into a copy of the source version file.
    // Rewrites MinPatch (the part of the version within MajorMinor); MinVersion/PublicVersion derive from it, and
    // the readers + compiled consts pick it up. The computed version is within MajorMinor for the floor and the
    // release tags; a StridePublicVersion override pointing outside MajorMinor is out of contract (bump MajorMinor).
    public static string Overlay(string source, string majorMinor, string version, string suffix, string buildMetadata)
    {
        string minPatch = version.StartsWith(majorMinor + ".", StringComparison.Ordinal)
            ? version.Substring(majorMinor.Length + 1)
            : version;
        string patched = Regex.Replace(source, "MinPatch = \"[^\"]*\";", "MinPatch = \"" + minPatch + "\";");
        patched = Regex.Replace(patched, "NuGetVersionSuffix = \"[^\"]*\";", "NuGetVersionSuffix = \"" + suffix + "\";");
        if (buildMetadata != null)
            patched = Regex.Replace(patched, "BuildMetadata = \"[^\"]*\";", "BuildMetadata = \"" + buildMetadata + "\";");
        return patched;
    }
}

// Dev builds: resolve this checkout's -devN suffix from a per-machine ledger and overlay the build version into
// SharedAssemblyInfo.Worktree.cs. Multiple checkouts on one machine each get a distinct suffix so they stop
// clobbering each other in the shared NugetDev feed / global cache. A tag-state stamp caches the result so the
// per-project invocations don't each shell out to git.
public class ResolveStrideWorktreeVersion : Task
{
    [Required] public string StrideRoot { get; set; }

    // Explicit override (StrideWorktreeId property/env var); wins over the ledger when set.
    public string OverrideId { get; set; }

    // Ledger location override (StrideWorktreeLedger property); defaults to LocalApplicationData/Stride/worktree-ids.txt.
    public string LedgerPath { get; set; }

    [Required] public string SourceVersionFile { get; set; }
    [Required] public string GeneratedVersionFile { get; set; }

    // Explicit dev version floor (StridePublicVersion property, set in the gitignored build/Stride.Local.props).
    public string PublicVersionOverride { get; set; }

    [Output] public string WorktreeSuffix { get; set; }
    [Output] public bool Generated { get; set; }

    public override bool Execute()
    {
        WorktreeSuffix = string.Empty;
        try
        {
            string root = NormalizePath(StrideRoot);
            string ledgerPath = !string.IsNullOrWhiteSpace(LedgerPath)
                ? LedgerPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stride", "worktree-ids.txt");
            // Stamp = tag-state + override + ledger mtime + committed MinVersion, so a tag fetch, a manual
            // worktree-ID remap, or a committed version bump each invalidate the generated overlay (the tag-state
            // alone doesn't change when only the ledger or SharedAssemblyInfo.cs is edited).
            string stamp = ComputeStamp(root) + "|" + (PublicVersionOverride ?? string.Empty) + "|" + LedgerStamp(ledgerPath) + "|" + ReadMinVersion();
            if (string.IsNullOrWhiteSpace(OverrideId) && TryFastPath(stamp))
                return true;
            Directory.CreateDirectory(Path.GetDirectoryName(ledgerPath));

            for (int attempt = 0; attempt < 300; attempt++)
            {
                try
                {
                    // FileShare.None on a sibling lock file serializes the first-build race across MSBuild's
                    // parallel project nodes (cross-process, cross-platform).
                    using (new FileStream(ledgerPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        string token = !string.IsNullOrWhiteSpace(OverrideId)
                            ? OverrideId.Trim()
                            : ResolveOrRegister(ledgerPath, root);

                        WorktreeSuffix = TokenToSuffix(token);
                        WriteGeneratedFile(WorktreeSuffix, stamp);
                    }
                    return true;
                }
                catch (IOException)
                {
                    Thread.Sleep(5);
                }
            }
            throw new IOException("Timed out acquiring the Stride worktree ledger lock at " + ledgerPath);
        }
        catch (Exception e)
        {
            Log.LogWarning("Stride worktree version resolution skipped: " + e.Message);
            WorktreeSuffix = string.Empty;
            return true;
        }
    }

    private string ResolveOrRegister(string ledgerPath, string root)
    {
        var lines = File.Exists(ledgerPath)
            ? new List<string>(File.ReadAllLines(ledgerPath))
            : new List<string>();

        // Steady state: already registered — return the existing token, no pruning.
        foreach (string line in lines)
        {
            string token, dir;
            if (TryParseLine(line, out token, out dir)
                && NormalizePath(dir).Equals(root, PathComparison))
                return token;
        }

        // Not registered: drop entries whose folder is gone, then allocate the lowest free slot (1 -> "dev",
        // 2 -> "dev2", ...). So deleting dev2 while dev3 is taken frees dev2 for the next checkout, instead of
        // climbing to dev4 indefinitely. UNC entries (\\server\share\...) are kept regardless — a transient
        // network failure makes Directory.Exists return false, which would silently delete a valid entry.
        var kept = new List<string>(lines.Count);
        var liveDevNumbers = new HashSet<int>();
        foreach (string line in lines)
        {
            string token, dir;
            if (!TryParseLine(line, out token, out dir))
            {
                kept.Add(line);
                continue;
            }
            string normalizedDir = NormalizePath(dir);
            bool isUnc = normalizedDir.StartsWith("\\\\", StringComparison.Ordinal)
                      || normalizedDir.StartsWith("//", StringComparison.Ordinal);
            if (!isUnc && !Directory.Exists(normalizedDir))
            {
                Log.LogMessage(MessageImportance.High,
                    "Pruning stale Stride worktree ledger entry: '" + token + "' -> '" + dir + "'");
                continue;
            }
            kept.Add(line);
            // Slot occupancy: "dev" (and the legacy "(primary)") = slot 1, "devN" = slot N.
            // "(empty)" holds no slot — it's the explicit no-suffix opt-out.
            if (token.Equals("dev", StringComparison.OrdinalIgnoreCase)
                || token.Equals("(primary)", StringComparison.OrdinalIgnoreCase))
                liveDevNumbers.Add(1);
            else
            {
                Match m = Regex.Match(token, "^dev([0-9]+)$");
                if (m.Success)
                    liveDevNumbers.Add(int.Parse(m.Groups[1].Value));
            }
        }

        // Lowest free slot: 1 -> "dev", 2 -> "dev2", ... So the first checkout is "-dev".
        int n = 1;
        while (liveDevNumbers.Contains(n)) n++;
        string newToken = n == 1 ? "dev" : "dev" + n;

        if (kept.Count == 0)
            kept.Add("# Stride checkout version-suffix ledger. Tokens: dev (-dev), dev2, dev3, ...; '(empty)' = no suffix (clean build).");
        kept.Add(newToken + " = " + root);
        File.WriteAllLines(ledgerPath, kept);
        Log.LogMessage(MessageImportance.High,
            "Registered this checkout in the Stride worktree ledger as '" + newToken + "' (" + ledgerPath + ")");
        return newToken;
    }

    private void WriteGeneratedFile(string suffix, string stamp)
    {
        string majorMinor, minVersion, baseSuffix;
        StrideVersionUtil.ReadInputs(SourceVersionFile, out majorMinor, out minVersion, out baseSuffix);
        string devVersion = StrideVersionUtil.ResolveFloor(StrideRoot, majorMinor, minVersion, PublicVersionOverride, Log);

        if (suffix.Length == 0 && devVersion == minVersion)
        {
            // No suffix (e.g. the "(empty)" opt-out) and no release-tag/override delta: nothing differs.
            if (File.Exists(GeneratedVersionFile))
                File.Delete(GeneratedVersionFile);
            return;
        }

        string patched = StrideVersionUtil.Overlay(File.ReadAllText(SourceVersionFile), majorMinor, devVersion, suffix, null);
        // Cache key = stamp + resolved suffix; TryFastPath reuses the file while the stamp matches.
        patched += "\n// git-stamp: " + stamp + "|" + suffix + "\n";
        if (!File.Exists(GeneratedVersionFile) || File.ReadAllText(GeneratedVersionFile) != patched)
            File.WriteAllText(GeneratedVersionFile, patched);
        Generated = true;
    }

    // The committed MinVersion (floor) from the source file; "" if unreadable. Part of the cache stamp so a
    // committed bump invalidates the generated overlay.
    private string ReadMinVersion()
    {
        try
        {
            string mm, mv, sfx;
            StrideVersionUtil.ReadInputs(SourceVersionFile, out mm, out mv, out sfx);
            return mv;
        }
        catch { return string.Empty; }
    }

    // Tag-state signature: mtimes/size of packed-refs + loose refs/tags. Changes when release tags are
    // created/fetched, NOT on commits -- so the version is recomputed only when it could actually change. Handles
    // the worktree ".git file -> gitdir -> commondir" indirection (linked worktrees share the common dir's tags).
    private string ComputeStamp(string root)
    {
        try
        {
            string gitPath = Path.Combine(root, ".git");
            string gitDir = gitPath;
            if (File.Exists(gitPath))
            {
                string line = File.ReadAllText(gitPath).Trim();
                if (!line.StartsWith("gitdir: ", StringComparison.Ordinal))
                    return string.Empty;
                gitDir = Path.GetFullPath(Path.Combine(root, line.Substring(8)));
            }
            string commonDirFile = Path.Combine(gitDir, "commondir");
            if (File.Exists(commonDirFile))
                gitDir = Path.GetFullPath(Path.Combine(gitDir, File.ReadAllText(commonDirFile).Trim()));
            var packed = new FileInfo(Path.Combine(gitDir, "packed-refs"));
            var looseTags = new DirectoryInfo(Path.Combine(gitDir, "refs", "tags"));
            string s = packed.Exists ? packed.LastWriteTimeUtc.Ticks + ":" + packed.Length : "0";
            s += "/" + (looseTags.Exists ? looseTags.LastWriteTimeUtc.Ticks.ToString() : "0");
            return s;
        }
        catch
        {
            return string.Empty;
        }
    }

    private bool TryFastPath(string stamp)
    {
        // Reuse the generated file when the stamp (tag-state, override, ledger mtime, committed MinVersion) is
        // unchanged; the file marker is "stamp|suffix", so it must start with stamp + "|".
        if (!File.Exists(GeneratedVersionFile))
            return false;
        var m = Regex.Match(File.ReadAllText(GeneratedVersionFile), "// git-stamp: (.*)");
        if (!m.Success)
            return false;
        string key = m.Groups[1].Value;
        if (!key.StartsWith(stamp + "|", StringComparison.Ordinal))
            return false;
        WorktreeSuffix = key.Substring(stamp.Length + 1).Trim();
        Generated = true;
        return true;
    }

    // Ledger mtime as a stamp component: changes when the file is remapped, so a manual edit to this checkout's
    // token re-triggers resolution instead of reusing a stale overlay.
    private static string LedgerStamp(string ledgerPath)
    {
        return File.Exists(ledgerPath) ? File.GetLastWriteTimeUtc(ledgerPath).Ticks.ToString() : "0";
    }

    // Windows file paths are case-insensitive; Linux is case-sensitive; macOS depends on the volume. On macOS the
    // FS reports paths in their stored casing either way, so Ordinal still matches the same checkout across builds —
    // and Ordinal correctly distinguishes truly-distinct case-sensitive checkouts on Linux (where OrdinalIgnoreCase
    // would falsely merge `/work/stride` and `/work/Stride` and silently clobber both).
    private static readonly StringComparison PathComparison =
        Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static string NormalizePath(string p)
        => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string TokenToSuffix(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;
        token = token.Trim();
        // Explicit opt-out: a clean build with no suffix (e.g. to reproduce a release).
        if (token.Equals("(empty)", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        // Legacy alias: ledgers written before "dev" became the default first-checkout token.
        if (token.Equals("(primary)", StringComparison.OrdinalIgnoreCase))
            return "-dev";
        return token[0] == '-' ? token : "-" + token;
    }

    private static bool TryParseLine(string line, out string token, out string dir)
    {
        token = null;
        dir = null;
        if (string.IsNullOrWhiteSpace(line))
            return false;
        string t = line.Trim();
        if (t[0] == '#')
            return false;
        int eq = t.IndexOf('=');
        if (eq <= 0)
            return false;
        token = t.Substring(0, eq).Trim();
        dir = t.Substring(eq + 1).Trim();
        return token.Length != 0 && dir.Length != 0;
    }
}

// Release/package builds: overlay the tag-resolved build version + release suffix + "+g<sha>" build metadata into
// SharedAssemblyInfo.NuGet.cs. Shares the floor logic with the dev generator (so a committed MinVersion bump is
// honored at release too), but has no ledger/cache and adds build metadata.
public class StrideGitVersion : Task
{
    [Required] public string RootDirectory { get; set; }
    [Required] public string VersionFile { get; set; }
    [Required] public string GeneratedVersionFile { get; set; }
    public string SuffixOverride { get; set; }

    [Output] public string NuGetVersion { get; set; }

    public override bool Execute()
    {
        try
        {
            VersionFile = VersionFile.Replace('\\', '/');
            GeneratedVersionFile = GeneratedVersionFile.Replace('\\', '/');
            string sourcePath = Path.Combine(RootDirectory, VersionFile);

            string majorMinor, minVersion, suffix;
            StrideVersionUtil.ReadInputs(sourcePath, out majorMinor, out minVersion, out suffix);
            // SuffixOverride (from -p:StrideVersionSuffix) is the bare word; add the dash here.
            if (!string.IsNullOrEmpty(SuffixOverride))
                suffix = "-" + SuffixOverride.TrimStart('-');

            string version = StrideVersionUtil.ResolveFloor(RootDirectory, majorMinor, minVersion, null, Log);
            string sha = StrideVersionUtil.Git(RootDirectory, "rev-parse HEAD").Substring(0, 8);

            string patched = StrideVersionUtil.Overlay(File.ReadAllText(sourcePath), majorMinor, version, suffix, "+g" + sha);
            File.WriteAllText(Path.Combine(RootDirectory, GeneratedVersionFile), patched);

            NuGetVersion = version + suffix;
            return true;
        }
        catch (Exception e)
        {
            Log.LogError("StrideGitVersion failed: " + e.Message);
            return false;
        }
    }
}
