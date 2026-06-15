// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text.RegularExpressions;

// Shared CI-artifact plumbing: the `gh run download` invocation used by both the web add-ci endpoint
// and the headless promote/dedup (one source of truth), plus run-owner resolution across the current
// checkout's git remotes so a bare run id works for whichever remote owns it (origin or a fork).
internal static class CiArtifacts
{
    public const string UpstreamRepo = "stride3d/stride";

    // gh run download <runId> [--repo <repo>] --name <artifactName> --dir <dir>. Null on success,
    // else a human-readable error. Empty/null repo lets gh infer it from the current checkout.
    public static string? Download(string runId, string? repo, string artifactName, string dir)
    {
        var args = new List<string> { "run", "download", runId, "--name", artifactName, "--dir", dir };
        if (!string.IsNullOrEmpty(repo)) { args.Add("--repo"); args.Add(repo); }
        var (exit, _, stderr) = Run("gh", args);
        if (exit is null) return "could not run gh; install the GitHub CLI or download the artifact manually";
        return exit == 0 ? null : $"gh failed for {artifactName}: {stderr.Trim()}";
    }

    // Probe each github.com remote in the current checkout for <runId>; return the first repo that
    // owns it, or null. Lets a bare run id resolve without the caller naming the repo.
    public static string? ResolveRepoFromRemotes(string runId)
    {
        foreach (var repo in GitHubRemotes())
        {
            var (exit, _, _) = Run("gh", ["api", $"repos/{repo}/actions/runs/{runId}", "--silent"]);
            if (exit == 0) return repo;
        }
        return null;
    }

    private static IEnumerable<string> GitHubRemotes()
    {
        var (exit, stdout, _) = Run("git", ["remote", "-v"]);
        if (exit != 0 || string.IsNullOrEmpty(stdout)) yield break;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Matches git@github.com:owner/repo(.git) and https://github.com/owner/repo(.git).
        foreach (Match m in Regex.Matches(stdout, @"github\.com[:/]([^/\s]+)/([^/\s]+?)(?:\.git)?(?:\s|$)"))
        {
            var repo = $"{m.Groups[1].Value}/{m.Groups[2].Value}";
            if (seen.Add(repo)) yield return repo;
        }
    }

    // Runs a process, capturing stdout/stderr. exit is null when the executable couldn't be started.
    private static (int? exit, string? stdout, string stderr) Run(string file, IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo(file) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        Process proc;
        try { proc = Process.Start(psi)!; }
        catch { return (null, null, ""); }
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }
}
