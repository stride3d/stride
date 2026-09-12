// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;

namespace Stride.Core.Assets;

/// <summary>
/// Picks the .NET major a host (Game Studio, launcher, CLI) starts the editor on. The editor loads the game's
/// assemblies into its own process, so a game targeting a newer major than the editor's build needs the editor
/// re-executed on that major: <c>dotnet exec --runtimeconfig</c> with a generated config. Framework-only on
/// purpose: it runs before the NuGet resolver and is linked into the launcher and the CLI.
/// </summary>
public static class DotNetHostSelector
{
    /// <summary>Marks a process the selector already re-executed; such a process never re-executes again.</summary>
    public const string RelaunchedArg = "--host-relaunched";

    /// <summary>Minimum major (<c>--framework net11.0</c>): raises the project's major, never lowers it; a global.json SDK pin wins over it.</summary>
    public const string FrameworkArg = "--framework";

    /// <summary>Environment form of <see cref="FrameworkArg"/>, a bare major such as <c>11</c>.</summary>
    public const string RuntimeMajorEnv = "STRIDE_RUNTIME_MAJOR";

    /// <summary>Debug switch (<c>1</c>): re-execute through the muxer even when the major is unchanged.</summary>
    public const string ForceRelaunchEnv = "STRIDE_HOST_FORCE_RELAUNCH";

    public enum DecisionKind { UseCurrent, Relaunch, Missing }

    /// <param name="Major">Major to run on (Relaunch), or the one that is needed but unavailable (Missing).</param>
    /// <param name="Reason">User-facing explanation, set for Missing.</param>
    /// <summary>The outcome; <paramref name="Install"/> is the installation a relaunch was decided against, for the start line.</summary>
    public sealed record Decision(DecisionKind Kind, int Major, string? Reason = null, DotNetInstall? Install = null);

