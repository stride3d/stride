// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Versioning;
using ServiceWire.NamedPipes;
using Stride.Core;                  // LoaderToolLocator
using Stride.Core.Assets;           // RestoreHelper
using Stride.VisualStudio.Commands; // IStrideCommands

namespace Stride.Cli.Legacy;

/// <summary>
/// Spawns the released, version-matched <c>Stride.VisualStudio.Commands</c> and calls its
/// <c>GenerateShaderKeys</c> over ServiceWire, to regenerate the C# from <c>.sdsl</c>/<c>.sdfx</c> files the way
/// the old VS Custom Tool did. Kept for Stride 4.0-4.3 (4.4+ generates it at build via a source generator).
/// </summary>
internal sealed class LegacyShaderCodeGenerator : IDisposable
{
    private const string CommandsPackageId = "Stride.VisualStudio.Commands";

    private readonly Process process;
    private readonly NpClient<IStrideCommands> client;

    private LegacyShaderCodeGenerator(Process process, NpClient<IStrideCommands> client)
    {
        this.process = process;
        this.client = client;
    }

    /// <summary>Restores the version-matched Commands, spawns it, and connects. Caller owns the returned instance.</summary>
    public static LegacyShaderCodeGenerator Start(PackageVersion version)
    {
        var executable = LocateCommandsExecutable(version)
            ?? throw new InvalidOperationException($"Could not restore {CommandsPackageId} {version} (needed to regenerate shader code). Check your NuGet sources.");

        var address = "Stride/StrideCliShaders/" + Guid.NewGuid();
        var startInfo = new ProcessStartInfo(executable, $"--pipe=\"{address}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
        };
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {CommandsPackageId} {version}.");

        // Stride 4.1 spoke ServiceWire 5.3.4 (BinaryFormatter, no compression); 4.2+ uses the modern default.
        var legacy = version.Version < new Version(4, 2);

        // The server needs a moment to open its named pipe; retry the connection briefly.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var endpoint = new NpEndPoint(address + "/IStrideCommands");
                var client = legacy
                    ? new NpClient<IStrideCommands>(endpoint, new LegacyBinaryFormatterSerializer(), new LegacyDoNothingCompressor())
                    : new NpClient<IStrideCommands>(endpoint);
                return new LegacyShaderCodeGenerator(process, client);
            }
            catch when (attempt < 30 && !process.HasExited)
            {
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>Regenerates the C# for one shader file; returns the file bytes, or null/empty if nothing was produced.</summary>
    public byte[]? Generate(string shaderFileName, string shaderFileContent)
        => client.Proxy.GenerateShaderKeys(shaderFileName, shaderFileContent);

    // Restores Commands (with its dependency closure) and maps the restored assembly to its runnable executable,
    // trying the frameworks a released Commands may ship as, newest first.
    private static string? LocateCommandsExecutable(PackageVersion version)
    {
        var range = new VersionRange(new NuGetVersion(version.Version, version.SpecialVersion));
        foreach (var framework in new[] { "net10.0-windows7.0", "net8.0-windows7.0", "net6.0-windows7.0", "net472" })
        {
            var (_, result) = RestoreHelper.Restore(NullLogger.Instance, NuGetFramework.ParseFolder(framework), "win", CommandsPackageId, range);
            if (!result.Success)
                continue;

            var commandsAssembly = RestoreHelper.ListAssemblies(result.LockFile)
                .FirstOrDefault(assembly => Path.GetFileNameWithoutExtension(assembly) == CommandsPackageId);
            if (commandsAssembly is not null)
                return LoaderToolLocator.GetExecutable(commandsAssembly);
        }

        return null;
    }

    public void Dispose()
    {
        try { client.Dispose(); } catch { /* best effort */ }
        try { if (!process.HasExited) process.Kill(); } catch { /* best effort */ }
        process.Dispose();
    }
}
