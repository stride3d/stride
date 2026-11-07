// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The check of a plugin assembly before it loads: the versions of the editor assemblies it references, read without loading
/// it, against the editor's (<see cref="EditorApiCheck"/>), and what the session loader and the plugin host do with a refusal.
/// </summary>
public sealed class TestEditorApiCheck : IDisposable
{
    private static readonly Version Editor = new(4, 5);

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.EditorApiCheck." + Guid.NewGuid().ToString("N"));

    public TestEditorApiCheck() => Directory.CreateDirectory(directory);

    public void Dispose()
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { /* a file loaded by path stays locked */ }
        catch (UnauthorizedAccessException) { /* same, on Windows */ }
    }

    [Fact]
    public void APluginBuiltForANewerMinorIsRefusedWithTheVersionItNeeds()
    {
        var path = WriteAssembly("Newer.Editor", ("Stride.Core.Assets.Editor", new Version(4, 5, 0, 0)), ("Stride.Assets.Editor", new Version(4, 6, 0, 0)));

        var error = new EditorApiCheck(Editor).Check(path, "Newer");

        Assert.NotNull(error);
        Assert.Equal("Newer.Editor", error.AssemblyName);
        Assert.Equal("Newer", error.PackageName);
        Assert.Equal("Stride.Assets.Editor", error.ContractAssembly);
        Assert.Equal(new Version(4, 6, 0, 0), error.ReferencedVersion);
        Assert.Equal(new Version(4, 6), error.RequiredEditorVersion);
        Assert.Equal(new Version(4, 5), error.EditorVersion);
        Assert.Equal("The plugin [Newer.Editor] of the package [Newer] needs Game Studio 4.6: it was built against Stride.Assets.Editor 4.6.0.0, and this Game Studio is 4.5. It is not loaded.", error.Message);
    }

    [Fact]
    public void APluginBuiltForANewerMajorIsRefused()
    {
        var path = WriteAssembly("NewerMajor.Editor", ("Stride.Editor", new Version(5, 0, 0, 0)));

        var error = new EditorApiCheck(Editor).Check(path);

        Assert.NotNull(error);
        Assert.Equal(new Version(5, 0), error.RequiredEditorVersion);
        // The package is named only when it differs from the assembly
        Assert.StartsWith("The plugin [NewerMajor.Editor] needs Game Studio 5.0:", error.Message);
    }

    [Theory]
    [InlineData(4, 5, 0, 0)] // the same version
    [InlineData(4, 4, 0, 0)] // an older minor: loads, the guard contains a missing member
    [InlineData(4, 5, 3, 0)] // a patch difference never counts (Stride keeps major.minor.0.0 anyway)
    [InlineData(3, 9, 0, 0)] // an older major
    public void APluginBuiltForTheSameOrAnOlderEditorLoads(int major, int minor, int build, int revision)
    {
        var path = WriteAssembly("Older.Editor", ("Stride.Assets.Editor.Avalonia", new Version(major, minor, build, revision)));

        Assert.Null(new EditorApiCheck(Editor).Check(path));
    }

    [Fact]
    public void APatchOfTheEditorDoesNotCount()
    {
        var path = WriteAssembly("Same.Editor", ("Stride.Assets.Editor", new Version(4, 5, 0, 0)));

        // Only major and minor of the editor count too
        var check = new EditorApiCheck(new Version(4, 5, 2, 0));
        Assert.Equal(new Version(4, 5), check.EditorVersion);
        Assert.Null(check.Check(path));
    }

    [Fact]
    public void AnAssemblyWithNoEditorReferenceLoads()
    {
        // A runtime or Assets assembly of a package: newer Stride assemblies that are not the editor's do not count
        var path = WriteAssembly("Runtime", ("Stride.Engine", new Version(4, 9, 0, 0)), ("Stride.Core.Assets", new Version(4, 9, 0, 0)));

        Assert.Null(EditorApiCheck.GetReferencedEditorVersion(path, out var contract));
        Assert.Null(contract);
        Assert.Null(new EditorApiCheck(Editor).Check(path));
    }

    [Fact]
    public void TheWpfAssembliesOfTheEditorCountToo()
    {
        var path = WriteAssembly("Wpf.Editor", ("Stride.Core.Presentation.Wpf", new Version(4, 7, 0, 0)), ("Stride.Assets.Editor", new Version(4, 5, 0, 0)));

        Assert.Equal(new Version(4, 7, 0, 0), EditorApiCheck.GetReferencedEditorVersion(path, out var contract));
        Assert.Equal("Stride.Core.Presentation.Wpf", contract);
        Assert.NotNull(new EditorApiCheck(Editor).Check(path));
    }

    [Fact]
    public void AnUnreadableFileIsLoggedAndLeftToTheLoader()
    {
        var path = Path.Combine(directory, "Broken.Editor.dll");
        File.WriteAllText(path, "not an assembly");
        var refused = new List<IncompatiblePluginException>();
        var check = new EditorApiCheck(Editor, refused.Add);
        var log = new LoggerResult();

        Assert.ThrowsAny<BadImageFormatException>(() => check.Check(path));
        // Through the loader's hook: a warning, and the loader goes on as usual (it reports its own error)
        Assert.True(check.CanLoad(new Package { Meta = { Name = "Broken" } }, path, log));
        var message = Assert.Single(log.Messages);
        Assert.Equal(LogMessageType.Warning, message.Type);
        Assert.Contains(path, message.Text);
        Assert.Empty(refused);

        // A missing file too
        Assert.True(check.CanLoad(new Package { Meta = { Name = "Broken" } }, Path.Combine(directory, "Missing.dll"), log));
        Assert.Equal(2, log.Messages.Count);
    }

    [Fact]
    public void TheSessionLoaderLeavesARefusedAssemblyOutAndChecksItOnce()
    {
        var refusedPath = WriteAssembly("Probe.Refused" + Guid.NewGuid().ToString("N"), ("Stride.Assets.Editor", new Version(Editor.Major, Editor.Minor + 1, 0, 0)));
        var loadedPath = WriteAssembly("Probe.Loaded" + Guid.NewGuid().ToString("N"), ("Stride.Assets.Editor", new Version(Editor.Major, Editor.Minor, 0, 0)));
        var refused = new List<IncompatiblePluginException>();
        var check = new CountingCheck(new EditorApiCheck(Editor, refused.Add));
        using var session = new PackageSession();
        var package = AddPackage(session, "Probe", refusedPath, loadedPath);
        var log = new LoggerResult();

        package.UpdateAssemblyReferences(log, new PackageLoadParameters { AssemblyLoadCheck = check });

        // The refused one is listed without an assembly, and is not in the session's container
        var refusedAssembly = package.LoadedAssemblies.Single(x => x.Path == refusedPath);
        Assert.Null(refusedAssembly.Assembly);
        Assert.DoesNotContain(session.AssemblyContainer.LoadedAssemblies, x => x.Path == Path.GetFullPath(refusedPath));
        Assert.NotNull(package.LoadedAssemblies.Single(x => x.Path == loadedPath).Assembly);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.StartsWith("The plugin [Probe.Refused", StringComparison.Ordinal));
        Assert.Equal(refusedPath, Assert.Single(refused).AssemblyPath);

        // A later load (the session's own check this time) does not check it again
        session.AssemblyLoadCheck = check;
        package.UpdateAssemblyReferences(log);
        Assert.Equal(2, check.Calls);
        Assert.Single(refused);
    }

    [Fact]
    public void TheSessionKeepsTheCheckOfItsLoadParameters()
    {
        var check = new CountingCheck(new EditorApiCheck(Editor));
        using var session = new PackageSession();

        session.LoadMissingAssets(new LoggerResult(), [], new PackageLoadParameters { AssemblyLoadCheck = check });
        Assert.Same(check, session.AssemblyLoadCheck);

        // The first one stays
        session.LoadMissingAssets(new LoggerResult(), [], new PackageLoadParameters { AssemblyLoadCheck = new CountingCheck(new EditorApiCheck(Editor)) });
        Assert.Same(check, session.AssemblyLoadCheck);
    }

    [Fact]
    public void TheHostRecordsARefusedPluginAsIncompatible()
    {
        var host = new RecordingEditorPluginHost([]);
        var changes = new List<(AssetsPlugin? Plugin, string Name, PluginState? State)>();
        host.PluginStatusChanged += (_, e) => changes.Add((e.Plugin, e.Name, e.Status?.State));
        var running = typeof(EditorApiCheck).Assembly.GetName().Version!;
        var path = WriteAssembly("Incompatible.Editor", ("Stride.Editor", new Version(running.Major, running.Minor + 1, 0, 0)));
        var log = new LoggerResult();
        try
        {
            // The editors give the host's check to the sessions they open
            Assert.Same(host.AssemblyLoadCheck, ((IAssetsPluginService)host).AssemblyLoadCheck);
            Assert.False(host.AssemblyLoadCheck.CanLoad(new Package { Meta = { Name = "Incompatible" } }, path, log));

            var status = Assert.Single(host.PluginStatuses);
            Assert.Null(status.Plugin);
            Assert.Equal("Incompatible.Editor", status.Name);
            Assert.Equal(PluginState.Incompatible, status.State);
            var error = Assert.Single(status.Errors);
            Assert.Equal(PluginContributions.Load, error.Contribution);
            var incompatible = Assert.IsType<IncompatiblePluginException>(error.Exception);
            Assert.Equal(new Version(running.Major, running.Minor + 1), incompatible.RequiredEditorVersion);
            Assert.Contains($"needs Game Studio {running.Major}.{running.Minor + 1}", error.ToString());
            Assert.Equal([((AssetsPlugin?)null, "Incompatible.Editor", (PluginState?)PluginState.Incompatible)], changes);
            // Logged once, by the check, in the log of the session's load
            Assert.Single(log.Messages, x => x.Type == LogMessageType.Warning);

            // A plugin that joins later is listed before it
            var joining = new TestPluginContributions.PanelPlugin();
            var hostWithPlugin = new RecordingEditorPluginHost([joining]) { PluginViewBaseType = typeof(object) };
            hostWithPlugin.DeclareRegistry<TestPluginContributions.Widget>();
            try
            {
                Assert.False(hostWithPlugin.AssemblyLoadCheck.CanLoad(new Package { Meta = { Name = "Incompatible" } }, path, log));
                var session = new FakeSession();
                session.ServiceProvider.RegisterService(hostWithPlugin);
                hostWithPlugin.RegisterSession(session, new LoggerResult());
                Assert.Equal([PluginState.Joined, PluginState.Incompatible], hostWithPlugin.PluginStatuses.Select(x => x.State));
                Assert.Equal(PluginState.Joined, hostWithPlugin.GetPluginStatus(joining)!.State);
            }
            finally
            {
                hostWithPlugin.Dispose();
            }
            Assert.Empty(hostWithPlugin.PluginStatuses);
        }
        finally
        {
            host.Dispose();
        }

        // The host forgets it with the session
        Assert.Empty(host.PluginStatuses);
        Assert.Equal(((AssetsPlugin?)null, "Incompatible.Editor", (PluginState?)null), changes[^1]);
    }

    private static Package AddPackage(PackageSession session, string name, params string[] assemblies)
    {
        var package = new Package { Meta = { Name = name, Version = new PackageVersion("1.0.0") } };
        var container = new StandalonePackage(package) { IsDependencyPackage = true };
        container.Assemblies.AddRange(assemblies);
        package.State = PackageState.DependenciesReady;
        session.Projects.Add(container);
        return package;
    }

    /// <summary>
    /// Writes an assembly with no code that references the given assemblies, as a plugin built against them would.
    /// </summary>
    private string WriteAssembly(string name, params (string Name, Version Version)[] references)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString(name + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString(name), new Version(1, 0, 0, 0), default, default, default, AssemblyHashAlgorithm.Sha1);
        var runtime = typeof(object).Assembly.GetName();
        metadata.AddAssemblyReference(metadata.GetOrAddString(runtime.Name!), runtime.Version!, default, metadata.GetOrAddBlob(runtime.GetPublicKeyToken() ?? []), default, default);
        foreach (var (referenceName, version) in references)
            metadata.AddAssemblyReference(metadata.GetOrAddString(referenceName), version, default, default, default, default);
        metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        var peBuilder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
        var image = new BlobBuilder();
        peBuilder.Serialize(image);
        var path = Path.Combine(directory, name + ".dll");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }

    private sealed class CountingCheck(IAssemblyLoadCheck inner) : IAssemblyLoadCheck
    {
        public int Calls { get; private set; }

        public bool CanLoad(Package package, string assemblyPath, ILogger log)
        {
            Calls++;
            return inner.CanLoad(package, assemblyPath, log);
        }
    }
}
