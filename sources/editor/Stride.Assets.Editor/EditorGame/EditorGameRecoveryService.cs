// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Pauses the game of an editor after an exception of its update or its drawing (<see cref="EditorServiceGame.ExceptionThrown"/>):
/// the editor shows the exception (<see cref="EditorGameHost.LastException"/>) until it resumes the game. The game of every
/// editor has it, in both editors.
/// </summary>
[EditorGameService(typeof(IEditorGameController), Order = 20)]
public sealed class EditorGameRecoveryService : EditorGameServiceBase, IEditorGameRecoveryViewModelService
{
    private readonly EditorGameHost host;
    private EditorServiceGame? game;

    public EditorGameRecoveryService(IGameEditorViewModel editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        host = editor.GameHost;
    }

    /// <inheritdoc />
    public override bool IsActive { get; set; } = true;

    void IEditorGameRecoveryViewModelService.Resume()
    {
        host.Dispatcher.EnsureAccess();

        if (!IsActive || game is null)
            return;

        // The game draws again
        game.Faulted = false;
        host.LastException = null;
    }

    /// <inheritdoc />
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = editorGame;
        game.ExceptionThrown += GameOnExceptionThrown;
        return Task.FromResult(true);
    }

    private void GameOnExceptionThrown(object? sender, ExceptionThrownEventArgs e)
    {
        if (!IsActive)
            return;

        e.Handled = true;
        host.Dispatcher.InvokeAsync(() => host.LastException = e.Exception).Forget();
    }
}
