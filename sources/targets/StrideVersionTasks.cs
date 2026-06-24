// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Build version generators, compiled at runtime by RoslynCodeTaskFactory (no prebuilt task assembly).
// Referenced from both Stride.WorktreeVersion.targets and Stride.GitVersion.targets via <Code Source>.
//
// The version is the committed value in SharedAssemblyInfo.cs (MajorMinor.Patch + NuGetVersionSuffix); it is
// bumped per release rather than derived from git tags. Both generators
// overlay that value into a generated copy; they differ only in their wrapper concerns:
//   ResolveStrideWorktreeVersion - dev builds: per-checkout ledger -> -devN suffix, cached, no metadata.
//   StrideGitVersion             - release/package builds: release suffix + "+g<sha>" build metadata, no ledger.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// Shared version logic for both the dev and release generators.
internal static class StrideVersionUtil
{
    // Reads the editable inputs from SharedAssemblyInfo.cs. The version is composed MajorMinor.Patch (Patch is the
    // only patch source; MajorMinor the only major.minor source — no duplication to drift).
    public static void ReadInputs(string sourceFile, out string majorMinor, out string version, out string suffix)
    {
        string data = File.ReadAllText(sourceFile);
        majorMinor = Field(data, "MajorMinor");
        string patch = Field(data, "Patch");
        suffix = Field(data, "NuGetVersionSuffix");
        // Fail loudly on an unparseable version rather than emit a malformed one (e.g. "." or "4.4.") that would
        // ship a weird package or compile a garbage const. Field returns "" when a line's shape changed.
        if (!Regex.IsMatch(majorMinor, @"^[0-9]+\.[0-9]+$") || !Regex.IsMatch(patch, @"^[0-9]+$"))
            throw new Exception("Could not parse a valid version from " + sourceFile + " (MajorMinor='" + majorMinor +
                "', Patch='" + patch + "'). Check the 'MajorMinor = \"x.y\";' and 'Patch = \"n\";' lines.");
        version = majorMinor + "." + patch;
    }

    private static string Field(string data, string name)
        => Regex.Match(data, name + " = \"([^\"]*)\";").Groups[1].Value;

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

    // Overlay the version + suffix (+ optional build metadata) into a copy of the source version file. Rewrites
    // Patch (read back by the package producers) and PublicVersion (the sentinel in the template -> the real
    // version, read by the compiled consts).
    public static string Overlay(string source, string majorMinor, string version, string suffix, string buildMetadata)
    {
        string patch = version.StartsWith(majorMinor + ".", StringComparison.Ordinal)
            ? version.Substring(majorMinor.Length + 1)
            : version;
        string patched = Regex.Replace(source, "Patch = \"[^\"]*\";", "Patch = \"" + patch + "\";");
        patched = Regex.Replace(patched, "const string PublicVersion = [^;]*;", "const string PublicVersion = \"" + version + "\";");
        patched = Regex.Replace(patched, "NuGetVersionSuffix = \"[^\"]*\";", "NuGetVersionSuffix = \"" + suffix + "\";");
        if (buildMetadata != null)
            patched = Regex.Replace(patched, "BuildMetadata = \"[^\"]*\";", "BuildMetadata = \"" + buildMetadata + "\";");
        return patched;
    }
}

// The per-project version generator: resolve this checkout's suffix and overlay the version into
// SharedAssemblyInfo.Generated.cs (the single file the Stride SDK swaps in). On dev builds the suffix comes from a
// per-machine ledger (-devN) so multiple checkouts on one machine stop clobbering each other in the shared NugetDev
// feed / global cache. On CI (and when StrideSkipWorktreeVersion is set) NoLedgerSuffix gives the clean, unsuffixed
// version with no ledger touch. A stamp caches the result so the per-project invocations don't each rewrite the file.
public class ResolveStrideWorktreeVersion : Task
{
    [Required] public string StrideRoot { get; set; }

    // Explicit override (StrideWorktreeId property/env var); wins over the ledger when set.
    public string OverrideId { get; set; }

    // CI / StrideSkipWorktreeVersion: emit the overlay with an empty suffix and no ledger registration (but keep the
    // fast path, unlike an explicit OverrideId).
    public bool NoLedgerSuffix { get; set; }

    // Ledger location override (StrideWorktreeLedger property); defaults to LocalApplicationData/Stride/worktree-ids.txt.
    public string LedgerPath { get; set; }

    [Required] public string SourceVersionFile { get; set; }
    [Required] public string GeneratedVersionFile { get; set; }

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
            // Stamp = ledger mtime + committed version+suffix, so a manual worktree-ID remap or a committed
            // version/suffix bump (edit to SharedAssemblyInfo.cs) invalidates the generated overlay.
            string stamp = LedgerStamp(ledgerPath) + "|" + ReadCommittedVersion();
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
                            : NoLedgerSuffix
                                ? "(empty)"
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
        string majorMinor, version, baseSuffix;
        StrideVersionUtil.ReadInputs(SourceVersionFile, out majorMinor, out version, out baseSuffix);

        // Compose the committed suffix with the worktree suffix so the const matches the package: the consumer
        // overlay replaces the whole NuGetVersionSuffix, so it must carry both (e.g. "-beta1" + "-dev3"), whereas the
        // producer reads the committed "-beta1" from the file and appends only the worktree "-dev3" (WorktreeSuffix
        // output stays worktree-only for that append). Always emit the overlay (even when it equals the committed
        // version): the base file's PublicVersion is a sentinel, so leaving the base to compile would ship it.
        string patched = StrideVersionUtil.Overlay(File.ReadAllText(SourceVersionFile), majorMinor, version, baseSuffix + suffix, null);
        // Cache key = stamp + resolved worktree suffix; TryFastPath reuses the file while the stamp matches.
        patched += "\n// version-stamp: " + stamp + "|" + suffix + "\n";
        if (!File.Exists(GeneratedVersionFile) || File.ReadAllText(GeneratedVersionFile) != patched)
            File.WriteAllText(GeneratedVersionFile, patched);
        Generated = true;
    }

    // The committed version + suffix from the source file; "" if unreadable. Part of the cache stamp so a committed
    // version or suffix bump invalidates the generated overlay.
    private string ReadCommittedVersion()
    {
        try
        {
            string mm, mv, sfx;
            StrideVersionUtil.ReadInputs(SourceVersionFile, out mm, out mv, out sfx);
            return mv + sfx;
        }
        catch { return string.Empty; }
    }

    private bool TryFastPath(string stamp)
    {
        // Reuse the generated file when the stamp (override, ledger mtime, committed version) is unchanged; the file
        // marker is "stamp|suffix", so it must start with stamp + "|".
        if (!File.Exists(GeneratedVersionFile))
            return false;
        var m = Regex.Match(File.ReadAllText(GeneratedVersionFile), "// version-stamp: (.*)");
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

// Release/package builds: overlay the committed version + release suffix + "+g<sha>" build metadata into
// SharedAssemblyInfo.Generated.cs. No ledger; the only git use is reading HEAD's short sha for the build metadata.
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

            string majorMinor, version, suffix;
            StrideVersionUtil.ReadInputs(sourcePath, out majorMinor, out version, out suffix);
            // SuffixOverride (from -p:StrideVersionSuffix) overrides the committed suffix; it's the bare word.
            if (!string.IsNullOrEmpty(SuffixOverride))
                suffix = "-" + SuffixOverride.TrimStart('-');

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
