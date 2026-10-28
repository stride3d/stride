// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Editor.Engine;
using Stride.Games;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The game of the scene editor, without a graphics device: the editor still opens when the session has no game viewport
/// or when its game cannot start.
/// </summary>
public sealed class SceneEditorGameTests
{
    [Fact]
    public async Task WithoutAGameViewportTheSceneEditorOpensWithoutAGameView()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("MainScene", new SceneAsset()));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var editor = new SceneEditorViewModel(fixture.Asset<SceneViewModel>("MainScene"));

        Assert.True(await editor.Initialize());

        Assert.Null(editor.GameView);
        editor.Destroy();
    }

    [Fact]
    public async Task AGameThatCannotStartLeavesTheSceneEditorOpenWithoutAGameView()
    {
        using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, ("MainScene", new SceneAsset()));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var viewportService = new FailingViewportService();
        fixture.ServiceProvider.RegisterService(viewportService);
        var editor = new SceneEditorViewModel(fixture.Asset<SceneViewModel>("MainScene"));

        Assert.False(await editor.Initialize());

        Assert.Null(editor.GameView);
        var viewport = Assert.Single(viewportService.Viewports);
        // The game thread let the viewport go
        Assert.True(viewport.IsDisposed);
        editor.Destroy();
    }

    private sealed class FailingViewportService : IEditorGameViewportService
    {
        public List<FailingViewport> Viewports { get; } = [];

        public IEditorGameViewport CreateViewport(Logger logger)
        {
            var viewport = new FailingViewport();
            Viewports.Add(viewport);
            return viewport;
        }
    }

    /// <summary>
    /// A viewport that cannot host a game, as one without a graphics device.
    /// </summary>
    private sealed class FailingViewport : IEditorGameViewport
    {
        public object View { get; } = new();

        public bool IsDisposed { get; private set; }

        public GameContext Attach(EmbeddedGame game) => throw new InvalidOperationException("No graphics device.");

        public void Initialize(EmbeddedGame game) { }

        public void BeginDraw(EmbeddedGame game) { }

        public void EndDraw(EmbeddedGame game, bool present) { }

        public void AfterEndDraw(EmbeddedGame game, bool present) { }

        public void Dispose() => IsDisposed = true;
    }
}
