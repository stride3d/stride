// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO.Compression;
using System.Text;

namespace Stride.Core.Assets.Tests.Helpers;

/// <summary>
/// A NuGet setup of its own in a temporary folder, deleted when disposed: a local feed of Stride packages written by the
/// test (a <c>.nuspec</c>, a packed <c>stride/&lt;id&gt;.sdpkg</c> and optional assemblies), a global packages folder, and a
/// <c>nuget.config</c> in <see cref="Root"/> that uses that folder and no other source. A restore with <see cref="Root"/> as
/// its settings root touches nothing of the user's.
/// </summary>
internal sealed class LocalPackageFeed : IDisposable
{
    public LocalPackageFeed()
    {
        Root = Path.Combine(Path.GetTempPath(), "stride-feed-tests", Guid.NewGuid().ToString("N"));
        FeedDirectory = Path.Combine(Root, "feed");
        PackagesDirectory = Path.Combine(Root, "packages");
        Directory.CreateDirectory(FeedDirectory);
        Directory.CreateDirectory(PackagesDirectory);
        File.WriteAllText(Path.Combine(Root, "nuget.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <config>
                <add key="globalPackagesFolder" value="{PackagesDirectory}" />
              </config>
              <packageSources>
                <clear />
              </packageSources>
              <fallbackPackageFolders>
                <clear />
              </fallbackPackageFolders>
            </configuration>
            """);
    }

    /// <summary>
    /// The folder of the <c>nuget.config</c>: the settings root of a restore, or the folder of a test session.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// The local feed the packages are written to (not a configured source: a restore names it).
    /// </summary>
    public string FeedDirectory { get; }

    /// <summary>
    /// The global packages folder restores extract into.
    /// </summary>
    public string PackagesDirectory { get; }

    /// <summary>
    /// Writes a Stride package into the feed (or into <paramref name="directory"/>).
    /// </summary>
    /// <param name="id">The package id.</param>
    /// <param name="version">The package version.</param>
    /// <param name="kind">What the package carries.</param>
    /// <param name="toolkit">The toolkit of an Editor package's views, or null.</param>
    /// <param name="companions">The companions the package declares.</param>
    /// <param name="dependencies">The NuGet dependencies of the package, by id and exact version.</param>
    /// <param name="assemblies">Assemblies the package carries, by file name, listed as its host assemblies.</param>
    /// <param name="directory">Where to write it, or null for the feed.</param>
    /// <param name="title">The <c>.nuspec</c> title, or null.</param>
    /// <returns>The path of the <c>.nupkg</c> file.</returns>
    public string AddPackage(string id, string version, PackageKind kind, string? toolkit = null, IEnumerable<CompanionPackage>? companions = null,
        IEnumerable<(string Id, string Version)>? dependencies = null, IReadOnlyDictionary<string, byte[]>? assemblies = null, string? directory = null, string? title = null)
    {
        directory ??= FeedDirectory;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{id}.{version}.nupkg");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        var dependencyElements = string.Join("", (dependencies ?? []).Select(x => $"""<dependency id="{x.Id}" version="[{x.Version}]" />"""));
        Write(archive, $"{id}.nuspec", $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{id}</id>
                <version>{version}</version>
                {(title is not null ? $"<title>{title}</title>" : "")}
                <authors>Stride tests</authors>
                <description>A package written by a test.</description>
                <dependencies>
                  <group targetFramework="net10.0">{dependencyElements}</group>
                </dependencies>
              </metadata>
            </package>
            """);

        var sdpkg = new StringBuilder();
        sdpkg.AppendLine("!Package");
        sdpkg.AppendLine("SerializedVersion: {Assets: 3.1.0.0}");
        sdpkg.AppendLine("Meta:");
        sdpkg.AppendLine($"    Name: {id}");
        sdpkg.AppendLine($"    Version: {version}");
        sdpkg.AppendLine("    Authors: []");
        sdpkg.AppendLine("    Owners: []");
        sdpkg.AppendLine("    Dependencies: null");
        sdpkg.AppendLine($"Kind: {kind}");
        if (assemblies is { Count: > 0 })
        {
            sdpkg.AppendLine("HostAssemblies:");
            foreach (var name in assemblies.Keys)
            {
                sdpkg.AppendLine("    -   TargetFramework: net10.0");
                sdpkg.AppendLine($"        Path: !file ../lib/net10.0/{name}");
            }
        }
        if (companions?.ToList() is { Count: > 0 } list)
        {
            sdpkg.AppendLine("CompanionPackages:");
            foreach (var companion in list)
            {
                sdpkg.AppendLine($"    -   Name: {companion.Name}");
                sdpkg.AppendLine($"        Version: {companion.Version}");
                sdpkg.AppendLine($"        Kind: {companion.Kind}");
                if (companion.Toolkit is not null)
                    sdpkg.AppendLine($"        Toolkit: {companion.Toolkit}");
            }
        }
        if (toolkit is not null)
            sdpkg.AppendLine($"Toolkit: {toolkit}");
        sdpkg.AppendLine($"AssetNamespace: {id}");
        Write(archive, $"stride/{id}.sdpkg", sdpkg.ToString());

        foreach (var (name, content) in assemblies ?? new Dictionary<string, byte[]>())
        {
            var entry = archive.CreateEntry($"lib/net10.0/{name}");
            using var stream = entry.Open();
            stream.Write(content);
        }
        if (assemblies is not { Count: > 0 })
            Write(archive, "lib/net10.0/_._", "");
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // An assembly of the feed may still be mapped by the test process (Windows): the temp folder keeps it
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void Write(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
