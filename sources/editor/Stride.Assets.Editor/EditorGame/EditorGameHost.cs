// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The game of an asset editor that shows one (the scene, prefab and UI editors): it starts the game, shows its view and
/// the exception that paused it. The editor owns it and destroys it.
/// </summary>
public sealed class EditorGameHost : DispatcherViewModel
{
    private IEditorGameController? controller;
    private object? gameView;
    private Exception? lastException;
    private bool sceneInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorGameHost"/> class.
    /// </summary>
    /// <param name="serviceProvider">The services of the editor.</param>
    public EditorGameHost(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        CopyErrorToClipboardCommand = new AnonymousTaskCommand(ServiceProvider, CopyErrorToClipboardAsync);
        ResumeCommand = new AnonymousCommand(ServiceProvider, Resume);
    }

    /// <summary>
    /// Gets the controller of the game, or <c>null</c> before <see cref="StartAsync"/>, or when the editor has no game.
    /// </summary>
    public IEditorGameController? Controller { get => controller; private set => SetValue(ref controller, value); }

    /// <summary>
    /// Gets the control that shows the game, or <c>null</c> while the game has not started, or when the editor has no game.
    /// </summary>
    public object? GameView { get => gameView; private set => SetValue(ref gameView, value); }

    /// <summary>
    /// Gets or sets the exception that paused the game, or <c>null</c> while it runs.
    /// </summary>
    /// <remarks>The recovery service of the game sets it; <see cref="ResumeCommand"/> clears it.</remarks>
    public Exception? LastException { get => lastException; set => SetValue(ref lastException, value); }

    /// <summary>
    /// Gets whether the game has created its scene. Its content can still be loading.
    /// </summary>
    public bool SceneInitialized { get => sceneInitialized; private set => SetValue(ref sceneInitialized, value); }

    /// <summary>
    /// Gets a task that completes when the game has ended, or at once when the editor has no game.
    /// </summary>
    /// <remarks>The game reads the scene editor settings, which the plugins register, on every frame.</remarks>
    public Task Stopped => Controller?.GameEnded ?? Task.CompletedTask;

    /// <summary>
    /// Gets the command that copies <see cref="LastException"/> to the clipboard.
    /// </summary>
    public ICommandBase CopyErrorToClipboardCommand { get; }

    /// <summary>
    /// Gets the command that lets the game run again after an exception.
    /// </summary>
    public ICommandBase ResumeCommand { get; }

    /// <summary>
    /// Starts the game of <paramref name="gameController"/>, waits for its content and creates its scene (UI thread).
    /// </summary>
    /// <param name="gameController">The controller of the game, which this host destroys.</param>
    /// <returns><c>false</c> when the game could not start or create its scene, or when the host was destroyed meanwhile.</returns>
    public async Task<bool> StartAsync(IEditorGameController gameController)
    {
        ArgumentNullException.ThrowIfNull(gameController);
        Dispatcher.EnsureAccess();
        if (Controller is not null)
            throw new InvalidOperationException("The game has already started.");

        Controller = gameController;
        if (!await gameController.StartGame())
            return false;
        GameView = gameController.View;

        // Wait for the game to be fully initialized (including services). LoadContent of an editor game loads nothing, so
        // this is almost instant.
        await gameController.GameContentLoaded;
        if (IsDestroyed || !await gameController.CreateScene())
            return false;

        SceneInitialized = !IsDestroyed;
        return SceneInitialized;
    }

    /// <summary>
    /// Lets the game render: its view is shown.
    /// </summary>
    public void ShowGame()
    {
        Controller?.OnShowGame();
    }

    /// <summary>
    /// Throttles the game: its view is hidden.
    /// </summary>
    public void HideGame()
    {
        Controller?.OnHideGame();
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EditorGameHost));

        SceneInitialized = false;
        GameView = null;
        Controller?.Destroy();
        base.Destroy();
    }

    private async Task CopyErrorToClipboardAsync()
    {
        var log = LastException?.ToString();
        if (!string.IsNullOrEmpty(log) && ServiceProvider.TryGet<IClipboardService>() is { } clipboard)
        {
            try
            {
                await clipboard.SetTextAsync(log);
            }
            catch (SystemException e)
            {
                // No feedback when the copy fails, as for the copy of parts
                e.Ignore();
            }
        }
    }

    private void Resume()
    {
        Controller?.GetService<IEditorGameRecoveryViewModelService>()?.Resume();
    }
}