    /// <summary>Where generated runtimeconfigs go; tests redirect it.</summary>
    internal static string RuntimeConfigDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "host");

    /// <summary>Decision for an app: reads its runtimeconfig, the session's projects and global.json, the args and the environment.</summary>
    /// <param name="currentMajor">Major the app would otherwise run on.</param>
    public static Decision ResolveFor(string appDllPath, int currentMajor, string? sessionPath, IEnumerable<string> args)
    {
        var argList = args.ToList();
        var relaunched = argList.Contains(RelaunchedArg);
        var forced = !relaunched && Environment.GetEnvironmentVariable(ForceRelaunchEnv) == "1";

        // The common case, a session on our own major, is settled without looking at the .NET installation.
        var sessionDirectory = sessionPath != null ? Path.GetDirectoryName(Path.GetFullPath(sessionPath)) : null;
        var overrideMajor = ReadOverride(argList);
        var globalJsonMajor = sessionDirectory != null ? GetGlobalJsonSdkMajor(sessionDirectory) : null;
        var requiredMajor = sessionPath != null ? GetRequiredMajor(sessionPath) : null;
        var (wanted, _, _) = WantedMajor(requiredMajor, globalJsonMajor, overrideMajor);
        if (!forced && (wanted == null || wanted <= currentMajor))
            return new Decision(DecisionKind.UseCurrent, currentMajor);

        var install = DotNetInstall.Detect();
        if (forced && install != null)
            return new Decision(DecisionKind.Relaunch, currentMajor, Install: install);
        return Resolve(currentMajor, requiredMajor, globalJsonMajor, overrideMajor, relaunched, ReadFrameworks(appDllPath), install);
    }

    /// <summary>What asked for the major a session runs on.</summary>
    public enum MajorSource { Project, Choice, GlobalJson }

    /// <summary>
    /// The major a session asks for, whether it is exact, and what asked for it. A global.json SDK pin is exact: MSBuild
    /// on another major's SDK would not honor it. Otherwise the project sets a floor and an explicit choice can only raise
    /// it, since a game's assemblies do not load below their own major.
    /// </summary>
    public static (int? Major, bool Exact, MajorSource Source) WantedMajor(int? requiredMajor, int? globalJsonMajor, int? overrideMajor)
    {
        if (globalJsonMajor is { } pinned)
            return (pinned, true, MajorSource.GlobalJson);
        if (overrideMajor is null || requiredMajor >= overrideMajor)
            return (requiredMajor, false, MajorSource.Project);
        return (overrideMajor, false, MajorSource.Choice);
    }

    /// <summary>Pure decision over gathered inputs.</summary>
    public static Decision Resolve(int currentMajor, int? requiredMajor, int? globalJsonMajor, int? overrideMajor,
        bool relaunched, IReadOnlyList<string> frameworks, DotNetInstall? install)
    {
        var (wanted, exact, source) = WantedMajor(requiredMajor, globalJsonMajor, overrideMajor);
        // Never below the app's own major: a game's assemblies load on any major at or above their TFM.
        if (wanted == null || wanted <= currentMajor)
            return new Decision(DecisionKind.UseCurrent, currentMajor);

        var major = wanted.Value;
        // The messages name what asked for the major: the project, an explicit choice, or global.json.
        var request = source switch
        {
            MajorSource.Choice => $"Game Studio was asked to run on .NET {major} or newer",
            MajorSource.GlobalJson => $"This project's global.json asks for the .NET {major} SDK",
            _ => $"This project needs .NET {major}",
        };
        if (relaunched)
            return new Decision(DecisionKind.Missing, major, $"{request}, but the editor is still on .NET {currentMajor} after re-executing.");
        if (install == null)
            return new Decision(DecisionKind.Missing, major, $"{request}, but no .NET installation was found.");

        // Lowest major that has every framework the app needs and an SDK (the editor hosts MSBuild in-process).
        IEnumerable<int> candidates = exact ? [major] : install.Majors().Where(m => m >= major);
        foreach (var candidate in candidates)
        {
            if (frameworks.All(f => install.Highest(f, candidate) != null) && install.HasSdk(candidate))
                return new Decision(DecisionKind.Relaunch, candidate, Install: install);
        }

        var missingFrameworks = frameworks.Where(f => install.Highest(f, major) == null).ToList();
        var missing = new List<string>();
        if (missingFrameworks.Count > 0)
            missing.Add($"the {string.Join(" and ", missingFrameworks)} runtime{(missingFrameworks.Count > 1 ? "s" : "")}");
        if (!install.HasSdk(major))
            missing.Add("the SDK");
        var plural = missing.Count > 1 || missingFrameworks.Count > 1;
        return new Decision(DecisionKind.Missing, major,
            $"{request}, but {string.Join(" and ", missing)} of .NET {major} {(plural ? "are" : "is")} not installed under {install.Root}.\n\nInstall the .NET {major} SDK from https://dotnet.microsoft.com/download and try again.");
    }

    /// <summary>The explicit major from <c>--framework</c> (a TFM or a number), else from <see cref="RuntimeMajorEnv"/>.</summary>
    public static int? ReadOverride(IReadOnlyList<string> args)
    {
        for (var i = 0; i + 1 < args.Count; i++)
        {
            if (args[i] == FrameworkArg)
                return ParseMajor(args[i + 1]);
        }
        return ParseMajor(Environment.GetEnvironmentVariable(RuntimeMajorEnv));
    }

    /// <summary>Major of a TFM (<c>net11.0-windows</c>), a version (<c>11.0.100</c>) or a bare number; null for anything else (net472, netstandard2.1).</summary>
    public static int? ParseMajor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var match = Regex.Match(value.Trim(), @"^(?:net)?(\d+)\.\d+|^(\d+)$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;
        var digits = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return int.TryParse(digits, out var major) && major >= 5 ? major : null;
    }

    /// <summary>
    /// Highest .NET major among the session's executable projects (all projects when none is an executable), or
    /// null when nothing could be read. Executables set the requirement: a library never targets higher than the
    /// executable referencing it.
    /// </summary>
    public static int? GetRequiredMajor(string sessionOrProjectPath)
    {
        try
        {
            // The executables decide; only when there is none do the libraries. Their assets files are read only as needed.
            var projects = ListProjects(sessionOrProjectPath).Where(File.Exists)
                .Select(p => (Path: p, Text: File.ReadAllText(p)))
                .ToList();
            var executables = projects.Where(p => Regex.IsMatch(p.Text, @"<OutputType>\s*(Win)?Exe\s*</OutputType>", RegexOptions.IgnoreCase)).ToList();
            int? max = null;
            foreach (var project in executables.Count > 0 ? executables : projects)
            {
                if (ProjectMajor(project.Path, project.Text) is { } major)
                    max = Math.Max(max ?? 0, major);
            }
            return max;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or XmlException)
        {
            // Unreadable or malformed files (a hand-edited .slnf or assets file) leave the major unknown.
            return null;
        }
    }

    /// <summary>SDK major pinned by the nearest global.json at or above <paramref name="directory"/>; null when none pins one.</summary>
    public static int? GetGlobalJsonSdkMajor(string directory)
    {
        for (var current = directory; current != null; current = Path.GetDirectoryName(current))
        {
            var file = Path.Combine(current, "global.json");
            if (!File.Exists(file))
                continue;
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                if (!document.RootElement.TryGetProperty("sdk", out var sdk) || !sdk.TryGetProperty("version", out var version))
                    return null;
                // major and latestMajor let dotnet pick a newer SDK, so the version is not a pin.
                if (sdk.TryGetProperty("rollForward", out var rollForward) && rollForward.ValueKind == JsonValueKind.String && rollForward.GetString() is "major" or "latestMajor")
                    return null;
                return version.ValueKind == JsonValueKind.String ? ParseMajor(version.GetString()) : null;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>Shared frameworks the app's runtimeconfig references (Microsoft.NETCore.App when it has none).</summary>
    public static IReadOnlyList<string> ReadFrameworks(string appDllPath)
    {
        var names = new List<string>();
        var config = LoadRuntimeConfig(appDllPath);
        if (config != null)
        {
            foreach (var framework in FrameworksOf(config).OfType<JsonObject>())
            {
                if (framework["name"]?.GetValue<string>() is { } name)
                    names.Add(name);
            }
        }
        if (names.Count == 0)
            names.Add("Microsoft.NETCore.App");
        return names;
    }

    /// <summary>Major the app's own runtimeconfig asks for, which is what its apphost runs it on.</summary>
    public static int ReadNativeMajor(string appDllPath)
    {
        var config = LoadRuntimeConfig(appDllPath);
        if (config != null)
        {
            foreach (var framework in FrameworksOf(config).OfType<JsonObject>())
            {
                if (ParseMajor(framework["version"]?.GetValue<string>()) is { } major)
                    return major;
            }
        }
        return Environment.Version.Major;
    }

    /// <summary>
    /// Writes the app's runtimeconfig re-pointed at the newest installed patch of <paramref name="major"/> for every
    /// framework it references, under the user's local app data; returns its path. Only the config moves: deps.json
    /// and BaseDirectory stay with the app.
    /// </summary>
    public static string WriteRuntimeConfig(string appDllPath, int major, DotNetInstall install)
    {
        var config = LoadRuntimeConfig(appDllPath) ?? throw new FileNotFoundException("No runtimeconfig.json next to the app.", appDllPath);
        foreach (var framework in FrameworksOf(config).OfType<JsonObject>())
        {
            var name = framework["name"]?.GetValue<string>() ?? throw new InvalidDataException("Framework entry without a name.");
            var version = install.Highest(name, major) ?? throw new InvalidOperationException($"{name} {major}.x is not installed under {install.Root}.");
            framework["version"] = version.Text;
        }
        ((JsonObject)config["runtimeOptions"]!)["rollForward"] = "LatestPatch";
        var content = config.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        // Keyed on the app directory so two installs of the same app keep separate configs.
        var appDirectory = Path.GetDirectoryName(Path.GetFullPath(appDllPath))!;
        var baseName = $"{Path.GetFileNameWithoutExtension(appDllPath)}.{PathHash(appDirectory)}.net{major}";
        Directory.CreateDirectory(RuntimeConfigDirectory);
        var target = Path.Combine(RuntimeConfigDirectory, baseName + ".runtimeconfig.json");
        if (!File.Exists(target) || File.ReadAllText(target) != content)
            Replace(target, path => File.WriteAllText(path, content));

        // hostfxr looks for the .dev.json sibling next to the config it was given, not next to the app.
        var devConfig = Path.ChangeExtension(appDllPath, ".runtimeconfig.dev.json");
        if (File.Exists(devConfig))
            Replace(Path.Combine(RuntimeConfigDirectory, baseName + ".runtimeconfig.dev.json"), path => File.Copy(devConfig, path, overwrite: true));
        return target;
    }

    // Written through a temporary sibling then moved: parallel writers (compiler slaves) never leave a partial file
    // for a starting host to read.
    static void Replace(string target, Action<string> write)
    {
        var temporary = $"{target}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        try
        {
            write(temporary);
            File.Move(temporary, target, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    /// <summary>
    /// Start line re-executing the app on <paramref name="major"/>: <c>dotnet exec --runtimeconfig … app.dll args… --host-relaunched</c>.
    /// The marker is for an app that decides its own host (so it does not decide again); <paramref name="marker"/> false
    /// leaves it out for a tool that just runs where it is started.
    /// </summary>
    public static ProcessStartInfo RelaunchStartInfo(string appDllPath, int major, IEnumerable<string> appArgs, DotNetInstall? install = null, bool marker = true)
    {
        install ??= DotNetInstall.Detect() ?? throw new InvalidOperationException("No .NET installation was found.");
        var startInfo = new ProcessStartInfo(install.MuxerPath) { UseShellExecute = false };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--runtimeconfig");
        startInfo.ArgumentList.Add(WriteRuntimeConfig(appDllPath, major, install));
        startInfo.ArgumentList.Add(Path.GetFullPath(appDllPath));
        foreach (var arg in appArgs.Where(a => a != RelaunchedArg))
            startInfo.ArgumentList.Add(arg);
        if (marker)
            startInfo.ArgumentList.Add(RelaunchedArg);
        return startInfo;
    }

    /// <summary>
    /// Start line for another instance of the running app on the runtime this process is on: the native one when that
    /// is what runs, else the muxer on the current major. A re-executed tool's child processes must follow it.
    /// </summary>
    public static ProcessStartInfo SiblingStartInfo(string appDllPath, IEnumerable<string> appArgs)
    {
        var current = Environment.Version.Major;
        return current == ReadNativeMajor(appDllPath)
            ? NativeStartInfo(appDllPath, appArgs)
            : RelaunchStartInfo(appDllPath, current, appArgs, marker: false);
    }

    /// <summary>Start line for the app on its own major: its apphost when present, else <c>dotnet app.dll</c>.</summary>
    public static ProcessStartInfo NativeStartInfo(string appDllPath, IEnumerable<string> appArgs)
    {
        var fullPath = Path.GetFullPath(appDllPath);
        var apphost = Path.ChangeExtension(fullPath, OperatingSystem.IsWindows() ? ".exe" : null);
        ProcessStartInfo startInfo;
        if (File.Exists(apphost))
        {
            startInfo = new ProcessStartInfo(apphost) { UseShellExecute = false };
        }
        else
        {
            startInfo = new ProcessStartInfo(DotNetInstall.Detect()?.MuxerPath ?? "dotnet") { UseShellExecute = false };
            startInfo.ArgumentList.Add(fullPath);
        }
        foreach (var arg in appArgs.Where(a => a != RelaunchedArg))
            startInfo.ArgumentList.Add(arg);
        return startInfo;
    }

    /// <summary>Short stable hash of a directory path (case-folded where the file system is), for per-install file and mutex names.</summary>
    public static string PathHash(string path)
    {
        var normalized = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            normalized = normalized.ToLowerInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))[..8].ToLowerInvariant();
    }

    static IEnumerable<string> ListProjects(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        switch (Path.GetExtension(fullPath).ToLowerInvariant())
        {
            case ".csproj":
                return [fullPath];
            case ".sln":
                return ProjectsFromSln(fullPath, directory);
            case ".slnx":
                return ProjectsFromSlnx(fullPath, directory);
            case ".slnf":
                return ProjectsFromSlnf(fullPath, directory);
            case ".sdpkg":
                var sibling = Path.ChangeExtension(fullPath, ".csproj");
                return File.Exists(sibling) ? [sibling] : [];
            default:
                return [];
        }
    }

    static string Resolve(string directory, string relative)
        => Path.GetFullPath(Path.Combine(directory, relative.Replace('\\', Path.DirectorySeparatorChar)));

    static IEnumerable<string> ProjectsFromSln(string sln, string directory)
        => Regex.Matches(File.ReadAllText(sln), @"^Project\(""\{[^}]*\}""\)\s*=\s*""[^""]*"",\s*""([^""]+\.csproj)""", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Select(m => Resolve(directory, m.Groups[1].Value))
            .ToList();

    static IEnumerable<string> ProjectsFromSlnx(string slnx, string directory)
    {
        var projects = new List<string>();
        using var reader = XmlReader.Create(slnx, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Project"
                && reader.GetAttribute("Path") is { } project && project.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                projects.Add(Resolve(directory, project));
        }
        return projects;
    }

    static IEnumerable<string> ProjectsFromSlnf(string slnf, string directory)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(slnf));
        if (!document.RootElement.TryGetProperty("solution", out var solution) || !solution.TryGetProperty("projects", out var projects))
            return [];
        // Filter entries are relative to the solution's directory.
        var solutionDirectory = solution.TryGetProperty("path", out var solutionPath) && solutionPath.GetString() is { } relative
            ? Path.GetDirectoryName(Resolve(directory, relative))!
            : directory;
        return projects.EnumerateArray()
            .Select(p => p.GetString())
            .Where(p => p != null && p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(p => Resolve(solutionDirectory, p!))
            .ToList();
    }

    // A multi-targeting project counts by its first framework: that is the build the editor loads.
    static int? ProjectMajor(string csproj, string text)
    {
        // The restored assets file has the evaluated frameworks (properties, imports), unless the csproj was edited after
        // that restore: then its own TargetFramework(s) is newer, as right after moving a project to a newer .NET.
        var assets = Path.Combine(Path.GetDirectoryName(csproj)!, "obj", "project.assets.json");
        var restored = File.Exists(assets);
        if (restored && File.GetLastWriteTimeUtc(csproj) > File.GetLastWriteTimeUtc(assets) && TextMajor(text) is { } edited)
            return edited;
        if (restored)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(assets));
            if (document.RootElement.TryGetProperty("project", out var project) && project.TryGetProperty("frameworks", out var frameworks))
            {
                foreach (var framework in frameworks.EnumerateObject())
                {
                    if (ParseMajor(framework.Name) is { } major)
                        return major;
                }
            }
        }
        return TextMajor(text);
    }

    // The first TargetFramework(s) entry written literally in a csproj.
    static int? TextMajor(string text)
    {
        foreach (Match match in Regex.Matches(text, @"<TargetFrameworks?>([^<]*)</TargetFrameworks?>", RegexOptions.IgnoreCase))
        {
            foreach (var tfm in match.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (ParseMajor(tfm) is { } major)
                    return major;
            }
        }
        return null;
    }

    /// <summary>
    /// Whether the app at <paramref name="appDllPath"/> understands <see cref="FrameworkArg"/> and <see cref="RelaunchedArg"/>:
    /// its assembly references this selector. An older editor would take those options for a session path.
    /// </summary>
    public static bool SupportsHostSelection(string appDllPath)
    {
        try
        {
            using var stream = File.OpenRead(appDllPath);
            using var pe = new PEReader(stream);
            var metadata = pe.GetMetadataReader();
            foreach (var handle in metadata.TypeReferences)
            {
                if (metadata.GetString(metadata.GetTypeReference(handle).Name) == nameof(DotNetHostSelector))
                    return true;
            }
            return false;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            return false;
        }
    }

    static JsonObject? LoadRuntimeConfig(string appDllPath)
    {
        var path = Path.ChangeExtension(appDllPath, ".runtimeconfig.json");
        if (!File.Exists(path))
            return null;
        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The frameworks array, folding the single-framework form into it.</summary>
    static JsonArray FrameworksOf(JsonObject config)
    {
        var options = config["runtimeOptions"] as JsonObject ?? throw new InvalidDataException("runtimeconfig.json has no runtimeOptions.");
        if (options["frameworks"] is JsonArray array)
            return array;
        array = new JsonArray();
        if (options["framework"] is JsonObject single)
        {
            options.Remove("framework");
            array.Add(single);
        }
        options["frameworks"] = array;
        return array;
    }
}

/// <summary>A dotnet root: its muxer, shared frameworks and SDKs.</summary>
public sealed class DotNetInstall
{
    public string Root { get; }
    public string MuxerPath { get; }
    /// <summary>Installed versions per shared framework name (Microsoft.NETCore.App, Microsoft.WindowsDesktop.App, …).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<DotNetVersion>> Runtimes { get; }
    public IReadOnlyList<DotNetVersion> Sdks { get; }

    public DotNetInstall(string root, string muxerPath, IReadOnlyDictionary<string, IReadOnlyList<DotNetVersion>> runtimes, IReadOnlyList<DotNetVersion> sdks)
    {
        Root = root;
        MuxerPath = muxerPath;
        Runtimes = runtimes;
        Sdks = sdks;
    }

    static string MuxerName => OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

    /// <summary>The install this process runs from, else DOTNET_ROOT, else dotnet on PATH, else the machine default; null when none exists.</summary>
    public static DotNetInstall? Detect()
    {
        foreach (var candidate in CandidateRoots())
        {
            if (string.IsNullOrEmpty(candidate))
                continue;
            var muxer = Path.Combine(candidate, MuxerName);
            if (!File.Exists(muxer))
                continue;
            // A PATH entry is often a symlink into the real root; a dangling one is skipped.
            string root;
            try
            {
                root = Path.GetDirectoryName(new FileInfo(muxer).ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? muxer)!;
            }
            catch (IOException)
            {
                continue;
            }
            if (Directory.Exists(Path.Combine(root, "shared")))
                return FromRoot(root);
        }
        return null;
    }

    public static DotNetInstall FromRoot(string root)
    {
        var runtimes = new Dictionary<string, IReadOnlyList<DotNetVersion>>(StringComparer.OrdinalIgnoreCase);
        var shared = Path.Combine(root, "shared");
        if (Directory.Exists(shared))
        {
            foreach (var frameworkDirectory in Directory.GetDirectories(shared))
                runtimes[Path.GetFileName(frameworkDirectory)] = Versions(frameworkDirectory);
        }
        var sdk = Path.Combine(root, "sdk");
        return new DotNetInstall(root, Path.Combine(root, MuxerName), runtimes, Directory.Exists(sdk) ? Versions(sdk) : []);

        static List<DotNetVersion> Versions(string directory)
            => Directory.GetDirectories(directory)
                .Select(d => DotNetVersion.TryParse(Path.GetFileName(d), out var version) ? version : default)
                .Where(v => v.Major > 0)
                .OrderBy(v => v)
                .ToList();
    }

    static IEnumerable<string?> CandidateRoots()
    {
        // <root>/shared/Microsoft.NETCore.App/<version>/Microsoft.NETCore.App.deps.json
        if (AppContext.GetData("FX_DEPS_FILE") is string depsFile && depsFile.Length > 0)
            yield return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(depsFile))));
        yield return Environment.GetEnvironmentVariable("DOTNET_ROOT_" + RuntimeInformation.ProcessArchitecture.ToString().ToUpperInvariant());
        yield return Environment.GetEnvironmentVariable("DOTNET_ROOT");
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            yield return directory;
        if (OperatingSystem.IsWindows())
        {
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return "/usr/local/share/dotnet";
        }
        else
        {
            yield return "/usr/share/dotnet";
            yield return "/usr/lib/dotnet";
            yield return "/usr/lib64/dotnet";
        }
    }

    /// <summary>Newest installed version of <paramref name="framework"/> within <paramref name="major"/>, or null.</summary>
    public DotNetVersion? Highest(string framework, int major)
    {
        if (!Runtimes.TryGetValue(framework, out var versions))
            return null;
        var matching = versions.Where(v => v.Major == major).ToList();
        return matching.Count > 0 ? matching.Max() : null;
    }

    public bool HasSdk(int major) => Sdks.Any(s => s.Major == major);

    /// <summary>Majors with a Microsoft.NETCore.App runtime, ascending.</summary>
    public IEnumerable<int> Majors()
        => Runtimes.TryGetValue("Microsoft.NETCore.App", out var versions) ? versions.Select(v => v.Major).Distinct().OrderBy(m => m) : [];
}

