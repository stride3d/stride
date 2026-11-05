// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A package saved to a temporary folder and opened through the agnostic <see cref="SessionViewModel"/>, with fake services.
/// </summary>
public sealed class EditorSessionFixture : IDisposable
{
    public const string PackageName = "TestGame";

    /// <summary>
    /// The suffix of a location given to <see cref="OpenAsync"/> that saves a <see cref="TestOtherEditorAsset"/> there.
    /// </summary>
    public const string OtherSuffix = ".other";

    private bool destroyed;
    private bool ownsDirectory = true;

    private EditorSessionFixture(string directory, IViewModelServiceProvider serviceProvider, FakeEditorDialogService dialogs, FakeClipboardService clipboard, FakePluginService plugins)
    {
        Directory = directory;
        ServiceProvider = serviceProvider;
        Dialogs = dialogs;
        Clipboard = clipboard;
        Plugins = plugins;
        Result = new PackageSessionResult();
    }

    public string Directory { get; }

    public UFile PackagePath => new(Path.Combine(Directory, PackageName, PackageName + Stride.Core.Assets.Package.PackageFileExtension));

    public IViewModelServiceProvider ServiceProvider { get; }

    public FakeEditorDialogService Dialogs { get; }

    public FakeClipboardService Clipboard { get; }

    public FakePluginService Plugins { get; }

    public PackageSessionResult Result { get; }

    public SessionViewModel Session { get; private set; } = null!;

    public PackageViewModel Package => Session.LocalPackages.Single();

    public AssetCollectionViewModel Assets => Session.AssetCollection;

    /// <summary>
    /// The location of an asset of the package, as the session names it (the project namespace prefixes it).
    /// </summary>
    public static string Url(string location) => "/" + PackageName + "/" + location;

    /// <summary>
    /// Selects the whole package, assets of sub-folders included.
    /// </summary>
    public void SelectPackageRecursively()
    {
        OnUiThread(() =>
        {
            Assets.DisplayAssetMode = DisplayAssetMode.AssetInSelectedFolderAndSubFolder;
            Assets.SelectedLocations.Clear();
            Assets.SelectedLocations.Add(Package);
        });
    }

    /// <summary>
    /// Runs the action as the UI thread of the editor would: on the session's dispatcher, one at a time with the work the session
    /// dispatches from the thread pool.
    /// </summary>
    /// <remarks>
    /// A test changes the selection, or runs what changes it, through this: the views that follow the selection (the references,
    /// the tags) refresh in the change, and a refresh on the test thread races the one the session dispatches after an edit.
    /// </remarks>
    public void OnUiThread(Action action) => Session.Dispatcher.Invoke(action);

    /// <inheritdoc cref="OnUiThread(Action)"/>
    public T OnUiThread<T>(Func<T> action) => Session.Dispatcher.Invoke(action);

    /// <summary>
    /// Starts the asynchronous action on the session's dispatcher, as the UI thread of the editor would: its continuations run there too.
    /// </summary>
    /// <inheritdoc cref="OnUiThread(Action)" path="/remarks"/>
    public Task OnUiThreadAsync(Func<Task> action) => Session.Dispatcher.InvokeTask(action);

    /// <inheritdoc cref="OnUiThreadAsync(Func{Task})"/>
    public Task<T> OnUiThreadAsync<T>(Func<Task<T>> action) => Session.Dispatcher.InvokeTask(action);

    /// <summary>
    /// The messages of the loading, for assertion failures.
    /// </summary>
    public string Log => string.Join(Environment.NewLine, Result.Messages.Select(x => x.ToString()));

    /// <summary>
    /// Saves a package with the given assets (location relative to the package, value) and opens it.
    /// </summary>
    public static Task<EditorSessionFixture> OpenAsync(params (string Location, string Value)[] assets) => OpenCoreAsync(null, assets);

    /// <summary>
    /// Saves a package whose project is the executable of a platform (the head of a game), with the given assets, and opens it.
    /// </summary>
    public static Task<EditorSessionFixture> OpenExecutableAsync(PlatformType platform, params (string Location, string Value)[] assets) => OpenCoreAsync(platform, assets);

    /// <summary>
    /// Saves a solution as the New game template makes it: the package, a library, and an executable project for each given
    /// platform (TestGame.Windows, TestGame.Linux...), then opens the solution.
    /// </summary>
    public static Task<EditorSessionFixture> OpenGameAsync(params PlatformType[] executablePlatforms) => OpenCoreAsync(null, [], executablePlatforms);

