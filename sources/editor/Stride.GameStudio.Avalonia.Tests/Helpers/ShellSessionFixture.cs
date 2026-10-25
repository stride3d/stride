// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Build;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

/// <summary>
/// A small package of Stride assets opened by the shell's <see cref="MainViewModel"/>, with the services the shell's
/// <see cref="App"/> registers: what the desktop host does when it starts with a session path.
/// </summary>
internal sealed class ShellSessionFixture : IDisposable
{
    public const string PackageName = "TestGame";

    private bool disposed;

    private ShellSessionFixture(string directory, ViewModelServiceProvider serviceProvider)
    {
        Directory = directory;
        ServiceProvider = serviceProvider;
        Main = new MainViewModel(serviceProvider);
    }

    public string Directory { get; }

    public string PackageDirectory => Path.Combine(Directory, PackageName);

    public UFile PackagePath => new(Path.Combine(PackageDirectory, PackageName + Stride.Core.Assets.Package.PackageFileExtension));

    public ViewModelServiceProvider ServiceProvider { get; }

    public MainViewModel Main { get; }

    public PluginService Plugins => ServiceProvider.Get<PluginService>();

    /// <summary>
    /// The session the main view model opened; null when the package was not opened (yet).
    /// </summary>
    public SessionViewModel Session => Main.Session ?? throw new InvalidOperationException("No session is open.");

    public PackageViewModel Package => Session.LocalPackages.Single();

    /// <summary>
    /// The location of an asset of the package, as the session names it (the package name prefixes it).
    /// </summary>
    public static string Url(string location) => "/" + PackageName + "/" + location;

    public AssetViewModel Asset(string location) => Session.GetAssetByUrl(Url(location)) ?? throw new InvalidOperationException($"No asset at {location}");

    /// <summary>
    /// Writes a package with the given assets (location relative to the package, asset); <see cref="OpenAsync"/> opens it.
    /// </summary>
    public static ShellSessionFixture Create(params (string Location, Asset Asset)[] assets)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.GameStudio.Avalonia.Tests", Guid.NewGuid().ToString("N"));
        var fixture = new ShellSessionFixture(directory, App.InitializeServiceProvider());
        fixture.WritePackage(assets);
        return fixture;
    }

    /// <summary>
    /// Writes a package with the given assets and opens it through the main view model, the way the shell opens the session
    /// of its command line.
    /// </summary>
    public static async Task<ShellSessionFixture> OpenAsync(params (string Location, Asset Asset)[] assets)
    {
        var fixture = Create(assets);
        var opened = await fixture.Main.OpenSession(fixture.PackagePath);
        if (opened != true || fixture.Main.Session is null)
        {
            fixture.Dispose();
            throw new InvalidOperationException($"The session could not be opened ({opened}).");
        }
        return fixture;
    }

    /// <summary>
    /// Replaces the shell's dialog service by one that records the message boxes instead of showing them.
    /// </summary>
    public RecordingDialogService ReplaceDialogService()
    {
        ServiceProvider.UnregisterService(ServiceProvider.Get<EditorDialogService>());
        var dialogs = new RecordingDialogService();
        ServiceProvider.RegisterService(dialogs);
        return dialogs;
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;

        if (Main.Session is { } session)
        {
            var builder = session.ServiceProvider.TryGet<GameStudioBuilderService>();
            if (builder != null)
            {
                builder.Database.Dispose();
                builder.Dispose();
                // The object database is process-wide (the editor opens one session per process); the next fixture opens its own
                Stride.Core.BuildEngine.Builder.CloseObjectDatabase();
            }
            session.ServiceProvider.TryGet<GameSettingsProviderService>()?.Dispose();
            session.Destroy();
        }
        Plugins.Dispose();

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
            Meta = { Name = PackageName, Version = new Stride.Core.PackageVersion("1.0.0") },
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
