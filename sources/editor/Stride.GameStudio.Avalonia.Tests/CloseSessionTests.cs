// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Assets.Editor.SceneEditor;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// File > Close, New, Open, Reload and Exit close the session while the games of its editors still run on their own threads:
/// the plugins, which take their settings keys with them, leave only once those games have ended.
/// </summary>
public sealed class CloseSessionTests
{
    [AvaloniaFact]
    public async Task ThePluginsLeaveOnceTheGamesOfTheClosedEditorsHaveEnded()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Scene", new GameEditorTestAsset()));
        fixture.Session.EditorCollection.OpenAssetEditor(fixture.Asset("Scene"));
        var editor = Assert.IsType<GameEditorTestViewModel>(Assert.Single(fixture.Session.EditorCollection.Editors));
        var stoppedBeforeThePluginsLeft = false;
        try
        {
            await editor.FirstFrame.WaitAsync(TimeSpan.FromSeconds(30));

            // What File > Close does: the session closes with its editors, then the plugins leave and their keys go
            Assert.True(await fixture.Main.CloseSessionAsync());
            stoppedBeforeThePluginsLeft = editor.Stopped.IsCompleted;
            fixture.Main.CloseSessionServices();
        }
        finally
        {
            // The game thread ends a few frames after the editor closes, whatever the test found
            editor.Destroy();
            await editor.Stopped.WaitAsync(TimeSpan.FromSeconds(30));
        }

        // Each of its frames found the key bindings of the scene editor, the last ones too
        Assert.Null(editor.FrameError);
        Assert.True(stoppedBeforeThePluginsLeft, "The session closed before the game of its editor ended.");
        Assert.Equal(GameEditorTestViewModel.FramesToEnd, editor.FramesAfterDestroy);
    }

    [AvaloniaFact]
    public async Task AnEditorThatDoesNotStopDoesNotKeepTheSessionOpen()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Scene", new GameEditorTestAsset()));
        fixture.Session.EditorCollection.StopTimeout = TimeSpan.FromMilliseconds(200);
        fixture.Session.EditorCollection.OpenAssetEditor(fixture.Asset("Scene"));
        var editor = Assert.IsType<GameEditorTestViewModel>(Assert.Single(fixture.Session.EditorCollection.Editors));
        editor.HangsOnDestroy = true;
        try
        {
            await editor.FirstFrame.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.True(await fixture.Main.CloseSessionAsync().WaitAsync(TimeSpan.FromSeconds(30)));
            Assert.False(editor.Stopped.IsCompleted);
        }
        finally
        {
            editor.HangsOnDestroy = false;
            await editor.Stopped.WaitAsync(TimeSpan.FromSeconds(30));
        }
        Assert.Null(editor.FrameError);
    }
}

[DataContract("GameEditorTestAsset")]
[AssetDescription(".sdgameeditortest")]
public sealed class GameEditorTestAsset : Asset;

[AssetViewModel<GameEditorTestAsset>]
public sealed class GameEditorTestAssetViewModel(AssetViewModelConstructionParameters parameters) : AssetViewModel<GameEditorTestAsset>(parameters);

/// <summary>
/// Opens <see cref="GameEditorTestAsset"/> in <see cref="GameEditorTestViewModel"/>, shown by <see cref="GameEditorTestView"/>.
/// </summary>
public sealed class GameEditorTestPlugin : AssetsPlugin
{
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        context.AssetEditorViewModelTypes.Register(typeof(GameEditorTestAssetViewModel), typeof(GameEditorTestViewModel));
        // The editor collection opens only the editors whose view the editor has
        context.AssetEditorViewTypes.Register(typeof(GameEditorTestViewModel), typeof(GameEditorTestView));
    }
}

/// <summary>
/// The view of <see cref="GameEditorTestViewModel"/>: it shows nothing.
/// </summary>
public sealed class GameEditorTestView : global::Avalonia.Controls.UserControl, IAssetEditorView;

/// <summary>
/// An editor with a game thread like that of the scene editor: every frame reads the key bindings of the scene editor
/// settings, and the thread ends a few frames after the editor is destroyed, as the game exits from work posted to its next
/// frame.
/// </summary>
public sealed class GameEditorTestViewModel : AssetEditorViewModel
{
    public const int FramesToEnd = 5;
    private static readonly TimeSpan FrameTime = TimeSpan.FromMilliseconds(10);

    private readonly TaskCompletionSource firstFrame = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool destroyed;
    private volatile bool hangsOnDestroy;
    private Thread? gameThread;

    public GameEditorTestViewModel(AssetViewModel asset)
        : base(asset)
    {
    }

    /// <summary>
    /// A task that completes once the game ran its first frame.
    /// </summary>
    public Task FirstFrame => firstFrame.Task;

    /// <inheritdoc/>
    public override Task Stopped => gameThread is null ? Task.CompletedTask : stopped.Task;

    /// <summary>
    /// The error of the first frame that failed, or <c>null</c>.
    /// </summary>
    public Exception? FrameError { get; private set; }

    /// <summary>
    /// How many frames the game ran after the editor was destroyed.
    /// </summary>
    public int FramesAfterDestroy { get; private set; }

    /// <summary>
    /// While <c>true</c>, the game keeps running after the editor is destroyed, as a game that hangs.
    /// </summary>
    public bool HangsOnDestroy { get => hangsOnDestroy; set => hangsOnDestroy = value; }

    public override Task<bool> Initialize()
    {
        gameThread = new Thread(RunGame) { IsBackground = true, Name = "Game editor test thread" };
        gameThread.Start();
        return Task.FromResult(true);
    }

    public override void Destroy()
    {
        if (destroyed)
            return;
        destroyed = true;
        base.Destroy();
    }

    private void RunGame()
    {
        try
        {
            while (!destroyed || hangsOnDestroy || FramesAfterDestroy < FramesToEnd)
            {
                if (destroyed)
                    FramesAfterDestroy++;
                try
                {
                    // What the camera of the scene editor reads on every frame
                    _ = SceneEditorSettings.IncreaseCamSpeed.GetValue();
                }
                catch (Exception e)
                {
                    FrameError ??= e;
                }
                firstFrame.TrySetResult();
                Thread.Sleep(FrameTime);
            }
        }
        finally
        {
            stopped.TrySetResult();
        }
    }
}
