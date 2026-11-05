// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Runtime.CompilerServices;
using Stride.Core.Assets.Editor.Internal;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Presentation.Services;
using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The default <see cref="IEditorDialogService.AddDelayedNotification"/>: the question waits for the main window, as the WPF
/// editor's delayed notifications do.
/// </summary>
internal static class DelayedNotifications
{
    private static readonly ConditionalWeakTable<IEditorDialogService, HashSet<SettingsKey>> Pending = new();

    public static async Task AskAsync(IEditorDialogService dialogs, SettingsKey<bool> confirmationSettingsKey, string message, string yesCaption, string noCaption, Action? yesAction, Action? noAction, SettingsKey<bool>? yesNoSettingsKey)
    {
        ArgumentNullException.ThrowIfNull(confirmationSettingsKey);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(yesCaption);
        ArgumentNullException.ThrowIfNull(noCaption);

        // Prevent duplicate
        var pending = Pending.GetOrCreateValue(dialogs);
        lock (pending)
        {
            if (!pending.Add(confirmationSettingsKey))
                return;
        }

        try
        {
            await dialogs.WaitForMainWindowActiveAsync();

            var yesNo = yesNoSettingsKey?.GetValue() ?? true;
            if (confirmationSettingsKey.GetValue())
            {
                var buttons = DialogButtons.Create([yesCaption, noCaption], 1, 2);
                var result = await dialogs.CheckedMessageBoxAsync(message, false, Tr._p("Settings", "Don't ask again"), buttons, MessageBoxImage.Question);
                // Closed without clicking on a button
                if (result.Result == 0)
                    return;

                yesNo = result.Result == 1;
                if (result.IsChecked == true && (yesNo || yesNoSettingsKey != null))
                {
                    confirmationSettingsKey.SetValue(false);
                    yesNoSettingsKey?.SetValue(yesNo);
                    EditorSettings.Save();
                }
            }
            if (yesNo)
                yesAction?.Invoke();
            else
                noAction?.Invoke();
        }
        finally
        {
            lock (pending)
            {
                pending.Remove(confirmationSettingsKey);
            }
        }
    }
}
