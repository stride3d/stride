// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO.Compression;
using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Package lookups of <see cref="NugetStore"/>, with a local-folder source and a global packages folder of their own.
/// </summary>
public sealed class TestNugetStore : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "stride-nugetstore-" + Guid.NewGuid().ToString("N"));

    private string Source => Path.Combine(root, "source");

    private string GlobalFolder => Path.Combine(root, "global");

    public TestNugetStore()
    {
        Directory.CreateDirectory(Source);
        Directory.CreateDirectory(GlobalFolder);
    }

    public void Dispose()
    {
        ReleaseMirroredNupkgs();
        Directory.Delete(root, recursive: true);
    }

    // The stream NuGet returns for a mirrored nupkg is left to its finalizer, and locks the installed file until then
    private static void ReleaseMirroredNupkgs()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private NugetStore CreateStore(string? sourceValue = null)
    {
        File.WriteAllText(Path.Combine(root, "NuGet.Config"), $"""
            <configuration>
              <packageSources>
                <clear />
                <add key="test" value="{sourceValue ?? Source}" />
              </packageSources>
            </configuration>
            """);

        var previousPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        try
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", GlobalFolder);
            var settings = NuGet.Configuration.Settings.LoadSpecificSettings(root, "NuGet.Config");
            var store = new NugetStore(null, settings);

            // Nothing may reach the real NuGet cache or sources
            Assert.Equal(GlobalFolder, Path.TrimEndingDirectorySeparator(store.InstallPath));
            var source = Assert.Single(new NuGet.Configuration.PackageSourceProvider(settings).LoadPackageSources());
            Assert.Equal(sourceValue ?? Source, source.Source);
            return store;
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", previousPackages);
        }
    }

    private string WriteNupkg(string fileName, string id, string version, string content = "first")
    {
        var path = Path.Combine(Source, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.Delete(path);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        using (var nuspec = new StreamWriter(archive.CreateEntry($"{id}.nuspec").Open()))
        {
            nuspec.Write($"""
                <?xml version="1.0" encoding="utf-8"?>
                <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
                  <metadata>
                    <id>{id}</id>
                    <version>{version}</version>
                    <authors>Stride</authors>
                    <description>Test</description>
                  </metadata>
                </package>
                """);
        }
        using (var file = new StreamWriter(archive.CreateEntry("content.txt").Open()))
            file.Write(content);
        return path;
    }

    private static NugetLocalPackage? FindExact(NugetStore store, string id, string version)
        => store.FindLocalPackage(id, new PackageVersionRange(new PackageVersion(version)));

    private static string ReadContent(NugetLocalPackage package) => File.ReadAllText(Path.Combine(package.Path, "content.txt"));

    private bool IsMirrored(string id, string version) => Directory.Exists(Path.Combine(GlobalFolder, id.ToLowerInvariant(), version.ToLowerInvariant()));

    private static readonly PackageVersionRange AnyVersion = new(new PackageVersion("0.1.0"), true);

    [Fact]
    public void ExactVersion_GivesTheSamePackageAsTheListing()
    {
        string[] versions = ["1.2.3", "1.2.3.4", "1.2.3-beta1", "4.4.0-beta7-dev4", "4.5.0-dev", "4.5.0-nightly.20261105.1"];
        foreach (var version in versions)
            WriteNupkg($"Foo.{version}.nupkg", "Foo", version);

        var listing = CreateStore().GetLocalPackages("Foo", AnyVersion).ToList();
        Assert.Equal(versions.Length, listing.Count);

        var store = CreateStore();
        foreach (var version in versions)
        {
            var expected = Assert.Single(listing, x => x.Version == new PackageVersion(version));
            var found = FindExact(store, "foo", version);

            Assert.NotNull(found);
            Assert.Equal(expected.Version, found.Version);
            Assert.Equal(expected.Path, found.Path);
            Assert.Equal(expected.NupkgPath, found.NupkgPath);
        }
        Assert.Equal("Foo", FindExact(store, "Foo", "1.2.3")!.Id);
    }

    [Fact]
    public void ExactVersion_IsNullWhenNotInstalled()
    {
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        var store = CreateStore();

        Assert.Null(FindExact(store, "Foo", "2.0.0"));
        Assert.Null(FindExact(store, "Bar", "1.0.0"));
        // Only what the lookup could use is mirrored
        Assert.False(IsMirrored("Foo", "1.0.0"));
    }

    [Fact]
    public void ExactVersion_IsNullForAFolderWithoutItsHash()
    {
        var nupkg = WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        Assert.NotNull(FindExact(CreateStore(), "Foo", "1.0.0"));

        File.Delete(nupkg);
        File.Delete(Path.Combine(GlobalFolder, "foo", "1.0.0", "foo.1.0.0.nupkg.sha512"));

        Assert.Null(FindExact(CreateStore(), "Foo", "1.0.0"));
    }

    [Fact]
    public void ExactVersion_KeepsThePrereleaseFilter()
    {
        WriteNupkg("Foo.1.0.0-beta1.nupkg", "Foo", "1.0.0-beta1");
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        var store = CreateStore();

        Assert.Null(store.FindLocalPackage("Foo", new PackageVersionRange(new PackageVersion("1.0.0-beta1")), allowPrereleaseVersions: false));
        Assert.NotNull(store.FindLocalPackage("Foo", new PackageVersionRange(new PackageVersion("1.0.0")), allowPrereleaseVersions: false));
    }

    [Fact]
    public void ExactVersionFromAConstraint_IsFound()
    {
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        WriteNupkg("Foo.2.0.0.nupkg", "Foo", "2.0.0");
        var constraints = new ConstraintProvider();
        constraints.AddConstraint("Foo", new PackageVersionRange(new PackageVersion("1.0.0")));

        var found = CreateStore().FindLocalPackage("Foo", (PackageVersionRange?)null, constraints);

        Assert.Equal(new PackageVersion("1.0.0"), found?.Version);
        Assert.False(IsMirrored("Foo", "2.0.0"));
    }

    [Fact]
    public void OtherRanges_GoThroughTheListing()
    {
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        var store = CreateStore();
        var version = new PackageVersion("1.0.0");

        Assert.NotNull(store.FindLocalPackage("Foo", new PackageVersionRange(version, true)));
        Assert.Null(store.FindLocalPackage("Foo", new PackageVersionRange(version, false, version, true)));
        Assert.NotNull(store.FindLocalPackage("Foo", new PackageVersionRange(version, true, new PackageVersion("2.0.0"), true)));
    }

    [Fact]
    public void GetLocalPackages_ReturnsEveryVersionWhateverTheRange()
    {
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        WriteNupkg("Foo.2.0.0.nupkg", "Foo", "2.0.0");
        var store = CreateStore();
        store.GetLocalPackages("Foo", AnyVersion).ToList();

        var packages = store.GetLocalPackages("Foo", new PackageVersionRange(new PackageVersion("1.0.0"))).ToList();

        Assert.Equal(2, packages.Count);
    }

    [Fact]
    public void PrefixId_IsNotAVersion()
    {
        WriteNupkg("Stride.Core.4.4.1.nupkg", "Stride.Core", "4.4.1");
        WriteNupkg("Stride.Core.Assets.4.4.1.nupkg", "Stride.Core.Assets", "4.4.1");
        var store = CreateStore();

        var packages = store.GetLocalPackages("Stride.Core", AnyVersion).ToList();

        Assert.Equal(new PackageVersion("4.4.1"), Assert.Single(packages).Version);
        Assert.Equal(["4.4.1"], Directory.GetDirectories(Path.Combine(GlobalFolder, "stride.core")).Select(Path.GetFileName));
        Assert.False(Directory.Exists(Path.Combine(GlobalFolder, "stride.core.assets")));
    }

    [Fact]
    public void IdEndingInANumber_IsNotMirroredAsTheShorterId()
    {
        // Reads as Foo 1.2.0 and as Foo.1 2.0.0, and is Foo.1 2.0.0
        WriteNupkg("Foo.1.2.0.0.nupkg", "Foo.1", "2.0.0");
        var store = CreateStore();

        Assert.Null(FindExact(store, "Foo", "1.2.0"));
        Assert.False(IsMirrored("Foo", "1.2.0"));

        var found = FindExact(store, "Foo.1", "2.0.0");
        Assert.Equal(Path.Combine(GlobalFolder, "foo.1", "2.0.0"), found?.Path);
    }

    [Fact]
    public void IdEndingInANumber_DoesNotReplaceAnInstalledPackage()
    {
        WriteNupkg("Foo.1.2.0.nupkg", "Foo", "1.2.0", "the real Foo");
        Assert.NotNull(FindExact(CreateStore(), "Foo", "1.2.0"));

        // A newer file, whose name still reads as Foo 1.2.0
        var nupkg = WriteNupkg("Foo.1.2.0.nupkg", "Foo.1", "2.0.0", "Foo.1");
        File.SetLastWriteTimeUtc(nupkg, DateTime.UtcNow.AddHours(1));

        var found = FindExact(CreateStore(), "Foo", "1.2.0");

        Assert.NotNull(found);
        Assert.Equal("the real Foo", ReadContent(found));
    }

    [Fact]
    public void NonStandardNames_AreSkipped()
    {
        WriteNupkg("Foo.custom.nupkg", "Foo", "1.0.0");
        WriteNupkg("Foo-1.0.0.nupkg", "Foo", "1.0.0");
        WriteNupkg("Foo.1.0.0.symbols.nupkg", "Foo", "1.0.0");
        WriteNupkg("Foo.2.0.0-beta.symbols.nupkg", "Foo", "2.0.0-beta");
        var store = CreateStore();

        Assert.Empty(store.GetLocalPackages("Foo", AnyVersion));
        Assert.Null(FindExact(store, "Foo", "1.0.0"));
        Assert.Null(FindExact(store, "Foo", "2.0.0-beta"));
    }

    [Fact]
    public void RenamedNupkg_IsNotMirrored()
    {
        WriteNupkg("Foo.2.0.0.nupkg", "Foo", "1.0.0");
        WriteNupkg("Foo.3.0.0.nupkg", "Bar", "3.0.0");
        var store = CreateStore();

        Assert.Null(FindExact(store, "Foo", "2.0.0"));
        Assert.Null(FindExact(store, "Foo", "1.0.0"));
        Assert.Null(FindExact(store, "Foo", "3.0.0"));
        Assert.Null(FindExact(store, "Bar", "3.0.0"));
        Assert.False(Directory.Exists(Path.Combine(GlobalFolder, "foo")));
    }

    [Fact]
    public void OtherVersions_AreNotOpened()
    {
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        File.WriteAllText(Path.Combine(Source, "Foo.9.0.0.nupkg"), "not a nupkg");

        Assert.NotNull(FindExact(CreateStore(), "Foo", "1.0.0"));
    }

    [Fact]
    public void CorruptNupkg_IsSkipped()
    {
        File.WriteAllText(Path.Combine(Source, "Foo.1.0.0.nupkg"), "not a nupkg");

        Assert.Null(FindExact(CreateStore(), "Foo", "1.0.0"));
        Assert.False(IsMirrored("Foo", "1.0.0"));
    }

    [Fact]
    public void NewerNupkg_ReplacesTheInstalledOne_AndAnOlderOneDoesNot()
    {
        WriteNupkg("Foo.1.0.0-dev.nupkg", "Foo", "1.0.0-dev");
        Assert.NotNull(FindExact(CreateStore(), "Foo", "1.0.0-dev"));
        ReleaseMirroredNupkgs();

        var older = WriteNupkg("Foo.1.0.0-dev.nupkg", "Foo", "1.0.0-dev", "older");
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddHours(-1));
        Assert.Equal("first", ReadContent(FindExact(CreateStore(), "Foo", "1.0.0-dev")!));

        var newer = WriteNupkg("Foo.1.0.0-dev.nupkg", "Foo", "1.0.0-dev", "newer");
        File.SetLastWriteTimeUtc(newer, DateTime.UtcNow.AddHours(1));
        Assert.Equal("newer", ReadContent(FindExact(CreateStore(), "Foo", "1.0.0-dev")!));
    }

    [Fact]
    public void NupkgAddedAfterALookup_IsFoundByTheNextOne()
    {
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");
        var store = CreateStore();
        Assert.NotNull(FindExact(store, "Foo", "1.0.0"));

        WriteNupkg("Foo.2.0.0.nupkg", "Foo", "2.0.0");

        Assert.NotNull(FindExact(store, "Foo", "2.0.0"));
    }

    [Fact]
    public void UnusualNames_AreFound()
    {
        WriteNupkg("foo.1.0.0.nupkg", "Foo", "1.0.0");
        WriteNupkg("Foo.2.0.nupkg", "Foo", "2.0.0");
        WriteNupkg("Foo.3.0.0.0.nupkg", "Foo", "3.0.0");
        WriteNupkg("Foo.4.0-rc.1.nupkg", "Foo", "4.0.0-rc.1");
        WriteNupkg(Path.Combine("sub", "Foo.5.0.0.nupkg"), "Foo", "5.0.0");
        var store = CreateStore();

        foreach (var version in new[] { "1.0.0", "2.0.0", "3.0.0", "4.0.0-rc.1", "5.0.0" })
            Assert.NotNull(FindExact(store, "Foo", version));
    }

    [Fact]
    public void SourceGivenAsAFileUri_IsRead()
    {
        WriteNupkg("Foo.1.0.0.nupkg", "Foo", "1.0.0");

        Assert.NotNull(FindExact(CreateStore(new Uri(Source).AbsoluteUri), "Foo", "1.0.0"));
    }
}
