// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.CrashReport;

/// <summary>How a headless tool handles crashes, from <c>STRIDE_CRASH_MODE</c>.</summary>
public enum CrashMode
{
    /// <summary>Default: ask when attended, save when not; never auto-sends.</summary>
    Interactive,

    /// <summary>Auto-send with no UI (CI only).</summary>
    Send,

    /// <summary>Always write to disk, never popup or send (also the forced-unattended fallback).</summary>
    Save,

    /// <summary>Crash handling disabled entirely: capture nothing.</summary>
    Off,
}

/// <summary>What to actually do with a build's captured crashes once it finishes.</summary>
public enum CrashAction
{
    /// <summary>Do nothing (mode is off).</summary>
    Ignore,

    /// <summary>Leave the crashes on disk for a later <c>stride crash send</c>.</summary>
    Save,

    /// <summary>Send them headlessly, no UI (CI).</summary>
    Send,

    /// <summary>Spawn the reporter so an attended user can decide.</summary>
    Report,
}

/// <summary>
/// Decides, from the environment, how a headless tool should treat crashes: the <c>STRIDE_CRASH_MODE</c>
/// override, and — for the default interactive mode — whether anyone is at the console to answer a reporter.
/// </summary>
public static class CrashPolicy
{
    public const string EnvMode = "STRIDE_CRASH_MODE";
    public const string EnvDump = "STRIDE_CRASH_DUMP";

    /// <summary>
    /// True when <c>STRIDE_CRASH_DUMP=full</c>: capture a full-memory dump (heap and all) instead of the
    /// triage dump, for a dev deliberately chasing a hard crash. A full dump is large and UNSCRUBBED, so it is
    /// never sent — only saved locally and kept if the user chooses (the sender refuses a full-flagged dump).
    /// Off by default.
    /// </summary>
    public static bool FullMemoryDump()
        => string.Equals(Environment.GetEnvironmentVariable(EnvDump)?.Trim(), "full", StringComparison.OrdinalIgnoreCase);

    public static CrashMode ResolveMode()
        => Environment.GetEnvironmentVariable(EnvMode)?.Trim().ToLowerInvariant() switch
        {
            "send" => CrashMode.Send,
            "save" => CrashMode.Save,
            "off" => CrashMode.Off,
            _ => CrashMode.Interactive,
        };

    /// <summary>The end-of-build action for the current environment: report when attended, save when not.</summary>
    public static CrashAction ResolveAction()
        => ResolveMode() switch
        {
            CrashMode.Off => CrashAction.Ignore,
            CrashMode.Save => CrashAction.Save,
            CrashMode.Send => CrashAction.Send,
            _ => IsUnattended() ? CrashAction.Save : CrashAction.Report,
        };

    /// <summary>
    /// True when no human is at the console, so a reporter must never pop up: a CI variable is set, we are in
    /// an SSH session, or the OS reports no interactive desktop. Order does not matter — any signal wins.
    /// </summary>
    public static bool IsUnattended()
    {
        foreach (var variable in CiVariables)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(variable)))
                return true;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SSH_CONNECTION"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SSH_TTY")))
            return true;

        if (OperatingSystem.IsWindows())
            return !Environment.UserInteractive;
        if (OperatingSystem.IsLinux())
            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))
                && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
        return false; // macOS desktop: attended unless SSH, which is handled above
    }

    private static readonly string[] CiVariables =
        { "CI", "GITHUB_ACTIONS", "TF_BUILD", "JENKINS_URL", "TEAMCITY_VERSION", "GITLAB_CI", "BUILDKITE" };
}