/// <summary>A runtime or SDK folder version (<c>11.0.0-rc.1.25451.107</c>); a release orders above any prerelease of the same number.</summary>
public readonly record struct DotNetVersion(int Major, int Minor, int Patch, string Prerelease, string Text) : IComparable<DotNetVersion>
{
    public static bool TryParse(string? text, out DotNetVersion version)
    {
        version = default;
        if (string.IsNullOrEmpty(text))
            return false;
        var dash = text.IndexOf('-');
        var numbers = (dash < 0 ? text : text[..dash]).Split('.');
        if (numbers.Length is < 2 or > 3 || !int.TryParse(numbers[0], out var major) || !int.TryParse(numbers[1], out var minor))
            return false;
        var patch = 0;
        if (numbers.Length == 3 && !int.TryParse(numbers[2], out patch))
            return false;
        version = new DotNetVersion(major, minor, patch, dash < 0 ? "" : text[(dash + 1)..], text);
        return true;
    }

    public int CompareTo(DotNetVersion other)
    {
        var result = Major.CompareTo(other.Major);
        if (result == 0) result = Minor.CompareTo(other.Minor);
        if (result == 0) result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;
        if (Prerelease.Length == 0 || other.Prerelease.Length == 0)
            return other.Prerelease.Length.CompareTo(Prerelease.Length);
        return string.CompareOrdinal(Prerelease, other.Prerelease);
    }

    public override string ToString() => Text;
}
