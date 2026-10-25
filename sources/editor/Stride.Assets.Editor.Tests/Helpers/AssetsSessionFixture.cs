// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Build;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// A session over a small project of Stride assets, with the default assets plugin and the editor plugin registered the way
/// the editor shell registers them.
/// </summary>
public sealed class AssetsSessionFixture : IDisposable
{
    public const string PackageName = "TestGame";

    private bool disposed;

    private AssetsSessionFixture(string directory, IViewModelServiceProvider serviceProvider, TestPluginService plugins)
    {
        Directory = directory;
        ServiceProvider = serviceProvider;
        Plugins = plugins;
        Result = new PackageSessionResult();
    }

    public string Directory { get; }

    public string PackageDirectory => Path.Combine(Directory, PackageName);

    public UFile PackagePath => new(Path.Combine(PackageDirectory, PackageName + Stride.Core.Assets.Package.PackageFileExtension));

    public IViewModelServiceProvider ServiceProvider { get; }

    public TestPluginService Plugins { get; }

    public PackageSessionResult Result { get; }

    public SessionViewModel Session { get; private set; } = null!;

    public StrideDefaultAssetsPlugin AssetsPlugin { get; } = new();

    public StrideEditorPlugin EditorPlugin { get; } = new() { EnableThumbnailService = false };

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

    public T Asset<T>(string location) where T : AssetViewModel => Assert.IsType<T>(Asset(location), exactMatch: false);

    /// <summary>
    /// Saves a package with the given assets (location relative to the package, asset) and opens it.
    /// </summary>
    /// <param name="initializeEditor">Whether to run the editor plugin's session initialization (build services, commands and updaters).</param>
    public static async Task<AssetsSessionFixture> OpenAsync(bool initializeEditor, params (string Location, Asset Asset)[] assets)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
        var assetsPlugin = new StrideDefaultAssetsPlugin();
        var editorPlugin = new StrideEditorPlugin { EnableThumbnailService = false };
        var plugins = new TestPluginService(assetsPlugin, editorPlugin);
        var serviceProvider = new ViewModelServiceProvider(
        [
            new NullDispatcherService(),
            new FakeEditorDialogService(),
            new FakeClipboardService(),
            plugins,
            new FakeThumbnailDataFactory(),
        ]);
        var fixture = new AssetsSessionFixture(directory, serviceProvider, plugins);
        fixture.WritePackage(assets);
        var session = await SessionViewModel.OpenSessionAsync(fixture.PackagePath, fixture.Result, new FakeMainViewModel(serviceProvider), serviceProvider);
        if (session is null)
            throw new InvalidOperationException("The session could not be opened:" + Environment.NewLine + fixture.Log);
        fixture.Session = session;
        if (initializeEditor)
            plugins.InitializeSession(session);
        return fixture;
    }

    /// <summary>
    /// The property grid of the given asset, as the editor builds it from the registered commands and updaters.
    /// </summary>
    public async Task<NodeViewModel> PropertiesOf(AssetViewModel asset)
    {
        var graph = await Session.AssetViewProperties.GenerateSelectionPropertiesAsync([asset]);
        return graph?.RootNode ?? throw new InvalidOperationException($"No properties for {asset.Url}.");
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            var builder = ServiceProvider.TryGet<GameStudioBuilderService>();
            if (builder != null)
            {
                builder.Database.Dispose();
                builder.Dispose();
                // The object database is process-wide (the editor opens one session per process); the next fixture opens its own
                Stride.Core.BuildEngine.Builder.CloseObjectDatabase();
            }
            ServiceProvider.TryGet<GameSettingsProviderService>()?.Dispose();
            Session.Destroy();
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
