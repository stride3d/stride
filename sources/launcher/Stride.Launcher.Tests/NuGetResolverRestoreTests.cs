// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO.Compression;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Versioning;
using Stride.Core.Assets;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class NuGetResolverRestoreTests
{
    [Fact]
    public void RebuiltDevPackageRestoresChangedDependencies()
    {
        var root = Path.Combine(Path.GetTempPath(), "stride-resolver-restore-" + Guid.NewGuid().ToString("N"));
        var feed = Path.Combine(root, "feed");
        var packages = Path.Combine(root, "packages");
        var packageId = "Stride.ResolverTest." + Guid.NewGuid().ToString("N");
        const string version = "1.0.0-dev";
        const string framework = "net10.0";
        const string runtime = "win-x64";
        Directory.CreateDirectory(feed);
        try
        {
            new XDocument(new XElement("configuration",
                new XElement("packageSources", new XElement("clear"), new XElement("add", new XAttribute("key", "local"), new XAttribute("value", feed))),
                new XElement("config", new XElement("add", new XAttribute("key", "globalPackagesFolder"), new XAttribute("value", packages)))))
                .Save(Path.Combine(root, "nuget.config"));
            WritePackage(feed, packageId, version, "OldDependency");
            WritePackage(feed, "OldDependency", "1.0.0");
            WritePackage(feed, "NewDependency", "1.0.0");

            var range = new VersionRange(NuGetVersion.Parse(version), true, NuGetVersion.Parse(version), true);
            var (_, first) = RestoreHelper.Restore(NullLogger.Instance, NuGetFramework.ParseFolder(framework), runtime, packageId, range, root);
            Assert.True(first.Success);
            Assert.Contains(first.LockFile.Libraries, library => library.Name == "OldDependency");

            // The installed dev package is replaced, but keeps its ID and version.
            var installedNuspec = Path.Combine(packages, packageId.ToLowerInvariant(), version, packageId.ToLowerInvariant() + ".nuspec");
            File.WriteAllText(installedNuspec, Nuspec(packageId, version, "NewDependency"));

            var (_, second) = RestoreHelper.Restore(NullLogger.Instance, NuGetFramework.ParseFolder(framework), runtime, packageId, range, root);
            Assert.True(second.Success);
            Assert.Contains(second.LockFile.Libraries, library => library.Name == "NewDependency");
            Assert.DoesNotContain(second.LockFile.Libraries, library => library.Name == "OldDependency");
        }
        finally
        {
            NuGetResolverCache.Delete(packageId, version);
            Directory.Delete(root, true);
        }
    }

    private static void WritePackage(string feed, string id, string version, string? dependency = null)
    {
        using var archive = ZipFile.Open(Path.Combine(feed, $"{id}.{version}.nupkg"), ZipArchiveMode.Create);
        using var writer = new StreamWriter(archive.CreateEntry(id + ".nuspec").Open());
        writer.Write(Nuspec(id, version, dependency));
    }

    private static string Nuspec(string id, string version, string? dependency)
        => $"""
           <?xml version="1.0"?>
           <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
             <metadata>
               <id>{id}</id><version>{version}</version><authors>Test</authors><description>Test</description>
               {(dependency is null ? "" : $"<dependencies><dependency id=\"{dependency}\" version=\"1.0.0\" /></dependencies>")}
             </metadata>
           </package>
           """;
}
