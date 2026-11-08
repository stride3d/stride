// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Build;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Editor.Tests.Helpers;

/// <summary>
/// A package with the given assets, saved to a temporary folder and opened through the agnostic <see cref="SessionViewModel"/>,
/// with the editor's build services registered the way the GameStudio plugin registers them.
/// </summary>
/// <remarks>
/// Its dispatcher gives the access of the UI thread to the dispatched work only: a test opens, changes and disposes the session after
/// <c>await ExclusiveDispatcherService.SwitchTo()</c>, as the editor does on its UI thread.
/// </remarks>
public sealed class ServicesSessionFixture : IAsyncDisposable
{
    public const string PackageName = "TestGame";

    private bool disposed;

    private ServicesSessionFixture(string directory, IViewModelServiceProvider serviceProvider, FakeEditorDialogService dialogs)
    {
        Directory = directory;
        ServiceProvider = serviceProvider;
        Dialogs = dialogs;
        Result = new PackageSessionResult();
    }

    public string Directory { get; }

    public string PackageDirectory => Path.Combine(Directory, PackageName);

    public UFile PackagePath => new(Path.Combine(PackageDirectory, PackageName + Stride.Core.Assets.Package.PackageFileExtension));

    public IViewModelServiceProvider ServiceProvider { get; }

    public FakeEditorDialogService Dialogs { get; }

    public PackageSessionResult Result { get; }

    public SessionViewModel Session { get; private set; } = null!;

    public GameSettingsProviderService SettingsProvider { get; private set; } = null!;

    public GameStudioBuilderService Builder { get; private set; } = null!;

    public GameStudioDatabase Database => Builder.Database;

    public PackageViewModel Package => Session.LocalPackages.Single();

    /// <summary>
    /// The messages of the loading, for assertion failures.
    /// </summary>
    public string Log => string.Join(Environment.NewLine, Result.Messages.Select(x => x.ToString()));

    /// <summary>
    /// The location of an asset of the package, as the session names it (the project namespace prefixes it).
    /// </summary>
    public static string Url(string location) => "/" + PackageName + "/" + location;

    public AssetViewModel Asset(string location) => Session.GetAssetByUrl(Url(location)) ?? throw new InvalidOperationException($"No asset at {location}");

    /// <summary>
    /// Saves a package with the given assets (location relative to the package, asset) and opens it with the build services.
    /// </summary>
    /// <param name="withBuilder">Whether to create the builder service (and its database); the thumbnail tests need it, a session-only test does not.</param>
    public static Task<ServicesSessionFixture> OpenAsync(bool withBuilder, params (string Location, Asset Asset)[] assets)
    {
        // The shader importer is a Stride assets one (Stride.Assets.Editor): this layer builds without it
        return OpenAsync(withBuilder, null, assets);
    }

    /// <summary>
    /// Saves a package with the given assets and opens it with the build services, the builder importing shaders with the given importer.
    /// </summary>
    public static Task<ServicesSessionFixture> OpenWithShaderImporterAsync(IShaderImporter shaderImporter, params (string Location, Asset Asset)[] assets)
    {
        return OpenAsync(true, shaderImporter, assets);
    }

    private static async Task<ServicesSessionFixture> OpenAsync(bool withBuilder, IShaderImporter? shaderImporter, (string Location, Asset Asset)[] assets)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.Editor.Tests", Guid.NewGuid().ToString("N"));
        var dialogs = new FakeEditorDialogService();
        var dispatcher = new ExclusiveDispatcherService(strict: true);
        dispatcher.EnsureAccess();
        var serviceProvider = new ViewModelServiceProvider(
        [
            dispatcher,
            dialogs,
            new FakeClipboardService(),
            new FakePluginService(),
            new FakeThumbnailDataFactory(),
        ]);
        var fixture = new ServicesSessionFixture(directory, serviceProvider, dialogs);
        fixture.WritePackage(assets);
        var session = await SessionViewModel.OpenSessionAsync(fixture.PackagePath, fixture.Result, new FakeMainViewModel(serviceProvider), serviceProvider);
        if (session is null)
            throw new InvalidOperationException("The session could not be opened:" + Environment.NewLine + fixture.Log);
        fixture.Session = session;

