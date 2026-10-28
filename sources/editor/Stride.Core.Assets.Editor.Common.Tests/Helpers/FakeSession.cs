// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Reflection;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModel.Logs;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Editor.Common.Tests.Helpers;

/// <summary>
/// A session without any UI, for the tests of the session objects. It records what they ask of it.
/// </summary>
public sealed class FakeSession : ISessionViewModel
{
    public const string LocalPackagesCategory = "Local packages";
    public const string StorePackagesCategory = "External packages";

    private readonly Dictionary<AssetId, AssetViewModel> assets = [];
    private readonly Dictionary<string, PackageCategoryViewModel> packageCategories;

    public FakeSession()
    {
        Dialogs = DialogRecorder.Create();
        ServiceProvider = new ViewModelServiceProvider(
        [
            new NullDispatcherService(),
            UndoRedo,
            Plugins,
            ProgressWindows,
            Thumbnails,
            Dialogs,
        ]);
        // The categories are session objects too, they need the service provider
        packageCategories = new Dictionary<string, PackageCategoryViewModel>
        {
            [LocalPackagesCategory] = new(LocalPackagesCategory, this),
            [StorePackagesCategory] = new(StorePackagesCategory, this),
        };
        GraphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        AssetNodeContainer = new SessionNodeContainer(this);
    }

    public UndoRedoService UndoRedo { get; } = new(100);

    public FakePluginService Plugins { get; } = new();

    public FakeProgressWindowService ProgressWindows { get; } = new();

    public FakeThumbnailDataFactory Thumbnails { get; } = new();

    public IDialogService2 Dialogs { get; }

    /// <summary>
    /// Gets the messages that the session objects showed with a blocking message box.
    /// </summary>
    public IReadOnlyList<string> Messages => ((DialogRecorder)(object)Dialogs).Messages;

    public FakeAssetLog Log { get; } = new();

    public List<IReadOnlyCollection<AssetViewModel>> NotifiedAssetChanges { get; } = [];

    public int RootAssetSelectionRefreshes { get; private set; }

    public IEnumerable<IDirtiable> Dirtiables { get; set; } = [];

    public IViewModelServiceProvider ServiceProvider { get; }

    public IDispatcherService Dispatcher => ServiceProvider.Get<IDispatcherService>();

    public UFile SolutionPath { get; set; } = null!;

    public ProjectViewModel CurrentProject { get; set; } = null!;

    public IReadOnlyDictionary<string, PackageCategoryViewModel> PackageCategories => packageCategories;

    public IObservableCollection<PackageViewModel> StorePackages { get; } = new ObservableList<PackageViewModel>();

    public IObservableCollection<PackageViewModel> LocalPackages { get; } = new ObservableList<PackageViewModel>();

    public IEnumerable<PackageViewModel> AllPackages => LocalPackages.Concat(StorePackages);

    public IEnumerable<AssetViewModel> AllAssets => assets.Values;

    public IAssetDependencyManager DependencyManager => null!;

    public AssetPropertyGraphContainer GraphContainer { get; }

    public SessionNodeContainer AssetNodeContainer { get; }

    public IAssetCollectionViewModel ActiveAssetView => null!;

    public ISessionObjectPropertiesViewModel AssetViewProperties => null!;

    public IAssetSourceTrackerViewModel SourceTracker => null!;

    public IAssetLogViewModel AssetLog => Log;

    public bool IsInFixupAssetContext => false;

    public bool HasAssetReplacements => false;

#pragma warning disable CS0067 // Raised by the editor's session only
    public event EventHandler<AssetChangedEventArgs>? AssetPropertiesChanged;

    public event EventHandler<NotifyCollectionChangedEventArgs>? DeletedAssetsChanged;

    public event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;
#pragma warning restore CS0067

    public AssetViewModel GetAssetById(AssetId id) => assets.GetValueOrDefault(id)!;

    public Type GetAssetViewModelType(AssetItem assetItem) => typeof(AssetViewModel<>);

    public void NotifyAssetPropertiesChanged(IReadOnlyCollection<AssetViewModel> assets) => NotifiedAssetChanges.Add(assets);

    public void CheckConsistency() => throw new NotSupportedException();

    public void CheckAssetReplacements(AssetViewModel? changedAsset = null)
    {
    }

    public PackageViewModel GetDefaultRootAssetTarget(IReadOnlyCollection<AssetViewModel> assets) => throw new NotSupportedException();

    public void ToggleRootAsset(IReadOnlyCollection<AssetViewModel> assets, PackageViewModel target) => throw new NotSupportedException();

    public void RefreshRootAssetSelection() => RootAssetSelectionRefreshes++;

    public void RegisterAsset(AssetViewModel asset) => assets.Add(asset.Id, asset);

