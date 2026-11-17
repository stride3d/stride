// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.CommandLine;
using Stride.Core;
using Stride.Cli.Core;

// Shared version resolution for the commands that use an installed version (studio, asset, new, upgrade).
internal static class CliVersion
{
    // The options that choose the version of a command among the installed versions, with the meaning they have for sdk
    // install: --version takes an exact version or a Major.Minor line, and the other options widen what a line or the
    // newest version may be.
    public sealed class Options(string versionDescription)
    {
        public const string LineHelp = "a major.minor line (e.g. 4.5) takes its newest installed release";

        public Option<string?> Version { get; } = new("--version") { Description = versionDescription };
        public Option<bool> Prerelease { get; } = new("--prerelease") { Description = "Include the installed prerelease (alpha, beta, preview, rc) versions when choosing the newest version or the newest of a line." };
        public Option<bool> Nightly { get; } = new("--nightly") { Description = "Include the installed nightly builds when choosing the newest version or the newest of a line." };
        public Option<string?> Tag { get; } = new("--tag") { Description = "Take only the installed builds of this tag when choosing the newest version or the newest of a line: a studio's custom builds (mystudio for 4.5.3-mystudio.32), or the local builds of a Stride checkout (dev8 for 4.5.5-dev8)." };

        public void AddTo(Command command)
        {
            command.Options.Add(Version);
            command.Options.Add(Prerelease);
            command.Options.Add(Nightly);
            command.Options.Add(Tag);
        }

        public VersionFilter Filter(ParseResult parseResult)
            => new(parseResult.GetValue(Prerelease), parseResult.GetValue(Nightly), parseResult.GetValue(Tag));

        // Whether the parse result names a version, by --version or by the options.
        public bool IsGiven(ParseResult parseResult)
            => !string.IsNullOrEmpty(parseResult.GetValue(Version)) || Filter(parseResult) != default;

        // Resolves the version these options name in the parse result (ResolveOrReport).
        public PackageVersion? ResolveOrReport(StrideVersionManager manager, ParseResult parseResult)
            => CliVersion.ResolveOrReport(manager, parseResult.GetValue(Version), Filter(parseResult));
    }

    // Resolves the Stride version for the current directory (restoring the project first). Prints any error and
    // returns null on failure.
    public static PackageVersion? ResolveOrReport(StrideVersionManager manager, string? versionSpec, VersionFilter filter = default)
    {
        try
        {
            var resolved = manager.ResolveVersion(versionSpec, Environment.CurrentDirectory, filter);
            if (resolved is null)
                Console.Error.WriteLine("No Stride version is installed.");
            return resolved;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return null;
        }
    }
}
