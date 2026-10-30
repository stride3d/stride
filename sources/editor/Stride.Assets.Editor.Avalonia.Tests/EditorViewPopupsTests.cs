// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Core;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.Physics;
using Stride.Physics.Editor;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The gizmo options popup of the view toolbar of a scene or prefab editor lists what the plugins add.
/// </summary>
public sealed class EditorViewPopupsTests
{
    private readonly ViewModelServiceProvider serviceProvider = new([new NullDispatcherService()]);

    static EditorViewPopupsTests()
    {
        // The gizmos of the plugins, as in a session that loaded them
        PluginEditorAssemblies.Register();
    }

    [AvaloniaFact]
    public void TheGizmoOptionsListThePluginGizmosWithTheirDefaults()
    {
        using var controller = new FakeController();
        var gizmos = new EditorViewGizmosViewModel(serviceProvider, new EditorGameComponentGizmoService(controller), null);

        Assert.True(Find(gizmos, typeof(AudioEmitterComponent)).IsActive);
        Assert.True(Find(gizmos, typeof(CameraComponent)).IsActive);
        // The collider shapes and constraints show once checked, as in WPF
        Assert.False(Find(gizmos, typeof(PhysicsComponent)).IsActive);
        Assert.False(Find(gizmos, typeof(PhysicsConstraintComponent)).IsActive);
        Assert.False(gizmos.FallbackGizmo.IsActive);
        Assert.Equal(gizmos.Gizmos.OrderBy(x => x.ComponentName, StringComparer.CurrentCultureIgnoreCase).Select(x => x.ComponentName), gizmos.Gizmos.Select(x => x.ComponentName));
    }

    [AvaloniaFact]
    public void TheGizmoTogglesAndSizesGoToTheGame()
    {
        using var controller = new FakeController();
        var service = new EditorGameComponentGizmoService(controller);
        var transformationSizes = new List<float>();
        var gizmos = new EditorViewGizmosViewModel(serviceProvider, service, size => { transformationSizes.Add(size); return Task.CompletedTask; });

        var calls = controller.Calls;
        Find(gizmos, typeof(PhysicsComponent)).IsActive = true;
        Assert.Equal(calls + 1, controller.Calls);
        gizmos.ToggleAllCommand.Execute(false);
        Assert.All(gizmos.Gizmos, x => Assert.False(x.IsActive));

        gizmos.ComponentGizmoSizeIndex = Utils.ZoomFactorIdentityIndex + 2;
        Assert.Equal((float)Utils.ZoomFactors[Utils.ZoomFactorIdentityIndex + 2], service.GizmoSize);
        gizmos.TransformationGizmoSizeIndex = 1000;
        Assert.Equal(gizmos.MaximumSizeIndex, gizmos.TransformationGizmoSizeIndex);
        Assert.Equal([(float)Utils.ZoomFactors[^1]], transformationSizes);
        gizmos.FixedSize = true;
        Assert.True(service.FixedSize);
    }

    [AvaloniaFact]
    public void TheGizmosPanelShowsACheckBoxPerTypeOfComponent()
    {
        using var controller = new FakeController();
        var gizmos = new EditorViewGizmosViewModel(serviceProvider, new EditorGameComponentGizmoService(controller), null);
        var panel = new EditorViewGizmosPanel { DataContext = gizmos };
        var window = Show(panel);

        var checkBoxes = panel.GetVisualDescendants().OfType<CheckBox>().ToList();
        var physics = checkBoxes.Single(x => Equals(x.Content, Find(gizmos, typeof(PhysicsComponent)).ComponentName));
        Assert.False(physics.IsChecked);
        Assert.Contains(checkBoxes, x => Equals(x.Content, "Other entities"));
        Assert.Contains(checkBoxes, x => Equals(x.Content, "Fixed"));
        Assert.Equal(2, panel.GetVisualDescendants().OfType<Slider>().Count());
        physics.IsChecked = true;
        Assert.True(Find(gizmos, typeof(PhysicsComponent)).IsActive);
        window.Close();
    }

    private static EditorViewGizmoViewModel Find(EditorViewGizmosViewModel gizmos, Type componentType) => gizmos.Gizmos.Single(x => x.ComponentType == componentType);

    private static Window Show(Control content)
    {
        var window = new Window { Width = 400, Height = 800, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>
    /// A game controller without a game: the calls to the game thread run at once, and are counted.
    /// </summary>
    private sealed class FakeController : IEditorGameController, IDisposable
    {
        public int Calls { get; private set; }

        public IEditorContentLoader Loader => throw new NotSupportedException();

        public Logger Logger { get; } = GlobalLogger.GetLogger(nameof(EditorViewPopupsTests));

        public Task GameContentLoaded => Task.CompletedTask;

        public SessionNodeContainer GameSideNodeContainer => throw new NotSupportedException();

        public Task<bool> StartGame() => Task.FromResult(true);

        public Task<bool> CreateScene() => Task.FromResult(true);

        public void OnHideGame() { }

        public void OnShowGame() { }

        public object? FindGameSidePart(AbsoluteId partId) => null;

        public T GetService<T>() where T : IEditorGameViewModelService => default!;

        public IReadOnlyList<T> GetServices<T>() where T : IEditorGameViewModelService => [];

        public void TriggerActiveRenderStageReevaluation() { }

        public void Destroy() { }

        public void Dispose() { }

        public void Invoke(Action callback) => Run(callback);

        public TResult Invoke<TResult>(Func<TResult> callback) { Calls++; return callback(); }

        public Task InvokeAsync(Action callback, CancellationToken token = default) { Run(callback); return Task.CompletedTask; }

        public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) => InvokeAsync(callback, token);

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(Invoke(callback));

        public Task InvokeTask(Func<Task> task, CancellationToken token = default) { Calls++; return task(); }

        public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) { Calls++; return task(); }

        public bool CheckAccess() => true;

        public void EnsureAccess(bool inDispatcherThread = true) { }

        private void Run(Action callback)
        {
            Calls++;
            callback();
        }
    }
}
