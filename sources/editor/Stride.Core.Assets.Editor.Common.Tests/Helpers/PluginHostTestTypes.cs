// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Views;
using Stride.Core.Quantum;
using Stride.Core.Settings;

// The declared images of the test assembly: the test host keeps the bytes of the resource as the image
[assembly: TypeImage(typeof(PluginHostDeclaredType), "TestPluginImage.txt")]
[assembly: EnumImage(PluginHostImageEnum.Declared, "TestPluginImage.txt")]
[assembly: EnumImage(PluginHostImageEnum.Both, "TestPluginImage.txt")]

namespace Stride.Core.Assets.Editor.Common.Tests.Helpers;

/// <summary>
/// An editor host for the tests: the template providers go to a list, the editor views are <see cref="IEditorView"/>s and
/// the property grid is a graph service of its own.
/// </summary>
public sealed class RecordingEditorPluginHost : EditorPluginHost
{
    public RecordingEditorPluginHost(IReadOnlyList<AssetsPlugin> plugins, IDispatcherService? dispatcher = null)
        : base(plugins, dispatcher)
    {
    }

    public List<ITemplateProvider> TemplateProviders { get; } = [];

    public GraphViewModelService GraphService { get; } = new(new NodeContainer());

    public bool ShowsAssemblyImages { get; init; }

    protected override bool ShowsImagesOfRegisteredAssemblies => ShowsAssemblyImages;

    public Func<Assembly, IEnumerable<Type>>? PrimitiveTypesOf { get; init; }

    protected override IEnumerable<Type> FindPrimitiveTypes(Assembly assembly) => PrimitiveTypesOf?.Invoke(assembly) ?? [];

    protected override Type AssetEditorViewModelType => typeof(IAssetEditorViewModel);

    protected override Type AssetEditorViewType => typeof(IEditorView);

    protected override bool AddTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
    {
        TemplateProviders.Add(provider);
        return true;
    }

    protected override void RemoveTemplateProvider(ISessionViewModel session, ITemplateProvider provider) => TemplateProviders.Remove(provider);

    protected override object DecodeImage(byte[] data) => data;

    protected override GraphViewModelService GetGraphViewModelService(ISessionViewModel session) => GraphService;
}

public sealed class PluginHostAsset : Asset;

[AssetViewModel<PluginHostAsset>]
public sealed class PluginHostAssetViewModel(AssetViewModelConstructionParameters parameters) : AssetViewModel<PluginHostAsset>(parameters);

public abstract class PluginHostEditorViewModel : IAssetEditorViewModel
{
    public AssetViewModel Asset => null!;

    public Task EditorInitialized => Task.CompletedTask;

    public Task<bool> Initialize() => Task.FromResult(true);

    public bool PreviewClose(bool? save) => true;

    public void Destroy()
    {
    }
}

[AssetEditorViewModel<PluginHostAssetViewModel>]
public sealed class PluginHostAssetEditorViewModel : PluginHostEditorViewModel;

[AssetEditorView<PluginHostAssetEditorViewModel>]
public sealed class PluginHostEditorView : IEditorView
{
    public object DataContext { get; set; } = null!;

    public Task EditorInitialization => Task.CompletedTask;

    public Task<bool> InitializeEditor(IAssetEditorViewModel editor) => Task.FromResult(true);
}

public sealed class PluginHostDeclaredType;

public enum PluginHostImageEnum
{
    None,
    Declared,
    Both,
    Registered,
}

[EditorSettings]
public static class PluginHostSettings
{
    public static readonly SettingsContainer Container = new();

    public static readonly SettingsKey<bool> Key = new("Test/PluginHost/Key", Container, true);
}

public sealed class TestTemplateProvider : ITemplateProvider
{
    public string Name => nameof(TestTemplateProvider);

    public OverrideRule OverrideRule { get; set; }

    public List<string> OverriddenProviderNames { get; } = [];

    public bool Match(object? obj) => false;

    public int CompareTo(ITemplateProvider? other) => 0;
}

public sealed class TestCopyProcessor : ICopyProcessor
{
    public bool Accept(Type dataType) => false;

    public bool Process(ref object data, Stride.Core.Assets.Yaml.AttachedYamlAssetMetadata metadata) => false;
}

public sealed class TestNodeCommand : NodePresenterCommandBase
{
    public override string Name => nameof(TestNodeCommand);

    public override bool CanAttach(INodePresenter nodePresenter) => false;

    public override Task Execute(INodePresenter nodePresenter, object? parameter, object? preExecuteResult) => Task.CompletedTask;
}

public sealed class TestNodeUpdater : NodePresenterUpdaterBase;

public sealed class TestSessionService;

/// <summary>
/// A copy and paste service that only records its processors.
/// </summary>
public sealed class RecordingCopyPasteService : ICopyPasteService
{
    public List<object> Processors { get; } = [];

    public IReadOnlyCollection<IAssetPostPasteProcessor> PostProcessors => Processors.OfType<IAssetPostPasteProcessor>().ToList();

    public Stride.Core.Assets.Quantum.AssetPropertyGraphContainer PropertyGraphContainer => null!;

    public string CopyFromAsset(Stride.Core.Assets.Quantum.AssetPropertyGraph? propertyGraph, AssetId? sourceId, object value, bool isObjectReference) => throw new NotSupportedException();

    public string CopyFromAssets(IReadOnlyList<(Stride.Core.Assets.Quantum.AssetPropertyGraph propertyGraph, AssetId? sourceId, object value, bool isObjectReference)> items, Type itemType) => throw new NotSupportedException();

    public string CopyMultipleAssets(object container) => throw new NotSupportedException();

    public bool CanPaste(string text, Type targetRootType, Type targetMemberType, params Type[] expectedTypes) => false;

    public IPasteResult DeserializeCopiedData(string text, object targetObject, Type targetMemberType) => throw new NotSupportedException();

    public void RegisterProcessor(ICopyProcessor processor) => Processors.Add(processor);

    public void RegisterProcessor(IPasteProcessor processor) => Processors.Add(processor);

    public void RegisterProcessor(IAssetPostPasteProcessor processor) => Processors.Add(processor);

    public void UnregisterProcessor(ICopyProcessor processor) => Processors.Remove(processor);

    public void UnregisterProcessor(IPasteProcessor processor) => Processors.Remove(processor);

    public void UnregisterProcessor(IAssetPostPasteProcessor processor) => Processors.Remove(processor);
}

/// <summary>
/// A dispatcher for another thread: what is queued runs when the test says so.
/// </summary>
public sealed class QueueDispatcherService : IDispatcherService
{
    private readonly Queue<Action> queue = new();

    public int Pending => queue.Count;

    public void RunPending()
    {
        while (queue.TryDequeue(out var action))
            action();
    }

    public bool CheckAccess() => false;

    public void EnsureAccess(bool inDispatcherThread = true)
    {
    }

    public void Invoke(Action callback) => callback();

    public TResult Invoke<TResult>(Func<TResult> callback) => callback();

    public Task InvokeAsync(Action callback, CancellationToken token = default)
    {
        queue.Enqueue(callback);
        return Task.CompletedTask;
    }

    public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) => InvokeAsync(callback, token);

    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => throw new NotSupportedException();

    public Task InvokeTask(Func<Task> task, CancellationToken token = default) => throw new NotSupportedException();

    public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) => throw new NotSupportedException();
}