    /// <summary>
    /// Saves a package with the given assets (location relative to the package, asset) and opens it. Without a project next to
    /// it, the package is read-only, as the packages of the dependencies (NuGet packages) are.
    /// </summary>
    public static Task<EditorSessionFixture> OpenAsync(bool readOnly, params (string Location, Asset Asset)[] assets)
    {
        var directory = NewDirectory();
        WritePackage(directory, readOnly ? null : ProjectFile(null), assets);
        return OpenCoreAsync(directory);
    }

    /// <summary>
    /// Destroys the session, then opens the files it left in a new session with new services, as the editor does when it opens
    /// the project again. The new fixture deletes the folder when it is disposed; this one no longer does.
    /// </summary>
    public async Task<EditorSessionFixture> ReopenAsync()
    {
        if (!destroyed)
        {
            destroyed = true;
            Session.Destroy();
            WaitForBackgroundWork(Session);
        }
        ownsDirectory = false;
        return await OpenCoreAsync(Directory);
    }

    private static string NewDirectory() => Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));

    private static async Task<EditorSessionFixture> OpenCoreAsync(string directory)
    {
        var fixture = CreateFixture(directory);
        await fixture.OpenSessionAsync(fixture.PackagePath);
        return fixture;
    }

    private static EditorSessionFixture CreateFixture(string directory)
    {
        var dialogs = new FakeEditorDialogService();
        var clipboard = new FakeClipboardService();
        var plugins = new FakePluginService();
        var serviceProvider = new ViewModelServiceProvider(
        [
            // One dispatcher call at a time, as on the UI thread: the session raises its events on the thread pool
            new ExclusiveDispatcherService(),
            dialogs,
            clipboard,
            plugins,
            new FakeThumbnailDataFactory(),
        ]);
        return new EditorSessionFixture(directory, serviceProvider, dialogs, clipboard, plugins);
    }

    private async Task OpenSessionAsync(UFile path)
    {
        var session = await SessionViewModel.OpenSessionAsync(path, Result, new FakeMainViewModel(ServiceProvider), ServiceProvider);
        if (session is null)
            throw new InvalidOperationException("The session could not be opened:" + Environment.NewLine + Log);
        Session = session;
        // The asset view of the session restores the filters of the editor's settings, which every session of the test process
        // shares: a filter that a test left (or is using in parallel) would hide the assets of this one
        session.AssetCollection.ClearAssetFilters();

        // The paste processors of the core assets, registered by the view plugin in the editor
        var copyPaste = ServiceProvider.Get<ICopyPasteService>();
        copyPaste.RegisterProcessor(new AssetPropertyPasteProcessor());
        copyPaste.RegisterProcessor(new AssetItemPasteProcessor(session));

        // What the session does in the background once open, so that the tests start from a session at rest
        await session.Initialization;
    }

    private static async Task<EditorSessionFixture> OpenCoreAsync(PlatformType? executablePlatform, (string Location, string Value)[] assets, PlatformType[]? heads = null)
    {
        var directory = NewDirectory();
        var fixture = CreateFixture(directory);
        fixture.WritePackage(assets, executablePlatform);
        var path = heads is null ? fixture.PackagePath : fixture.WriteSolution(heads);
        await fixture.OpenSessionAsync(path);
        return fixture;
    }

    /// <summary>
    /// Reads the asset file saved for the given location.
    /// </summary>
    public TestEditorAsset ReadAsset(string location)
    {
        var path = Path.Combine(Directory, PackageName, "Assets", location + ".sdtesteditor");
        return (TestEditorAsset)AssetFileSerializer.Load<TestEditorAsset>(path).Asset;
    }

    public void Dispose()
    {
        if (!destroyed)
        {
            destroyed = true;
            Session.Destroy();
            WaitForBackgroundWork(Session);
        }
        if (!ownsDirectory)
            return;
        try
        {
            System.IO.Directory.Delete(Directory, true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }

    /// <summary>
    /// Waits for the background work of a destroyed session to end: nothing the test started may outlive it.
    /// </summary>
    internal static void WaitForBackgroundWork(SessionViewModel session)
    {
        if (!session.BackgroundWork.Wait(TimeSpan.FromSeconds(30)))
            throw new TimeoutException("The background work of the session did not end within 30 seconds of its destruction.");
    }

    private UFile WriteSolution(PlatformType[] heads)
    {
        // The heads have no package of their own, as in the template; they hold no asset types either
        var projects = new List<string> { $"{PackageName}/{PackageName}.csproj" };
        foreach (var platform in heads)
        {
            var name = $"{PackageName}.{platform}";
            System.IO.Directory.CreateDirectory(Path.Combine(Directory, name));
            File.WriteAllText(Path.Combine(Directory, name, name + ".csproj"), ProjectFile(platform));
            projects.Add($"{name}/{name}.csproj");
        }
        var solutionPath = Path.Combine(Directory, PackageName + ".slnx");
        File.WriteAllText(solutionPath,
            "<Solution>" + Environment.NewLine +
            string.Concat(projects.Select(x => $"  <Project Path=\"{x}\" />" + Environment.NewLine)) +
            "</Solution>" + Environment.NewLine);
        return new UFile(solutionPath);
    }

    /// <summary>
    /// A project that holds no asset types, so that the session does not build it; an executable declares its platform, as the
    /// heads of a game do.
    /// </summary>
    private static string ProjectFile(PlatformType? executablePlatform)
    {
        var executable = executablePlatform is { } platform
            ? "    <OutputType>Exe</OutputType>" + Environment.NewLine +
              $"    <StridePlatform>{platform}</StridePlatform>" + Environment.NewLine
            : string.Empty;
        return "<Project Sdk=\"Microsoft.NET.Sdk\">" + Environment.NewLine +
            "  <PropertyGroup>" + Environment.NewLine +
            "    <TargetFramework>net10.0</TargetFramework>" + Environment.NewLine +
            executable +
            "    <StrideContainsAssetTypes>false</StrideContainsAssetTypes>" + Environment.NewLine +
            "  </PropertyGroup>" + Environment.NewLine +
            "</Project>" + Environment.NewLine;
    }

    /// <summary>
    /// Saves a package in the given folder with the given assets, and the given project file next to it; without a project,
    /// the package is read-only.
    /// </summary>
    private static void WritePackage(string directory, string? projectFile, (string Location, Asset Asset)[] assets)
    {
        var packageDirectory = Path.Combine(directory, PackageName);
        System.IO.Directory.CreateDirectory(Path.Combine(packageDirectory, "Assets"));
        if (projectFile is not null)
            File.WriteAllText(Path.Combine(packageDirectory, PackageName + ".csproj"), projectFile);

        var packagePath = new UFile(Path.Combine(packageDirectory, PackageName + Stride.Core.Assets.Package.PackageFileExtension));
        var package = new Stride.Core.Assets.Package
        {
            FullPath = packagePath,
            Meta = { Name = PackageName, Version = new PackageVersion("1.0.0") },
            AssetFolders = { new AssetFolder("Assets") },
            ResourceFolders = { "Resources" },
        };
        AssetFileSerializer.Save(packagePath.ToOSPath(), package, null);

        foreach (var (location, asset) in assets)
        {
            var extension = AssetRegistry.GetDefaultExtension(asset.GetType());
            var path = Path.Combine(packageDirectory, "Assets", location + extension);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetFileSerializer.Save(path, asset, null);
        }
    }

    private void WritePackage((string Location, string Value)[] assets, PlatformType? executablePlatform)
    {
        var packageDirectory = Path.Combine(Directory, PackageName);
        System.IO.Directory.CreateDirectory(Path.Combine(packageDirectory, "Assets"));

        // A .csproj next to the package makes it a project of the solution, hence editable
        File.WriteAllText(Path.Combine(packageDirectory, PackageName + ".csproj"), ProjectFile(executablePlatform));

        var package = new Stride.Core.Assets.Package
        {
            FullPath = PackagePath,
            Meta = { Name = PackageName, Version = new PackageVersion("1.0.0") },
            AssetFolders = { new AssetFolder("Assets") },
            ResourceFolders = { "Resources" },
        };
        AssetFileSerializer.Save(PackagePath.ToOSPath(), package, null);

        foreach (var (location, value) in assets)
        {
            // A location ending with ".other" holds a TestOtherEditorAsset, the others a TestEditorAsset
            var isOther = location.EndsWith(OtherSuffix, StringComparison.Ordinal);
            var path = Path.Combine(packageDirectory, "Assets", isOther ? location[..^OtherSuffix.Length] + ".sdtestother" : location + ".sdtesteditor");
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Asset asset = isOther ? new TestOtherEditorAsset { Value = value } : new TestEditorAsset { Value = value };
            AssetFileSerializer.Save(path, asset, null);
        }
    }
}
