// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.CopyPasteProcessors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A package saved to a temporary folder and opened through the agnostic <see cref="SessionViewModel"/>, with fake services.
/// </summary>
public sealed class EditorSessionFixture : IDisposable
{
    public const string PackageName = "TestGame";

    private bool destroyed;

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
        Assets.DisplayAssetMode = DisplayAssetMode.AssetInSelectedFolderAndSubFolder;
        Assets.SelectedLocations.Clear();
        Assets.SelectedLocations.Add(Package);
    }

    /// <summary>
    /// The messages of the loading, for assertion failures.
    /// </summary>
    public string Log => string.Join(Environment.NewLine, Result.Messages.Select(x => x.ToString()));

    /// <summary>
    /// Saves a package with the given assets (location relative to the package, value) and opens it.
    /// </summary>
    public static async Task<EditorSessionFixture> OpenAsync(params (string Location, string Value)[] assets)
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
        var dialogs = new FakeEditorDialogService();
        var clipboard = new FakeClipboardService();
        var plugins = new FakePluginService();
        var serviceProvider = new ViewModelServiceProvider(
        [
            new NullDispatcherService(),
            dialogs,
            clipboard,
            plugins,
            new FakeThumbnailDataFactory(),
        ]);
        var fixture = new EditorSessionFixture(directory, serviceProvider, dialogs, clipboard, plugins);
        fixture.WritePackage(assets);
        var session = await SessionViewModel.OpenSessionAsync(fixture.PackagePath, fixture.Result, new FakeMainViewModel(serviceProvider), serviceProvider);
        if (session is null)
            throw new InvalidOperationException("The session could not be opened:" + Environment.NewLine + fixture.Log);
        fixture.Session = session;

        // The paste processors of the core assets, registered by the view plugin in the editor
        var copyPaste = serviceProvider.Get<ICopyPasteService>();
        copyPaste.RegisterProcessor(new AssetPropertyPasteProcessor());
        copyPaste.RegisterProcessor(new AssetItemPasteProcessor(session));
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

    private void WritePackage((string Location, string Value)[] assets)
    {
        var packageDirectory = Path.Combine(Directory, PackageName);
        System.IO.Directory.CreateDirectory(Path.Combine(packageDirectory, "Assets"));

        // A .csproj next to the package makes it a project of the solution, hence editable. It declares that it holds no
        // asset types so that the session does not build it.
        File.WriteAllText(Path.Combine(packageDirectory, PackageName + ".csproj"),
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

        foreach (var (location, value) in assets)
        {
            var path = Path.Combine(packageDirectory, "Assets", location + ".sdtesteditor");
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetFileSerializer.Save(path, new TestEditorAsset { Value = value }, null);
        }
    }
}