    public void UnregisterAsset(AssetViewModel asset) => assets.Remove(asset.Id);

    public void ProcessAddedProjects(LoggerResult loggerResult, WorkProgressViewModel workProgress, bool packageAlreadyInSession) => throw new NotSupportedException();

    public void ProcessRemovedProjects() => throw new NotSupportedException();

    public Task LoadAddedDependencies(LoggerResult loggerResult) => throw new NotSupportedException();

    /// <summary>
    /// Creates a package of the solution (editable), with its assets ready.
    /// </summary>
    public PackageViewModel CreateLocalPackage(string name)
    {
        var path = new UFile($"/fake/{name}/{name}.sdpkg");
        var package = new Package { FullPath = path, State = PackageState.AssetsReady };
        return new PackageViewModel(this, new SolutionProject(package, Guid.NewGuid(), path), false);
    }

    /// <summary>
    /// Creates a package that does not belong to the solution (read-only).
    /// </summary>
    public PackageViewModel CreateStorePackage(string name)
    {
        var package = new Package { FullPath = new UFile($"/fake/{name}/{name}.sdpkg"), State = PackageState.AssetsReady };
        return new PackageViewModel(this, new StandalonePackage(package), false);
    }
}

public sealed class FakePluginService : IAssetsPluginService
{
    public List<Type> PrimitiveTypes { get; } = [];

    public List<AssetsPlugin> Plugins { get; } = [];

    IReadOnlyList<AssetsPlugin> IAssetsPluginService.Plugins => Plugins;

    public IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session) => PrimitiveTypes;

    public IEnumerable<PackageSettingsEntry> GetProfileSettings() => [];

    public bool HasImagesForEnum(ISessionViewModel? session, Type enumType) => false;

    public object? GetImageForEnum(ISessionViewModel? session, object value) => null;

    public bool HasEditorView(ISessionViewModel session, Type viewModelType) => false;

    public Type? GetAssetViewModelType(Type assetType) => null;

    public Type? GetEditorViewModelType(Type viewModelType) => null;

    public Type? GetEditorViewType(Type editorViewModelType) => null;

    public Type? GetPreviewViewModelType(Type previewType) => null;

    public Type? GetPreviewViewType(Type previewType) => null;

    public void RegisterSession(ISessionViewModel session, ILogger logger)
    {
    }

    public void InitializeSession(ISessionViewModel session)
    {
        foreach (var plugin in Plugins)
            plugin.InitializeSession(session);
    }
}

public sealed class FakeProgressWindowService : IProgressWindowService
{
    public List<(WorkProgressViewModel WorkProgress, int MinDelay)> Shown { get; } = [];

    public void ShowProgressWindow(WorkProgressViewModel workProgress, int minDelay) => Shown.Add((workProgress, minDelay));
}

public sealed class FakeThumbnailDataFactory : IThumbnailDataFactory
{
    public List<object> ResourceKeys { get; } = [];

    public ThumbnailData CreateFromResource(ObjectId thumbnailId, object resourceKey)
    {
        ResourceKeys.Add(resourceKey);
        return new FakeThumbnailData(thumbnailId, resourceKey);
    }
}

/// <summary>
/// A thumbnail whose image is the object it was built from.
/// </summary>
public sealed class FakeThumbnailData(ObjectId thumbnailId, object image) : ThumbnailData(thumbnailId)
{
    public int FreeCount { get; private set; }

    protected override object BuildImageSource() => image;

    protected override void FreeBuildingResources() => FreeCount++;
}

public sealed class FakeAssetLog : IAssetLogViewModel
{
    private readonly Dictionary<LogKey, LoggerResult> loggers = [];

    public List<LogKey> Cleared { get; } = [];

    public LoggerResult this[LogKey key] => loggers[key];

    public Logger GetLogger(LogKey key, bool create = true)
    {
        if (!loggers.TryGetValue(key, out var logger) && create)
            loggers.Add(key, logger = new LoggerResult());
        return logger!;
    }

    public void ClearMessages(LogKey key)
    {
        Cleared.Add(key);
        if (loggers.TryGetValue(key, out var logger))
            logger.Clear();
    }
}

/// <summary>
/// A dialog service that records the blocking message boxes and answers OK; the other dialogs are not supported.
/// </summary>
public class DialogRecorder : DispatchProxy
{
    public List<string> Messages { get; } = [];

    public static IDialogService2 Create() => Create<IDialogService2, DialogRecorder>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name == nameof(IDialogService2.BlockingMessageBox) && targetMethod.ReturnType == typeof(MessageBoxResult))
        {
            Messages.Add((string)args![0]!);
            return MessageBoxResult.OK;
        }
        throw new NotSupportedException($"{targetMethod?.Name} is not supported by the fake dialog service.");
    }
}