        // The services the GameStudio plugin registers when a session opens
        fixture.SettingsProvider = new GameSettingsProviderService(session);
        serviceProvider.RegisterService(fixture.SettingsProvider);
        if (withBuilder)
        {
            var buildDirectory = Path.Combine(directory, "build");
            System.IO.Directory.CreateDirectory(buildDirectory);
            fixture.Builder = new GameStudioBuilderService(session, fixture.SettingsProvider, shaderImporter, buildDirectory);
            serviceProvider.RegisterService(fixture.Builder);
            serviceProvider.RegisterService(fixture.Builder.Database);
        }

        // What the session does in the background once open, so that the tests start from a session at rest
        await session.Initialization;
        return fixture;
    }

    /// <summary>
    /// Writes a resource file of the package and returns its path, for the assets with a source.
    /// </summary>
    public static string WriteResource(string packageDirectory, string name, string content)
    {
        var path = Path.Combine(packageDirectory, "Resources", name);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// The temporary package directory the assets of a fixture are written to, before the fixture exists (for the sources of the assets).
    /// </summary>
    public static string NewPackageDirectory() => Path.Combine(Path.GetTempPath(), "Stride.Editor.Tests", Guid.NewGuid().ToString("N"), PackageName);

    public async ValueTask DisposeAsync()
    {
        if (!disposed)
        {
            disposed = true;
            if (Builder != null)
            {
                // The builder waits for its thread, which can dispatch work: outside the dispatcher
                await Task.Run(() =>
                {
                    Builder.Database.Dispose();
                    Builder.Dispose();
                    // The object database is process-wide (the editor opens one session per process); the next fixture opens its own
                    Stride.Core.BuildEngine.Builder.CloseObjectDatabase();
                });
            }
            var dispatcher = ServiceProvider.Get<IDispatcherService>();
            dispatcher.Invoke(() =>
            {
                SettingsProvider?.Dispose();
                Session.Destroy();
            });
            // Nothing the test started may outlive it
            try
            {
                await Session.BackgroundWork.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                throw new TimeoutException("The background work of the session did not end within 30 seconds of its destruction.");
            }
        }
        try
        {
            System.IO.Directory.Delete(Directory, true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }

    private void WritePackage((string Location, Asset Asset)[] assets)
    {
        System.IO.Directory.CreateDirectory(Path.Combine(PackageDirectory, "Assets"));

        // A .csproj next to the package makes it a project of the solution, hence editable. It declares that it holds no
        // asset types so that the session does not build it.
        File.WriteAllText(Path.Combine(PackageDirectory, PackageName + ".csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\">" + Environment.NewLine +
            "  <PropertyGroup>" + Environment.NewLine +
            "    <TargetFramework>net10.0</TargetFramework>" + Environment.NewLine +
            "    <StrideContainsAssetTypes>false</StrideContainsAssetTypes>" + Environment.NewLine +
            "  </PropertyGroup>" + Environment.NewLine +
            "</Project>" + Environment.NewLine);

        var package = new Stride.Core.Assets.Package
        {
            FullPath = PackagePath,
            Meta = { Name = PackageName, Version = new PackageVersion("1.0.0") },
            AssetFolders = { new AssetFolder("Assets") },
            ResourceFolders = { "Resources" },
        };
        AssetFileSerializer.Save(PackagePath.ToOSPath(), package, null);

        foreach (var (location, asset) in assets)
        {
            var extension = AssetRegistry.GetDefaultExtension(asset.GetType()) ?? throw new InvalidOperationException($"{asset.GetType()} has no AssetDescription");
            var path = Path.Combine(PackageDirectory, "Assets", location + extension);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetFileSerializer.Save(path, asset, null);
        }
    }
}
