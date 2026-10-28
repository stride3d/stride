// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Threading;
using Stride.Core.Presentation.Avalonia.Services;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// Shows the dialogs of the template prompts over the active window of the editor.
/// </summary>
internal static class TemplateDialogs
{
    /// <summary>
    /// Shows a dialog modally over the active window (the new project window, usually), and gets whether it closed with <c>true</c>.
    /// Without a visible owner (a headless host), the dialog is shown on its own.
    /// </summary>
    public static Task<bool> ShowModalAsync(Window dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (DialogService.ActiveWindow is { IsVisible: true } owner)
                return await dialog.ShowDialog<bool>(owner);

            var closed = new TaskCompletionSource<bool>();
            dialog.Closed += (_, _) => closed.TrySetResult(false);
            dialog.Show();
            return await closed.Task;
        });
    }
}
