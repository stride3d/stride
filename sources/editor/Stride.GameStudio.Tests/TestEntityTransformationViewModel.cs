// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.AssetEditors.GameEditor;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.EditorGame.Tools;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Editor.Engine;
using Xunit;

namespace Stride.GameStudio.Tests;

/// <summary>
/// The WPF transformation toolbar drives the shared tool service of the scene and prefab editors.
/// </summary>
public sealed class TestEntityTransformationViewModel
{
    private readonly ViewModelServiceProvider serviceProvider = new([new NullDispatcherService()]);

    [Fact]
    public void TheSnapsAndGizmoSizeOfTheSceneReachTheToolService()
    {
        var tools = new FakeToolService();
        var transform = new EntityTransformationViewModel(serviceProvider, new FakeController(tools));

        transform.LoadSettings(new SceneSettingsData
        {
            TranslationSnapValue = 0.5f, TranslationSnapActive = true,
            RotationSnapValue = 15f, RotationSnapActive = false,
            ScaleSnapValue = 2f, ScaleSnapActive = true,
            TransformationGizmoSize = 2.0,
        });
        Assert.Equal((0.5f, true), tools.Snaps[Transformation.Translation]);
        Assert.Equal((15f, false), tools.Snaps[Transformation.Rotation]);
        Assert.Equal((2f, true), tools.Snaps[Transformation.Scale]);
        Assert.Equal(2f, tools.GizmoSize);

        transform.RotationSnap.IsActive = true;
        Assert.Equal((15f, true), tools.Snaps[Transformation.Rotation]);
        transform.GizmoSize = 0.5;
        Assert.Equal(0.5f, tools.GizmoSize);
        var saved = new SceneSettingsData();
        transform.SaveSettings(saved);
        Assert.True(saved.RotationSnapActive);
        Assert.Equal(0.5, saved.TransformationGizmoSize);
        transform.Destroy();
    }

    [Fact]
    public void TheToolbarAndTheKeysOfTheGameSwitchTheTransformation()
    {
        var tools = new FakeToolService();
        var transform = new EntityTransformationViewModel(serviceProvider, new FakeController(tools));
        Assert.Equal(Transformation.Translation, transform.ActiveTransformation);

        // The toolbar activates the tool of the transformation, and the scale works in the object space only
        transform.ActiveTransformation = Transformation.Scale;
        Assert.Equal(Transformation.Scale, Assert.IsAssignableFrom<TransformationTool>(tools.Active).Transformation);
        Assert.Equal([TransformationSpace.ObjectSpace], transform.AvailableSpaces);
        Assert.Equal(TransformationSpace.ObjectSpace, tools.Space);

        // A key of the game (E) changes the tool: the toolbar follows
        tools.RaiseActiveToolChanged(tools.Tools.OfType<TransformationTool>().Single(x => x.Transformation == Transformation.Rotation));
        Assert.Equal(Transformation.Rotation, transform.ActiveTransformation);
        Assert.Equal(3, transform.AvailableSpaces.Count);

        // The select tool (Q) has no transformation gizmo: no transformation and no space
        tools.RaiseActiveToolChanged(tools.Tools[0]);
        Assert.Null(transform.ActiveTransformation);
        Assert.Empty(transform.AvailableSpaces);

        transform.Destroy();
        Assert.False(tools.HasActiveToolChangedHandlers);
    }

    /// <summary>
    /// A tool service without a game: it records what the toolbar gives it.
    /// </summary>
    private sealed class FakeToolService : IEditorGameToolViewModelService
    {
        public FakeToolService()
        {
            // The tools of a view, created as the shared service creates them
            Tools = EditorGameToolService.CreateTools(EditorGameToolService.GetToolTypes([]), new LoggerResult());
        }

        public IReadOnlyList<IEditorViewTool> Tools { get; }

        public IEditorViewTool Active { get; private set; }

        public TransformationSpace Space { get; private set; } = TransformationSpace.WorldSpace;

        public float GizmoSize { get; private set; } = 1f;

        public Dictionary<Transformation, (float Value, bool IsActive)> Snaps { get; } = [];

        public bool HasActiveToolChangedHandlers => ActiveToolChanged is not null;

        public event EventHandler<IEditorViewTool> ActiveToolChanged;

        public event EventHandler<IEditorViewTool> ToolFailed { add { } remove { } }

        public void RaiseActiveToolChanged(IEditorViewTool tool)
        {
            Active = tool;
            ActiveToolChanged?.Invoke(this, tool);
        }

        public Task<(IEditorViewTool ActiveTool, IReadOnlyList<IEditorViewTool> FailedTools)> GetStateAsync()
            => Task.FromResult((Active ?? Tools[1], (IReadOnlyList<IEditorViewTool>)[]));

        public Task<bool> ActivateAsync(IEditorViewTool tool)
        {
            Active = tool;
            return Task.FromResult(true);
        }

        public Task SetSpaceAsync(TransformationSpace space)
        {
            Space = space;
            return Task.CompletedTask;
        }

        public Task SetGizmoSizeAsync(float size)
        {
            GizmoSize = size;
            return Task.CompletedTask;
        }

        public Task SetSnapAsync(Transformation transformation, float value, bool isActive)
        {
            Snaps[transformation] = (value, isActive);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A game controller without a game, which has the given tool service.
    /// </summary>
    private sealed class FakeController(IEditorGameToolViewModelService tools) : IEditorGameController
    {
        public IEditorContentLoader Loader => throw new NotSupportedException();

        public Logger Logger { get; } = GlobalLogger.GetLogger(nameof(TestEntityTransformationViewModel));

        public Task GameContentLoaded => Task.CompletedTask;

        public Task GameEnded => Task.CompletedTask;

        public SessionNodeContainer GameSideNodeContainer => throw new NotSupportedException();

        public object View => throw new NotSupportedException();

        public IEditorGameViewport Viewport => throw new NotSupportedException();

        public Task<bool> StartGame() => Task.FromResult(true);

        public Task<bool> CreateScene() => Task.FromResult(true);

        public void OnHideGame() { }

        public void OnShowGame() { }

        public object FindGameSidePart(AbsoluteId partId) => null;

        public T GetService<T>() where T : IEditorGameViewModelService => tools is T service ? service : default;

        public IReadOnlyList<T> GetServices<T>() where T : IEditorGameViewModelService => tools is T service ? [service] : [];

        public void TriggerActiveRenderStageReevaluation() { }

        public void Destroy() { }

        public void Invoke(Action callback) => callback();

        public TResult Invoke<TResult>(Func<TResult> callback) => callback();

        public Task InvokeAsync(Action callback, CancellationToken token = default) { callback(); return Task.CompletedTask; }

        public Task LowPriorityInvokeAsync(Action callback, CancellationToken token = default) => InvokeAsync(callback, token);

        public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback, CancellationToken token = default) => Task.FromResult(callback());

        public Task InvokeTask(Func<Task> task, CancellationToken token = default) => task();

        public Task<TResult> InvokeTask<TResult>(Func<Task<TResult>> task, CancellationToken token = default) => task();

        public bool CheckAccess() => true;

        public void EnsureAccess(bool inDispatcherThread = true) { }
    }
}
